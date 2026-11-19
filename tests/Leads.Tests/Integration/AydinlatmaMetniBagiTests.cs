using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// `0019` — `Leads`'in kendi tarafı: talepteki bağın saklanışı (AC-23, AC-42, AC-43), eski
/// talepler (AC-34), geri alınamazlık (AC-48), `Content`'siz kap (AC-49), çağrı sayısı
/// (AC-51), hata yolunda bütçe (AC-44) ve iznin bugünkü biçimi (AC-53). Sürüm metninin
/// geri okunduğu kriterler burada DEĞİL, `Host.Tests`'te gerçek `Content` ile koşar
/// (Bilinen tuzak 1).
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class AydinlatmaMetniBagiTests(LeadsDatabase database) : IAsyncLifetime
{
    private const string OncekiMigration = "20260922080321_AddLeadDeleteGuard";

    private static readonly Guid SurumKimligi = Guid.Parse("0192c000-0000-7000-8000-0000000019a1");

    private readonly TalepOlusturmaHarness harness = new(database);

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "0019/AC-23: Uygulamanın kaydettiği hiçbir talep \"kayıt tutulmuyordu\" durumunu taşımaz: privacy yayındayken, yayında değilken ve hiç metni yokken bırakılan talepler durumlarını (\"sürüm\" ya da \"yayında değildi\") açıkça taşır (D15)")]
    public async Task YazarDurumuAcikca_IkiDurumda()
    {
        harness.AydinlatmaMetni.Yayinda(SurumKimligi);
        Guid yayinda = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac23-yayinda"))).Value;
        harness.AydinlatmaMetni.YayindaDegil();
        Guid degil = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac23-degil"))).Value;

        Assert.Equal(("published", (Guid?)SurumKimligi), await BagAsync(yayinda));
        Assert.Equal(("not_published", (Guid?)null), await BagAsync(degil));
    }

    [Fact(DisplayName = "0019/AC-34: Kartın migration'ından önce var olan talepler migration'dan sonra \"kayıt tutulmuyordu\" durumunu taşır, hiçbir sürüme — başlangıç sürümüne de — bağlanmaz ve bu durum \"yayında değildi\"den ayırt edilir")]
    public async Task Tasir_KayitTutulmuyordu_EskiTalepler()
    {
        (LeadsDbContext context, string baglanti) = await KarttanOnceKurAsync();
        await using LeadsDbContext _ = context;
        Guid eski = Guid.CreateVersion7();
        await EskiTalepYazAsync(baglanti, eski);

        await context.Database.MigrateAsync();

        Lead okunan = await context.Leads.AsNoTracking().SingleAsync(lead => lead.Id == new LeadId(eski));
        Assert.Equal((PrivacyNoticeStatus.NotRecorded, (Guid?)null), (okunan.PrivacyNoticeStatus, okunan.PrivacyNoticeVersionId));
        Assert.Equal(("not_recorded", (Guid?)null), await BagAsync(eski, baglanti));
        Assert.NotEqual(PrivacyNoticeStatusCodes.NotPublished, PrivacyNoticeStatusCodes.ToCode(okunan.PrivacyNoticeStatus));
    }

    [Fact(DisplayName = "0019/AC-42: Kişisel alanları anonimleştirilen talep, sürüm bağını ve durumunu (sürüm · yayında değildi · kayıt tutulmuyordu) korur")]
    public async Task Korur_BagiVeDurumu_Anonimlestirmede()
    {
        harness.AydinlatmaMetni.Yayinda(SurumKimligi);
        Guid surum = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac42-surum"))).Value;
        harness.AydinlatmaMetni.YayindaDegil();
        Guid degil = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac42-degil"))).Value;
        Guid eski = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac42-eski"))).Value;
        await SqlAsync($"UPDATE leads.leads SET privacy_notice_status = 'not_recorded' WHERE id = '{eski}'");

        foreach (Guid id in new[] { surum, degil, eski })
        {
            await using LeadsDbContext context = database.CreateContext();
            Lead talep = await context.Leads.SingleAsync(lead => lead.Id == new LeadId(id));
            talep.Anonymize();
            await context.SaveChangesAsync();
        }

        Assert.Equal(("published", (Guid?)SurumKimligi), await BagAsync(surum));
        Assert.Equal(("not_published", (Guid?)null), await BagAsync(degil));
        Assert.Equal(("not_recorded", (Guid?)null), await BagAsync(eski));
        await using LeadsDbContext dogrulama = database.CreateContext();
        Assert.All(await dogrulama.Leads.AsNoTracking().ToListAsync(), lead => Assert.True(lead.IsAnonymized()));
    }

    [Theory(DisplayName = "0019/AC-43: leads'te durumu \"sürüm\" olup kimliği boş olan ya da durumu \"yayında değildi\" veya \"kayıt tutulmuyordu\" olup kimliği dolu olan bir talep satırı veritabanında reddedilir")]
    [InlineData("published", null)]
    [InlineData("not_published", "0192c000-0000-7000-8000-0000000019a1")]
    [InlineData("not_recorded", "0192c000-0000-7000-8000-0000000019a1")]
    public async Task Reddedilir_TutarsizSatir(string durum, string? kimlik)
    {
        Guid kaynak = (await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: $"ac43-{durum}"))).Value;
        string surum = kimlik is null ? "NULL" : $"'{kimlik}'";

        PostgresException ekleme = await Assert.ThrowsAsync<PostgresException>(() => SqlAsync(
            $"""
            CREATE TEMP TABLE kopya ON COMMIT DROP AS SELECT * FROM leads.leads WHERE id = '{kaynak}';
            UPDATE kopya SET id = gen_random_uuid(), idempotency_key = 'ac43-kopya',
                privacy_notice_status = '{durum}', privacy_notice_version_id = {surum};
            INSERT INTO leads.leads SELECT * FROM kopya;
            """,
            islemde: true));
        PostgresException guncelleme = await Assert.ThrowsAsync<PostgresException>(() => SqlAsync(
            $"UPDATE leads.leads SET privacy_notice_status = '{durum}', privacy_notice_version_id = {surum} WHERE id = '{kaynak}'"));

        Assert.Equal(("23514", "ck_leads_privacy_notice"), (ekleme.SqlState, ekleme.ConstraintName));
        Assert.Equal(("23514", "ck_leads_privacy_notice"), (guncelleme.SqlState, guncelleme.ConstraintName));
        Assert.Equal(("not_published", (Guid?)null), await BagAsync(kaynak));
        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "0019/AC-44: Talep anında sürüm okuması hata verdiğinde talep kaydedilmez, hiçbir talebe \"yayında değildi\" yazılmaz, hız bütçesi tüketilmez (aynı istemciden ardından gelen geçerli talepler bütçenin tamamını kullanabilir) ve yanıt 500 application/problem+json'dur; gövdede istisna metni ve yığın izi yoktur")]
    public async Task Tuketmez_Butceyi_OkumaHatasinda()
    {
        harness.AydinlatmaMetni.Bozuk(new InvalidOperationException("içerik okunamadı"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac44-bozuk")));

        await using (LeadsDbContext context = database.CreateContext())
        {
            Assert.Equal(0, await context.Leads.CountAsync());
        }

        harness.AydinlatmaMetni.Onar();

        // Aynı kota, aynı istemci: bozuk okuma bütçeden bir şey yediyse beşinci talep 429 alırdı.
        for (int sira = 1; sira <= LeadRateLimitOptions.DefaultCapturedLeadLimit; sira++)
        {
            Assert.True((await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: $"ac44-{sira}"))).IsSuccess);
        }

        Result<Guid> fazla = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac44-fazla"));
        Assert.Equal(LeadErrors.RateLimited, fazla.Error.Code);
    }

    [Fact(DisplayName = "0019/AC-48: Bu kartın iki migration'ının Down'u çağrıldığında NotSupportedException atar; sürüm satırları ve taleplerin sürüm bağları yerinde kalır")]
    public async Task Atar_Down_BaglarYerindeKalir()
    {
        (LeadsDbContext context, string baglanti) = await KarttanOnceKurAsync();
        await using LeadsDbContext _ = context;
        await context.Database.MigrateAsync();
        Guid id = Guid.CreateVersion7();
        await EskiTalepYazAsync(baglanti, id);
        await SqlAsync(
            $"UPDATE leads.leads SET privacy_notice_status = 'published', privacy_notice_version_id = '{SurumKimligi}' WHERE id = '{id}'",
            baglanti: baglanti);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            context.GetService<IMigrator>().MigrateAsync(OncekiMigration));

        Assert.Equal(("published", (Guid?)SurumKimligi), await BagAsync(id, baglanti));
    }

    [Fact(DisplayName = "0019/AC-49: Content'in kayıtlı olmadığı bir kapta Leads'in talep yolu sessiz bir varsayılana düşmez: IPrivacyNoticeQueries çözülemediği için hata verir ve hiçbir talebe \"yayında değildi\" yazılmaz")]
    public async Task Dusmez_SessizVarsayilana_ContentYokken()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLeads(database.ConnectionString, ["tr", "ar", "en"]);
        builder.Services.Replace(ServiceDescriptor.Singleton<IListingSnapshotSource>(new FakeListingSnapshotSource()));
        builder.Services.Replace(ServiceDescriptor.Singleton<ICanonicalModelNameSource>(new FakeCanonicalModelNameSource()));
        await using WebApplication app = builder.Build();
        app.MapLeads();
        await app.StartAsync();

        using HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(new
            {
                listingSlug = FakeListingSnapshotSource.GecerliSlug,
                leadType = "information_request",
                locale = "tr",
                fullName = "Ayşe Yılmaz",
                phone = "+905321112233",
                consentGiven = true,
            }),
        };
        istek.Headers.Add("Idempotency-Key", "ac49-anahtar");

        InvalidOperationException hata = await Assert.ThrowsAsync<InvalidOperationException>(() => app.GetTestClient().SendAsync(istek));

        Assert.Contains(nameof(IPrivacyNoticeQueries), hata.Message, StringComparison.Ordinal);
        Assert.Empty(app.Services.GetServices<IPrivacyNoticeQueries>());
        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Leads.CountAsync());
        await app.StopAsync();
    }

    [Fact(DisplayName = "0019/AC-51: Leads.Tests'te sürüm portu kabul edilen talepte tam bir kez çağrılır; aynı tekrar anahtarıyla ilk talebi döndüren istekte hiç çağrılmaz")]
    public async Task Cagrilir_KabuldeBirKez_TekrardaHic()
    {
        harness.AydinlatmaMetni.Yayinda(SurumKimligi);

        Result<Guid> ilk = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac51-anahtar"));
        int ilkSonrasi = harness.AydinlatmaMetni.CallCount;
        Result<Guid> tekrar = await harness.CalistirAsync(TalepOlusturmaHarness.Komut(anahtar: "ac51-anahtar"));

        Assert.Equal(ilk.Value, tekrar.Value);
        Assert.Equal(1, ilkSonrasi);
        Assert.Equal(1, harness.AydinlatmaMetni.CallCount);
    }

    [Fact(DisplayName = "0019/AC-53: 0005'in kabul kriterlerinin testleri doğrulamaları değiştirilmeden yeşil kalır (yalnız kurulumları değişebilir) ve izin kaydı bir bool olarak kalır")]
    public async Task Kalir_IzinBool()
    {
        PropertyInfo izin = typeof(Lead).GetProperty(nameof(Lead.ConsentGiven), BindingFlags.Instance | BindingFlags.NonPublic)!;

        await using NpgsqlConnection baglanti = new(database.ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(
            "SELECT data_type FROM information_schema.columns WHERE table_schema = 'leads' AND table_name = 'leads' AND column_name = 'consent_given'",
            baglanti);

        Assert.Equal(typeof(bool), izin.PropertyType);
        Assert.Equal("boolean", await komut.ExecuteScalarAsync());
    }

    private async Task<(string Durum, Guid? Kimlik)> BagAsync(Guid id, string? baglanti = null)
    {
        await using NpgsqlConnection connection = new(baglanti ?? database.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand komut = new(
            "SELECT privacy_notice_status, privacy_notice_version_id FROM leads.leads WHERE id = @id",
            connection);
        komut.Parameters.AddWithValue("id", id);
        await using DbDataReader okuyucu = await komut.ExecuteReaderAsync();
        Assert.True(await okuyucu.ReadAsync());

        return (okuyucu.GetString(0), okuyucu.IsDBNull(1) ? null : okuyucu.GetGuid(1));
    }

    private async Task SqlAsync(string sql, bool islemde = false, string? baglanti = null)
    {
        await using NpgsqlConnection connection = new(baglanti ?? database.ConnectionString);
        await connection.OpenAsync();
        await using NpgsqlTransaction? islem = islemde ? await connection.BeginTransactionAsync() : null;
        await using NpgsqlCommand komut = new(sql, connection, islem);
        await komut.ExecuteNonQueryAsync();

        if (islem is not null)
        {
            await islem.CommitAsync();
        }
    }

    /// <summary>Yeni bir veritabanı, kartın migration'ının ÖNCESİNE kurulur (D23: `Down`'a dayanmaz).</summary>
    private async Task<(LeadsDbContext Context, string Baglanti)> KarttanOnceKurAsync()
    {
        string ad = $"goc_{Guid.NewGuid():N}";

        await using (NpgsqlConnection ana = new(database.ConnectionString))
        {
            await ana.OpenAsync();
            await using NpgsqlCommand olustur = new($"CREATE DATABASE {ad}", ana);
            await olustur.ExecuteNonQueryAsync();
        }

        string baglanti = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = ad }.ConnectionString;
        DbContextOptionsBuilder<LeadsDbContext> builder = new();
        builder.UseNpgsql(baglanti, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LeadsDbContext.Schema));
        LeadsDbContext context = new(builder.Options);
        await context.GetService<IMigrator>().MigrateAsync(OncekiMigration);

        return (context, baglanti);
    }

    /// <summary>Kartın migration'ından önceki şemanın kolonlarıyla bir talep (bugünkü EF modeli o şemaya yazamaz).</summary>
    private static async Task EskiTalepYazAsync(string baglanti, Guid id)
    {
        await using NpgsqlConnection connection = new(baglanti);
        await connection.OpenAsync();
        await using NpgsqlCommand komut = new(
            """
            INSERT INTO leads.leads (id, type, status, branch_id, submitted_locale, consent_given, idempotency_key,
                captured_at, full_name, phone, vehicle_canonical_model_name, listing_id,
                vehicle_price_amount, vehicle_price_currency_code, vehicle_price_minor_unit_exponent, vehicle_price_on_request)
            VALUES (@id, 1, 1, @sube, 'tr', true, @anahtar, now(), 'Eski Talep', '+905321112233', 'Toyota Corolla',
                @ilan, 1250000, 'TRY', 2, false)
            """,
            connection);
        komut.Parameters.AddWithValue("id", id);
        komut.Parameters.AddWithValue("sube", FakeListingSnapshotSource.SubeId);
        komut.Parameters.AddWithValue("anahtar", $"eski-{id}");
        komut.Parameters.AddWithValue("ilan", FakeListingSnapshotSource.ListingId);
        await komut.ExecuteNonQueryAsync();
    }
}
