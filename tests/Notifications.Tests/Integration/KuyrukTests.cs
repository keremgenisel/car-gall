using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Olay geldiğinde iki mesaj KUYRUĞA yazılır; gönderim yapılmaz (C10, AC-13).
///
/// `Leads` bu test projesinin sınırının ötesindedir (docs/testing.md § 1) ve sahtelenir:
/// burada sınanan şey talebin nasıl okunduğu değil, okunanla ne yapıldığıdır.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class KuyrukTests(NotificationsDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TalepId = Guid.Parse("0192c000-0000-7000-8000-00000000d001");

    [Fact(DisplayName = "0009/AC-13: Mesaj önce kuyruğa kaydedilir, sonra gönderilir, sonra sonucu işaretlenir")]
    public async Task Kaydedilir_Once_IkiMesaj()
    {
        await database.ResetMessagesAsync();

        await IsleAsync(Talep("ziyaretci@ornek.com"));

        List<NotificationMessage> mesajlar = await MesajlarAsync();

        Assert.Equal(2, mesajlar.Count);

        // Gönderim YAPILMADI: ikisi de sırada bekliyor. Gönderim arka plan servisinin işi.
        Assert.All(mesajlar, mesaj => Assert.Equal(NotificationStatus.Queued, mesaj.Status));
        Assert.All(mesajlar, mesaj => Assert.NotNull(mesaj.NextAttemptAt));
        Assert.All(mesajlar, mesaj => Assert.Null(mesaj.CompletedAt));
    }

    [Fact(DisplayName = "0009/AC-6: Galeri bildirimi kurulum varsayılan dilinde gönderilir")]
    public async Task Kuyruga_VarsayilanDille_GaleriBildirimi()
    {
        await database.ResetMessagesAsync();

        // Talep ARAPÇA bırakıldı; galeri bildirimi yine de kurulum varsayılanında olmalı:
        // alıcı bir adrestir, bir kişi değil (R5).
        await IsleAsync(Talep("ziyaretci@ornek.com", locale: "ar"));

        NotificationMessage galeri = await MesajAsync(TemplateKeys.LeadReceivedGallery);

        Assert.Equal("tr", galeri.Locale);
    }

    [Fact(DisplayName = "0009/AC-8: Bu e-posta ziyaretçinin talebi bıraktığı dilde gönderilir; Arapça bırakılan talebin karşılığı Arapça gider")]
    public async Task Kuyruga_BirakilanDille_ZiyaretciKarsiligi()
    {
        await database.ResetMessagesAsync();

        await IsleAsync(Talep("ziyaretci@ornek.com", locale: "ar"));

        NotificationMessage ziyaretci = await MesajAsync(TemplateKeys.LeadReceivedVisitor);

        Assert.Equal("ar", ziyaretci.Locale);
    }

    /// <summary>
    /// R6 — bu bir hata değil, eksik kanaldır. `Failed` yazmak galeriye yanlış bir şey
    /// söylerdi; kaydı hiç tutmamak ise "neden ulaşılmadı" sorusunu cevapsız bırakırdı.
    /// </summary>
    [Fact(DisplayName = "0009/AC-11: E-posta adresi olmayan — yalnız telefon bırakılmış — bir talepte ziyaretçi karşılığı gönderilmez; gönderim geçmişine \"kanal yok\" sonucuyla kaydedilir")]
    public async Task Kaydedilir_KanalYok_EpostaYokken()
    {
        await database.ResetMessagesAsync();

        await IsleAsync(Talep(email: null));

        NotificationMessage ziyaretci = await MesajAsync(TemplateKeys.LeadReceivedVisitor);

        Assert.Equal(NotificationStatus.NoChannel, ziyaretci.Status);
        Assert.Null(ziyaretci.NextAttemptAt);
        Assert.Equal(An, ziyaretci.CompletedAt);

        // Galeri bildirimi NORMAL gider: ziyaretçinin kanalı yok diye galeri habersiz kalmaz.
        NotificationMessage galeri = await MesajAsync(TemplateKeys.LeadReceivedGallery);
        Assert.Equal(NotificationStatus.Queued, galeri.Status);
    }

    [Fact(DisplayName = "0009/AC-19: Aynı `LeadCaptured` olayı iki kez işlendiğinde ikinci bir gönderim kaydı oluşmaz")]
    public async Task Olusmaz_IkinciKayit_AyniOlayIkiKez()
    {
        await database.ResetMessagesAsync();

        LeadCaptured olay = Olay();

        await IsleAsync(Talep("ziyaretci@ornek.com"), olay);
        await IsleAsync(Talep("ziyaretci@ornek.com"), olay);

        // Vaat edilen şey tam-bir-kez TESLİM değil (C10); aynı olayın ikinci işlenmesinde
        // ikinci bir KAYIT oluşmaması.
        Assert.Equal(2, (await MesajlarAsync()).Count);
    }

    [Fact(DisplayName = "0009/AC-23: Gönderim geçmişi her mesaj için `LeadId`, şablon anahtarı, locale, kanal, sonuç ve zaman tutar")]
    public async Task Tutar_AlanlariniKayit()
    {
        await database.ResetMessagesAsync();

        await IsleAsync(Talep("ziyaretci@ornek.com"));

        NotificationMessage mesaj = await MesajAsync(TemplateKeys.LeadReceivedGallery);

        Assert.Equal(TalepId, mesaj.LeadId);
        Assert.Equal(TemplateKeys.LeadReceivedGallery, mesaj.TemplateKey);
        Assert.Equal("tr", mesaj.Locale);
        Assert.Equal(NotificationChannel.Email, mesaj.Channel);
        Assert.Equal(NotificationStatus.Queued, mesaj.Status);
        Assert.Equal(An, mesaj.QueuedAt);
    }

    /// <summary>
    /// Talep silinmiş veya anonimleştirilmişse gönderilecek bir şey yoktur; işleyici bunu
    /// bir hata değil, işlenecek bir şey kalmadığı biçiminde okur.
    /// </summary>
    [Fact]
    public async Task Kaydedilmez_TalepBulunamazsa()
    {
        await database.ResetMessagesAsync();

        await IsleAsync(talep: null);

        Assert.Empty(await MesajlarAsync());
    }

    private static LeadCaptured Olay() => new(
        Guid.Parse("0192c000-0000-7000-8000-00000000e001"),
        An,
        TalepId,
        Guid.Parse("0192c000-0000-7000-8000-00000000b001"),
        Guid.Parse("0192c000-0000-7000-8000-00000000c001"),
        "InformationRequest",
        "tr");

    private static LeadNotificationDto Talep(string? email, string locale = "tr") => new(
        TalepId,
        email,
        locale,
        "Toyota Corolla",
        Guid.Parse("0192c000-0000-7000-8000-00000000b001"),
        Guid.Parse("0192c000-0000-7000-8000-00000000c001"));

    private async Task IsleAsync(LeadNotificationDto? talep, LeadCaptured? olay = null)
    {
        await using NotificationsDbContext context = database.CreateContext();

        LeadCapturedNotificationHandler isleyici = new(
            context,
            new SabitSaat(An),
            new SahteTalepSorgusu(talep),
            Secenekler(),
            NullLogger<LeadCapturedNotificationHandler>.Instance);

        await isleyici.HandleAsync(olay ?? Olay(), CancellationToken.None);
    }

    private static NotificationsOptions Secenekler() => NotificationsOptions.Create(new NotificationsSetup(
        ConnectionString: "yok",
        DefaultLocale: "tr",
        SetupNotificationAddress: "galeri@ornek.com",
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: "bildirim@ornek.com",
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 10)).Value;

    private async Task<List<NotificationMessage>> MesajlarAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages.AsNoTracking().ToListAsync();
    }

    private async Task<NotificationMessage> MesajAsync(string templateKey)
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages
            .AsNoTracking()
            .SingleAsync(message => message.TemplateKey == templateKey);
    }

    private sealed class SahteTalepSorgusu(LeadNotificationDto? talep) : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult(talep);
    }

    private sealed class SabitSaat(DateTimeOffset an) : IClock
    {
        public DateTimeOffset UtcNow { get; } = an;
    }
}
