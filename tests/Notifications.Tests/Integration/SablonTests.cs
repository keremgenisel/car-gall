using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Şablon çözümü ve dil düşüşü (R9). Şablonlar seed migration'dan geliyor; testler
/// migration'ın gerçekten koştuğunu da doğruluyor.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class SablonTests(NotificationsDatabase database)
{
    [Fact(DisplayName = "0009/AC-22: Şablon metinleri TR, AR ve EN üçünde de vardır; eksik anahtar kontrolü backend tarafında bir testle doğrulanır")]
    public async Task Vardir_UcDilde_HerAnahtar()
    {
        await using NotificationsDbContext context = database.CreateContext();

        // C13 — frontend'in `check:locales` kapısı bu şablonları GÖRMEZ; kapı burada.
        foreach (string anahtar in TemplateKeys.All)
        {
            foreach (string locale in new[] { "tr", "ar", "en" })
            {
                NotificationTemplate? sablon = await context.Templates
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.TemplateKey == anahtar && item.Locale == locale);

                Assert.True(sablon is not null, $"eksik şablon: {anahtar} / {locale}");
                Assert.False(string.IsNullOrWhiteSpace(sablon.Subject), $"boş konu: {anahtar} / {locale}");
                Assert.False(string.IsNullOrWhiteSpace(sablon.Body), $"boş gövde: {anahtar} / {locale}");
            }
        }
    }

    /// <summary>
    /// R4/AC-9 — 0005 Karar D6 ulaşma sözünü yasaklıyor ve orada bir kez gerçekten
    /// çiğnenmişti; bu yüzden kalıp taraması üç dilde birden koşuyor.
    /// </summary>
    [Fact(DisplayName = "0009/AC-9: E-posta hiçbir dilde dönüş süresi taahhüdü içermez")]
    public async Task IcermezDonusTaahhudu_HicbirDilde()
    {
        await using NotificationsDbContext context = database.CreateContext();

        string[] kaliplar =
        [
            "ulaşacak", "döneceğ", "dönecek", "en kısa sürede", "24 saat", "size dön",
            "will get back", "will contact", "shortly", "within",
            "سيتواصل", "سنتواصل", "خلال",
        ];

        List<NotificationTemplate> sablonlar = await context.Templates.AsNoTracking().ToListAsync();

        Assert.NotEmpty(sablonlar);

        foreach (NotificationTemplate sablon in sablonlar)
        {
            foreach (string kalip in kaliplar)
            {
                Assert.False(
                    sablon.Body.Contains(kalip, StringComparison.OrdinalIgnoreCase)
                    || sablon.Subject.Contains(kalip, StringComparison.OrdinalIgnoreCase),
                    $"taahhüt kalıbı '{kalip}' bulundu: {sablon.TemplateKey} / {sablon.Locale}");
            }
        }
    }

    [Fact(DisplayName = "0009/AC-20: İstenen dilde şablon yoksa varsayılan dile düşülür ve bu durum loglanır")]
    public async Task Duser_VarsayilanDile_IstenenDilYokken()
    {
        await using NotificationsDbContext context = database.CreateContext();

        Result<ResolvedTemplate> sonuc = await Cozucu(context)
            .ResolveAsync(TemplateKeys.LeadReceivedVisitor, "de", "tr", CancellationToken.None);

        Assert.True(sonuc.IsSuccess);

        // Çözülen locale, İSTENEN değil KULLANILAN olmalı.
        Assert.Equal("tr", sonuc.Value.Locale);
        Assert.False(string.IsNullOrWhiteSpace(sonuc.Value.Body));
    }

    [Fact(DisplayName = "0009/AC-20: İstenen dilde şablon yoksa varsayılan dile düşülür ve bu durum loglanır")]
    public async Task Dusmez_IstenenDilVarken()
    {
        await using NotificationsDbContext context = database.CreateContext();

        Result<ResolvedTemplate> sonuc = await Cozucu(context)
            .ResolveAsync(TemplateKeys.LeadReceivedVisitor, "ar", "tr", CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal("ar", sonuc.Value.Locale);
    }

    [Fact(DisplayName = "0009/AC-21: Varsayılan dilde de şablon yoksa gönderim başarısız işaretlenir; ham şablon anahtarı veya boş gövde gönderilmez")]
    public async Task Basarisiz_VarsayilanDaYokken()
    {
        await using NotificationsDbContext context = database.CreateContext();

        Result<ResolvedTemplate> sonuc = await Cozucu(context)
            .ResolveAsync("hic.olmayan.anahtar", "tr", "tr", CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(NotificationErrors.TemplateMissing, sonuc.Error.Code);
    }

    /// <summary>
    /// Satırın VAR OLMASI yetmez: boş gövdeli bir şablon ziyaretçiye boş e-posta göndermek
    /// demektir ve R9 bunu açıkça yasaklıyor.
    /// </summary>
    [Fact(DisplayName = "0009/AC-21: Varsayılan dilde de şablon yoksa gönderim başarısız işaretlenir; ham şablon anahtarı veya boş gövde gönderilmez")]
    public async Task Basarisiz_BosGovdeliSablonda()
    {
        await using NotificationsDbContext context = database.CreateContext();

        context.Templates.Add(new NotificationTemplate
        {
            TemplateKey = "bos.sablon",
            Locale = "tr",
            Subject = "Konu",
            Body = "   ",
        });
        await context.SaveChangesAsync();

        try
        {
            Result<ResolvedTemplate> sonuc = await Cozucu(context)
                .ResolveAsync("bos.sablon", "tr", "tr", CancellationToken.None);

            Assert.True(sonuc.IsFailure);
            Assert.Equal(NotificationErrors.TemplateMissing, sonuc.Error.Code);
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {NotificationsDbContext.Schema}.templates WHERE template_key = 'bos.sablon'");
        }
    }

    private static TemplateResolver Cozucu(NotificationsDbContext context) =>
        new(context, NullLogger<TemplateResolver>.Instance);
}
