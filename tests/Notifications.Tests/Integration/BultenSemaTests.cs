using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` **D23** · **D27** · C6 — şemanın kendi kuralları. Test ŞEMAYA bakıyor: kural kod
/// yoluna değil kısıta dayanmalı ve kısıtı ancak gerçek PostgreSQL gösterir.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class BultenSemaTests(NotificationsDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    [Theory(DisplayName = "0018/AC-84: Hem bir talebe hem bir aboneliğe bağlı ya da hiçbirine bağlı olmayan bir gönderim kaydı veritabanı tarafından reddedilir (D23)")]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    public async Task Reddedilir_IkiHedefliYaDaHedefsizKayit(bool talep, bool abonelik, bool donem)
    {
        await database.ResetNewsletterAsync();

        PostgresException hata = await Assert.ThrowsAsync<PostgresException>(() =>
            KaydetAsync(talep ? Guid.CreateVersion7() : null, abonelik ? Guid.CreateVersion7() : null, donem ? Guid.CreateVersion7() : null));

        Assert.Equal(PostgresErrorCodes.CheckViolation, hata.SqlState);
        Assert.Equal("ck_messages_single_target", hata.ConstraintName);
    }

    [Theory(DisplayName = "0018/AC-84: Hem bir talebe hem bir aboneliğe bağlı ya da hiçbirine bağlı olmayan bir gönderim kaydı veritabanı tarafından reddedilir (D23)")]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    public async Task KabulEdilir_UcGecerliBicim(bool talep, bool abonelik, bool donem)
    {
        await database.ResetNewsletterAsync();

        await KaydetAsync(talep ? Guid.CreateVersion7() : null, abonelik ? Guid.CreateVersion7() : null, donem ? Guid.CreateVersion7() : null);

        await using NotificationsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Messages.CountAsync());
    }

    [Fact(DisplayName = "0018/AC-69 (şema yarısı): Dönem işleyicisi aynı dönem için **ardışık** iki kez çağrıldığında her abone o dönem için en çok bir bülten kaydı alır")]
    public async Task Reddedilir_AyniAbonelikVeDonemIcinIkinciKayit()
    {
        await database.ResetNewsletterAsync();

        Guid abonelik = Guid.CreateVersion7();
        Guid donem = Guid.CreateVersion7();

        await KaydetAsync(null, abonelik, donem);

        PostgresException hata = await Assert.ThrowsAsync<PostgresException>(() => KaydetAsync(null, abonelik, donem));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, hata.SqlState);
    }

    [Fact(DisplayName = "0018/AC-93: İzin olay kaydı e-posta adresini hiçbir alanda taşımaz; bir istek → teyit → çıkış → istek → teyit dizisinden sonra beş olayın beşi de zamanlarıyla durur")]
    public async Task TasimazAdres_IzinOlayKaydiSemasi()
    {
        await using NotificationsDbContext context = database.CreateContext();

        List<string> kolonlar = await context.Database
            .SqlQuery<string>($"""
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'notifications' AND table_name = 'consent_events'
                """)
            .ToListAsync();

        Assert.Equal(["id", "kind", "occurred_at", "subscription_id"], kolonlar.Order(StringComparer.Ordinal));
    }

    [Fact(DisplayName = "0018/AC-93: İzin olay kaydı e-posta adresini hiçbir alanda taşımaz; bir istek → teyit → çıkış → istek → teyit dizisinden sonra beş olayın beşi de zamanlarıyla durur")]
    public async Task Reddeder_IzinOlayininGuncellenmesiniVeSilinmesini()
    {
        await database.ResetNewsletterAsync();

        await using (NotificationsDbContext context = database.CreateContext())
        {
            context.ConsentEvents.Add(ConsentEvent.Record(Guid.CreateVersion7(), ConsentEventKind.Requested, An));
            await context.SaveChangesAsync();
        }

        await using NotificationsDbContext okuma = database.CreateContext();

        PostgresException guncelleme = await Assert.ThrowsAsync<PostgresException>(() =>
            okuma.Database.ExecuteSqlRawAsync("UPDATE notifications.consent_events SET kind = 3"));
        PostgresException silme = await Assert.ThrowsAsync<PostgresException>(() =>
            okuma.Database.ExecuteSqlRawAsync("DELETE FROM notifications.consent_events"));

        Assert.Contains("append-only", guncelleme.MessageText, StringComparison.Ordinal);
        Assert.Contains("append-only", silme.MessageText, StringComparison.Ordinal);
        Assert.Equal(1, await okuma.ConsentEvents.CountAsync());
    }

    [Fact(DisplayName = "0018/AC-96: Teyit iletisi ile bültenin başlık, satır ve altlık şablonları ve \"Fiyat sorunuz\" ile \"Diğer yeni araçlar…\" metinleri TR, AR ve EN üçünde de seed edilmiştir; eksik şablon kontrolü backend testiyle yeşildir (`0009` C13)")]
    public async Task Vardir_UcDilde_BultenSablonlari()
    {
        string[] anahtarlar =
        [
            TemplateKeys.NewsletterConfirmation,
            TemplateKeys.NewsletterHeader,
            TemplateKeys.NewsletterRow,
            TemplateKeys.NewsletterFooter,
            TemplateKeys.NewsletterPriceOnRequest,
            TemplateKeys.NewsletterMore,
        ];

        Assert.All(anahtarlar, anahtar => Assert.Contains(anahtar, TemplateKeys.All));

        await using NotificationsDbContext context = database.CreateContext();

        foreach (string anahtar in anahtarlar)
        {
            foreach (string locale in new[] { "tr", "ar", "en" })
            {
                NotificationTemplate? sablon = await context.Templates.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.TemplateKey == anahtar && item.Locale == locale);

                Assert.True(sablon is not null, $"eksik şablon: {anahtar} / {locale}");
                Assert.False(string.IsNullOrWhiteSpace(sablon.Subject), $"boş konu: {anahtar} / {locale}");
                Assert.False(string.IsNullOrWhiteSpace(sablon.Body), $"boş gövde: {anahtar} / {locale}");
            }
        }
    }

    /// <summary>
    /// Zincirin ARA noktası: `0009`'un iki migration'ı uygulanmış, bu kartınkiler uygulanmamış.
    /// O hâlde kuyruğa giren talep iletisi `0018`'in migration'larından SONRA gönderilmeli —
    /// `lead_id` boş değer kabul eder hâle gelirken var olan satır CHECK'i geçmeli.
    /// </summary>
    [Fact(DisplayName = "0018/AC-85: Migration'dan önce kuyrukta bekleyen bir talep iletisi, migration'dan sonra gönderilir ve gönderildi işaretlenir")]
    public async Task Gonderilir_MigrationOncesiKuyruktakiTalepIletisi()
    {
        string baglanti = await database.CreateEmptyDatabaseAsync("goc_0018");
        Guid talepId = Guid.CreateVersion7();

        await using (NotificationsDbContext eski = NotificationsDatabase.CreateContext(baglanti))
        {
            await eski.GetService<IMigrator>().MigrateAsync("SeedNotificationTemplates");

            await eski.Database.ExecuteSqlAsync($"""
                INSERT INTO notifications.messages
                    (id, lead_id, template_key, locale, channel, status, attempt_count, next_attempt_at, queued_at)
                VALUES ({Guid.CreateVersion7()}, {talepId}, 'lead.received.visitor', 'tr', 1, 1, 0, {An}, {An})
                """);
        }

        await using (NotificationsDbContext yeni = NotificationsDatabase.CreateContext(baglanti))
        {
            await yeni.Database.MigrateAsync();
        }

        await using NotificationsDbContext context = NotificationsDatabase.CreateContext(baglanti);
        SayanTasiyici tasiyici = new();

        await new NotificationDispatcher(
            context,
            tasiyici,
            new TemplateResolver(context, NullLogger<TemplateResolver>.Instance),
            new TekTalep(talepId),
            new TekSube(),
            new TekSlug(),
            Besteci.Kur(context, new Bulten.SabitSaat(An)),
            Bulten.Secenekler(),
            new Bulten.SabitSaat(An),
            NullLogger<NotificationDispatcher>.Instance).DispatchDueAsync(CancellationToken.None);

        NotificationMessage mesaj = await context.Messages.AsNoTracking().SingleAsync();

        Assert.Equal(1, tasiyici.Sayi);
        Assert.Equal(NotificationStatus.Sent, mesaj.Status);
        Assert.Equal(talepId, mesaj.LeadId);
    }

    private async Task KaydetAsync(Guid? talep, Guid? abonelik, Guid? donem)
    {
        await using NotificationsDbContext context = database.CreateContext();

        await context.Database.ExecuteSqlAsync($"""
            INSERT INTO notifications.messages
                (id, lead_id, subscription_id, period_id, template_key, locale, channel, status, attempt_count, queued_at)
            VALUES ({Guid.CreateVersion7()}, {talep}, {abonelik}, {donem}, 'x', 'tr', 1, 1, 0, {An})
            """);
    }

    private sealed class SayanTasiyici : INotificationTransport
    {
        internal int Sayi { get; private set; }

        public Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            Sayi++;
            return Task.FromResult(SendResult.Success());
        }
    }

    private sealed class TekTalep(Guid talepId) : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult<LeadNotificationDto?>(leadId == talepId
                ? new LeadNotificationDto(talepId, "ziyaretci@ornek.com", "tr", "Toyota Corolla", Guid.CreateVersion7(), Guid.CreateVersion7())
                : null);
    }

    private sealed class TekSube : IBranchContactQueries
    {
        public Task<BranchNotificationContactDto?> FindContactAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<BranchNotificationContactDto?>(new BranchNotificationContactDto(branchId, "Kadıköy Şubesi", null));
    }

    private sealed class TekSlug : IListingSlugQueries
    {
        public Task<string?> FindSlugAsync(Guid listingId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("toyota-corolla-2019-abc123");
    }
}
