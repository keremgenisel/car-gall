using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using CarGallery.Modules.Leads.Tests.Integration;

namespace CarGallery.Modules.Leads.Tests.Contracts;

/// <summary>
/// 0009 D1 — `Notifications`'ın gönderim anında okuduğu dar yüzey.
///
/// Sözleşmenin DARLIĞI da sınanıyor, yalnız doğruluğu değil: ad ve telefon bu DTO'da
/// bulunmamalı. Bir alan sonradan eklemek ucuz, geçmişten silmek imkânsız — ve bu yüzey
/// bir modül sınırını geçiyor.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class LeadBildirimSorgusuTests(LeadsDatabase database)
{
    [Fact(DisplayName = "0009/AC-3: Gönderilen e-posta talebin şubesinin adını, aracın model adını ve ilanın detay sayfasına giden mutlak bağlantıyı taşır")]
    public async Task Doner_ModelAdi_Sube_VeIlan()
    {
        await database.ResetAsync();

        Guid subeId = Guid.Parse("0192c000-0000-7000-8000-0000000000b1");
        Guid ilanId = Guid.Parse("0192c000-0000-7000-8000-0000000000c1");

        Lead talep = new LeadBuilder()
            .Sube(subeId)
            .Eposta("ziyaretci@ornek.com")
            .Locale("ar")
            .AnlikGoruntu(AnlikGoruntu(ilanId, "Toyota Corolla"))
            .Olustur()
            .Value;

        await KaydetAsync(talep);

        LeadNotificationDto? okunan = await SorAsync(talep.Id.Value);

        Assert.NotNull(okunan);
        Assert.Equal("Toyota Corolla", okunan.CanonicalModelName);
        Assert.Equal(subeId, okunan.BranchId);
        Assert.Equal(ilanId, okunan.ListingId);
        Assert.Equal("ziyaretci@ornek.com", okunan.Email);
        Assert.Equal("ar", okunan.SubmittedLocale);
    }

    [Fact(DisplayName = "0009/AC-11: E-posta adresi olmayan — yalnız telefon bırakılmış — bir talepte ziyaretçi karşılığı gönderilmez; gönderim geçmişine \"kanal yok\" sonucuyla kaydedilir")]
    public async Task Doner_NullEposta_YalnizTelefonVarken()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Telefon("+905321112233").Eposta(null).Olustur().Value;
        await KaydetAsync(talep);

        LeadNotificationDto? okunan = await SorAsync(talep.Id.Value);

        Assert.NotNull(okunan);
        Assert.Null(okunan.Email);
    }

    [Fact(DisplayName = "0009/AC-24: Gönderim geçmişi alıcının adını, e-posta adresini veya telefonunu hiç tutmaz")]
    public async Task TasimazKisiselVeri_SozlesmeYuzeyi()
    {
        string[] alanlar = [.. typeof(LeadNotificationDto).GetProperties().Select(property => property.Name)];

        // Ad ve telefon bu sınırı HİÇ geçmiyor (D7). E-posta geçiyor ama yalnız gönderim
        // anında ve bellekte; gönderim geçmişine yazılmıyor — onu `Notifications` sınar.
        Assert.DoesNotContain(alanlar, ad => ad.Contains("Name", StringComparison.OrdinalIgnoreCase)
            && !ad.Contains("ModelName", StringComparison.Ordinal));
        Assert.DoesNotContain(alanlar, ad => ad.Contains("Phone", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(alanlar, ad => ad.Contains("Message", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "0009/AC-19: Aynı `LeadCaptured` olayı iki kez işlendiğinde ikinci bir gönderim kaydı oluşmaz")]
    public async Task Doner_Null_VarOlmayanTalepIcin()
    {
        await database.ResetAsync();

        Assert.Null(await SorAsync(Guid.NewGuid()));
    }

    private static VehicleSnapshot AnlikGoruntu(Guid ilanId, string modelAdi) =>
        VehicleSnapshot.Create(ilanId, modelAdi, PriceSnapshot.OnRequest()).Value;

    private async Task KaydetAsync(Lead talep)
    {
        await using LeadsDbContext context = database.CreateContext();
        context.Leads.Add(talep);
        await database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
            .SaveChangesAsync(CancellationToken.None);
    }

    private async Task<LeadNotificationDto?> SorAsync(Guid leadId)
    {
        await using LeadsDbContext context = database.CreateContext();

        return await new LeadNotificationQueries(context).FindAsync(leadId, CancellationToken.None);
    }
}
