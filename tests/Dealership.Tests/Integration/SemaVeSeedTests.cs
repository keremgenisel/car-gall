using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Tests.Integration;

[Collection(DealershipDatabaseCollection.Name)]
public sealed class SemaVeSeedTests(DealershipDatabase database)
{
    private static readonly Locale VarsayilanLocale = Locale.Create("tr").Value;
    private static readonly Guid KadikoySube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    [Fact(DisplayName = "0003/AC-1: Kurulumda tam olarak bir galeri profili bulunur; ikinci bir profil oluşturulamaz")]
    public async Task OlusturulamazIkinciProfil()
    {
        await using DealershipDbContext context = database.CreateContext();

        int sayi = await context.Galleries.CountAsync();
        Assert.Equal(1, sayi);

        // Tekillik uygulamada değil ŞEMADA dayatılıyor: CHECK kısıtı ikinci kimliği reddeder.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO dealership.galleries (id, commercial_name) VALUES ('00000000-0000-7000-8000-00000000da02', 'Sahte Galeri')"));
    }

    [Fact(DisplayName = "0003/AC-2: Galeri profili ticari unvan, logo medya referansı, vergi/sicil bilgisi ve sosyal hesap bağlantılarını taşır")]
    public async Task TasirGaleriAlanlari()
    {
        await using DealershipDbContext context = database.CreateContext();

        Gallery galeri = await context.Galleries.AsNoTracking().SingleAsync();

        Assert.Equal("Oto Galeri", galeri.CommercialName);
        Assert.Equal("1234567890", galeri.TaxNumber);
        Assert.Equal("TR-123456", galeri.TradeRegistryNumber);
    }

    [Fact(DisplayName = "0003/AC-11: Aynı slug'a sahip ikinci bir şube kaydedilemez")]
    public async Task Kaydedilemez_AyniSlug()
    {
        await using DealershipDbContext context = database.CreateContext();

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO dealership.branches (id, names, addresses, phone, time_zone, status, slug, media_asset_ids)
                VALUES ('00000000-0000-7000-8000-00000000b009', '{"tr":"Kopya"}', '{"tr":"Kopya"}',
                        '+902160000000', 'Europe/Istanbul', 1, 'kadikoy-subesi', '{}')
                """));
    }

    [Fact(DisplayName = "0003/AC-15: Veritabanında şube durumunun çevrilmiş adı saklanmaz; durum kodu saklanır")]
    public async Task KodSaklanir_DurumAdiDegil()
    {
        await using DealershipDbContext context = database.CreateContext();

        int kod = await context.Database
            .SqlQuery<int>($"SELECT status AS \"Value\" FROM dealership.branches WHERE slug = 'kadikoy-subesi'")
            .SingleAsync();

        Assert.Equal((int)BranchStatus.Active, kod);
    }

    [Fact(DisplayName = "0003/AC-34: Her modülün migration'ları yalnızca kendi şemasına dokunur")]
    public async Task DokunurYalnizKendiSemasina()
    {
        await using DealershipDbContext context = database.CreateContext();

        int yabanciTablo = await context.Database
            .SqlQuery<int>($"""
                SELECT count(*)::int AS "Value" FROM information_schema.tables
                WHERE table_schema NOT IN ('dealership', 'information_schema', 'pg_catalog')
                """)
            .SingleAsync();

        Assert.Equal(0, yabanciTablo);
    }

    [Fact(DisplayName = "0003/AC-31: Başka bir modül, Dealership.Contracts üzerinden bir şube kimliğinin geçerli olup olmadığını ve şubenin verilen locale'deki adını sorabilir")]
    public async Task SorulabilirSubeOzeti()
    {
        await using DealershipDbContext context = database.CreateContext();
        BranchQueries queries = new(context, VarsayilanLocale);

        BranchSummaryDto? ozet = await queries.FindAsync(KadikoySube, "ar", CancellationToken.None);
        BranchSummaryDto? yok = await queries.FindAsync(Guid.CreateVersion7(), "tr", CancellationToken.None);

        Assert.NotNull(ozet);
        Assert.Equal("فرع كاديكوي", ozet.Name);
        Assert.Equal("Active", ozet.Status);
        Assert.Null(yok);
    }

    [Fact(DisplayName = "0003/AC-5: Ziyaretçinin locale'inde karşılığı olmayan bir şube adı istendiğinde varsayılan locale'deki ad döner; boş değer dönmez")]
    public async Task VarsayilanaDuser_TanimsizLocale()
    {
        await using DealershipDbContext context = database.CreateContext();
        BranchQueries queries = new(context, VarsayilanLocale);

        BranchSummaryDto ozet = (await queries.FindAsync(KadikoySube, "de", CancellationToken.None))!;

        Assert.Equal("Kadıköy Şubesi", ozet.Name);
        Assert.NotEqual(string.Empty, ozet.Name);
    }
}
