using CarGallery.Modules.Identity.Infrastructure;
using CarGallery.Modules.Identity.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// Şemanın kendisi hakkında kriterler var; bunlar ancak GERÇEK veritabanına bakılarak
/// doğrulanabilir (docs/testing.md § 1).
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class SemaTests(IdentityDatabase database)
{
    /// <summary>
    /// C7/Yasak Listesi 22 — kod saklanır, çevrilmiş ad değil. Kolonun TİPİNE bakmak
    /// yetmez; içine yazılan değere bakılır.
    /// </summary>
    [Fact(DisplayName = "0006/AC-52: Veritabanında rol ve durum kod olarak saklanır; çevrilmiş adları saklanmaz")]
    public async Task KodOlarakSaklanir_RolVeDurum()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", rol: "manager"));

        await using IdentityDbContext context = database.CreateContext();

        int rol = await context.Database
            .SqlQuery<int>($"SELECT role AS \"Value\" FROM identity.staff LIMIT 1")
            .SingleAsync();

        int durum = await context.Database
            .SqlQuery<int>($"SELECT status AS \"Value\" FROM identity.staff LIMIT 1")
            .SingleAsync();

        Assert.Equal(1, rol);
        Assert.Equal(1, durum);

        // Çevrilmiş ad hiçbir kolonda geçmiyor: ne "Yönetici", ne "Manager", ne "Aktif".
        string satir = await context.Database
            .SqlQuery<string>($"SELECT staff::text AS \"Value\" FROM identity.staff LIMIT 1")
            .SingleAsync();

        foreach (string yasak in new[] { "Yönetici", "Manager", "Aktif", "Active", "manager" })
        {
            Assert.DoesNotContain(yasak, satir, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0006/AC-12: `Ali@x.com` kayıtlıyken `ali@x.com` ile ikinci bir personel eklenemez")]
    public async Task TekilIndeksVar_NormalizeGirisKimliginde()
    {
        await database.ResetAsync();

        await using IdentityDbContext context = database.CreateContext();

        int indeks = await context.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM pg_indexes
                WHERE schemaname = 'identity' AND indexname = {StaffConfiguration.LoginIdIndexName}
                """)
            .SingleAsync();

        Assert.Equal(1, indeks);
    }

    [Fact(DisplayName = "0006/AC-50: Personel kaydının döndüğü hiçbir yanıt parolayı veya parola özetini içermez")]
    public async Task SaklanmazAcikParola_StaffTablosunda()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "yonetici@ornek.com", parola: "acik-parola-1234", rol: "manager"));

        await using IdentityDbContext context = database.CreateContext();

        string satir = await context.Database
            .SqlQuery<string>($"SELECT staff::text AS \"Value\" FROM identity.staff LIMIT 1")
            .SingleAsync();

        Assert.DoesNotContain("acik-parola-1234", satir, StringComparison.Ordinal);
        Assert.Contains("pbkdf2-sha256", satir, StringComparison.Ordinal);
    }
}
