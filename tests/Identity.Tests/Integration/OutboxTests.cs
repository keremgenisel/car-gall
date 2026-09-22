using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Contracts.Events;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// C5 — duyuru iş verisiyle AYNI transaction'da outbox'a yazılır. Burada sınanan şey
/// duyurunun üretilmesi değil (onu domain testi yapıyor), birlikte kalıcılaşmasıdır:
/// iki yazma ayrı düşerse ya duyurulmayan bir pasifleştirme ya da olmayan bir
/// pasifleştirmenin duyurusu kalır.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class OutboxTests(IdentityDatabase database)
{
    [Fact(DisplayName = "0006/AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public async Task Yazilir_StaffDeactivated_PersonelKimligiyle()
    {
        IdentityHarness harness = await KurAsync();
        Guid personelId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "danisman@ornek.com", rol: "sales_advisor"))).Value;

        await harness.KomutAsync(k => k.DeactivateAsync(personelId, CancellationToken.None));

        await using IdentityDbContext context = database.CreateContext();
        OutboxMessage satir = await context.OutboxMessages.AsNoTracking().SingleAsync();

        Assert.Equal(typeof(StaffDeactivated).FullName, satir.Type);

        StaffDeactivated duyuru = JsonSerializer.Deserialize<StaffDeactivated>(satir.Payload)!;
        Assert.Equal(personelId, duyuru.StaffId);
    }

    [Fact(DisplayName = "0006/AC-49: Üretilen her duyuru benzersiz bir duyuru kimliği taşır")]
    public async Task Benzersiz_DuyuruKimligi()
    {
        IdentityHarness harness = await KurAsync();

        foreach (string kimlik in new[] { "bir@ornek.com", "iki@ornek.com", "uc@ornek.com" })
        {
            Guid id = (await harness.PersonelEkleAsync(
                IdentityHarness.EkleKomutu(kimlik: kimlik, rol: "sales_advisor"))).Value;

            await harness.KomutAsync(k => k.DeactivateAsync(id, CancellationToken.None));
        }

        await using IdentityDbContext context = database.CreateContext();
        List<OutboxMessage> satirlar = await context.OutboxMessages.AsNoTracking().ToListAsync();

        Assert.Equal(3, satirlar.Count);
        Assert.Equal(3, satirlar.Select(satir => satir.Id).Distinct().Count());

        HashSet<Guid> duyuruKimlikleri =
        [
            .. satirlar.Select(satir => JsonSerializer.Deserialize<StaffDeactivated>(satir.Payload)!.EventId)
        ];

        Assert.Equal(3, duyuruKimlikleri.Count);
    }

    /// <summary>
    /// Değişiklik geri alındığında duyuru da geri alınır. Burada geri alma, son aktif
    /// Yönetici korumasının işi bitirmeden reddetmesiyle canlandırılıyor: hiçbir şey
    /// kaydedilmediği için outbox da boş kalır.
    /// </summary>
    [Fact(DisplayName = "0006/AC-48: Personel değişikliği kaydedildiğinde duyurusu da kaydedilmiştir; değişiklik geri alındığında kaydedilmiş bir duyuru kalmaz")]
    public async Task Kalmaz_Duyuru_DegisiklikGeriAlindiginda()
    {
        IdentityHarness harness = await KurAsync();
        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        Result sonuc = await harness.KomutAsync(k => k.DeactivateAsync(yoneticiId, CancellationToken.None));

        Assert.True(sonuc.IsFailure);

        await using IdentityDbContext context = database.CreateContext();

        Assert.Equal(0, await context.OutboxMessages.CountAsync());
        Assert.Equal(
            StaffStatus.Active,
            (await context.Staff.SingleAsync(staff => staff.Id == new StaffId(yoneticiId))).Status);
    }

    [Fact(DisplayName = "0006/AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public async Task TasimazKisiselVeri_OutboxGovdesi()
    {
        IdentityHarness harness = await KurAsync();
        Guid personelId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            ad: "Mehmet Demir", kimlik: "mehmet@ornek.com", rol: "sales_advisor"))).Value;

        await harness.KomutAsync(k => k.DeactivateAsync(personelId, CancellationToken.None));

        await using IdentityDbContext context = database.CreateContext();
        OutboxMessage satir = await context.OutboxMessages.AsNoTracking().SingleAsync();

        Assert.DoesNotContain("Mehmet", satir.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("mehmet@ornek.com", satir.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("pbkdf2", satir.Payload, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IdentityHarness> KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", rol: "manager"));

        return harness;
    }
}
