using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Kuyruğun işlenmesi: alıcı çözümü, gövde üretimi, tekrar deneme ve hata sınıfları.
///
/// Taşıyıcı sahtelenir — gerçek SMTP'ye karşı koşan tek test ayrıdır (D6). Buradaki
/// testler sağlayıcının davranışını değil, KUYRUĞUN davranışını ölçüyor.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class GonderimTests(NotificationsDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TalepId = Guid.Parse("0192c000-0000-7000-8000-00000000d001");
    private static readonly Guid SubeId = Guid.Parse("0192c000-0000-7000-8000-00000000b001");
    private static readonly Guid IlanId = Guid.Parse("0192c000-0000-7000-8000-00000000c001");

    [Fact(DisplayName = "0009/AC-2: Bir talep oluşturulduğunda galeri tarafına bir e-posta gönderilir")]
    public async Task Gonderilir_GaleriBildirimi()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici);

        Assert.Single(tasiyici.Gonderilenler);
        Assert.Equal(NotificationStatus.Sent, (await MesajlarAsync())[0].Status);
    }

    [Fact(DisplayName = "0009/AC-3: Gönderilen e-posta talebin şubesinin adını, aracın model adını ve ilanın detay sayfasına giden mutlak bağlantıyı taşır")]
    public async Task Tasir_SubeAdi_ModelAdi_VeMutlakBaglanti()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici);

        OutboundMessage mesaj = tasiyici.Gonderilenler[0];

        Assert.Contains("Toyota Corolla", mesaj.Body, StringComparison.Ordinal);
        Assert.Contains("Kadıköy Şubesi", mesaj.Body, StringComparison.Ordinal);

        // MUTLAK bağlantı: göreli bir adres e-postada tıklanamaz.
        Assert.Contains("https://ornek.com/tr/cars/toyota-corolla-2019-abc123", mesaj.Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0009/AC-4: Şubenin e-posta adresi varsa bildirim o adrese gider")]
    public async Task Gider_SubeninAdresine()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici);

        Assert.Equal("kadikoy@ornek.com", tasiyici.Gonderilenler[0].Recipient);
    }

    [Fact(DisplayName = "0009/AC-5: Şubenin e-posta adresi yoksa bildirim kurulum bildirim adresine gider")]
    public async Task Gider_KurulumAdresine_SubeEpostasiYokken()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici, sube: new BranchNotificationContactDto(SubeId, "Kadıköy Şubesi", null));

        Assert.Equal("galeri@ornek.com", tasiyici.Gonderilenler[0].Recipient);
    }

    [Fact(DisplayName = "0009/AC-7: Ziyaretçiye talebinin alındığını bildiren bir e-posta gönderilir")]
    public async Task Gonderilir_ZiyaretciKarsiligi()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedVisitor, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici);

        Assert.Equal("ziyaretci@ornek.com", tasiyici.Gonderilenler[0].Recipient);
    }

    [Fact(DisplayName = "0009/AC-8: Bu e-posta ziyaretçinin talebi bıraktığı dilde gönderilir; Arapça bırakılan talebin karşılığı Arapça gider")]
    public async Task Gonderilir_ArapcaKarsilik()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedVisitor, "ar");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici, talep: Talep(locale: "ar"));

        Assert.Contains("وصل طلبك", tasiyici.Gonderilenler[0].Body, StringComparison.Ordinal);
    }

    /// <summary>
    /// R13/L7 — izin PAZARLAMA mesajı içindir; talebin kendisine dair işlemsel karşılık
    /// izinden bağımsız gider. Bu spec'te pazarlama şablonu zaten yok (X2).
    /// </summary>
    [Fact(DisplayName = "0009/AC-10: `ConsentGiven` false olan ziyaretçiye de bu işlemsel karşılık gönderilir")]
    public async Task Gonderilir_IzinOlmasaDa()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedVisitor, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici);

        // `ConsentGiven` bu yolun hiçbir yerinde okunmuyor: işlemsel mesaj izne bağlı değil.
        Assert.Single(tasiyici.Gonderilenler);
    }

    [Fact(DisplayName = "0009/AC-12: Ziyaretçi karşılığının başarısız olması galeri bildirimini engellemez; galeri bildiriminin başarısız olması da ziyaretçi karşılığını engellemez")]
    public async Task Engellemez_BirininBasarisizligi_Digerini()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");
        await KuyrugaAsync(TemplateKeys.LeadReceivedVisitor, "tr");

        // Galeri bildirimi kalıcı hata alıyor; ziyaretçi karşılığı başarılı olmalı.
        SahteTasiyici tasiyici = new()
        {
            Kural = mesaj => mesaj.Recipient == "kadikoy@ornek.com"
                ? SendResult.Permanent("x")
                : SendResult.Success(),
        };

        await IsleAsync(tasiyici);

        List<NotificationMessage> mesajlar = await MesajlarAsync();

        Assert.Equal(
            NotificationStatus.Failed,
            mesajlar.Single(m => m.TemplateKey == TemplateKeys.LeadReceivedGallery).Status);
        Assert.Equal(
            NotificationStatus.Sent,
            mesajlar.Single(m => m.TemplateKey == TemplateKeys.LeadReceivedVisitor).Status);
    }

    /// <summary>
    /// AC-14'ün ölçülebilir anlamı: durum SÜREÇTE değil VERİTABANINDA. Yeni bir dispatcher
    /// örneği — yeni bağlam, yeni taşıyıcı — bekleyen satırı bulup işler. Gerçek bir süreç
    /// yeniden başlatmak Host ölçeğinde bir iştir ve aynı şeyi kanıtlamaz.
    /// </summary>
    [Fact(DisplayName = "0009/AC-14: Bekleyen gönderimler süreç yeniden başladıktan sonra kaybolmaz ve işlenmeye devam eder")]
    public async Task Kaybolmaz_BekleyenGonderim()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        // Birinci tur: taşıyıcı geçici hata veriyor, mesaj kuyrukta kalıyor.
        await IsleAsync(new SahteTasiyici { Kural = _ => SendResult.Transient("x") });

        NotificationMessage bekleyen = (await MesajlarAsync())[0];
        Assert.Equal(NotificationStatus.Queued, bekleyen.Status);

        // İkinci tur: YENİ dispatcher örneği, sıradaki denemenin zamanında.
        SahteTasiyici ikinci = new();
        await IsleAsync(ikinci, simdi: bekleyen.NextAttemptAt!.Value);

        Assert.Single(ikinci.Gonderilenler);
        Assert.Equal(NotificationStatus.Sent, (await MesajlarAsync())[0].Status);
    }

    [Fact(DisplayName = "0009/AC-15: Geçici bir hatada gönderim yapılandırılan sayıda yeniden denenir ve denemeler arasındaki aralık artar")]
    public async Task Denenir_YapilandirilanSayida_VeAralikArtar()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        DateTimeOffset simdi = An;
        List<TimeSpan> aralıklar = [];

        for (int deneme = 0; deneme < 2; deneme++)
        {
            await IsleAsync(new SahteTasiyici { Kural = _ => SendResult.Transient("x") }, simdi: simdi);

            NotificationMessage mesaj = (await MesajlarAsync())[0];
            aralıklar.Add(mesaj.NextAttemptAt!.Value - simdi);
            simdi = mesaj.NextAttemptAt.Value;
        }

        Assert.True(aralıklar[1] > aralıklar[0], "aralık artmadı");
    }

    [Fact(DisplayName = "0009/AC-16: Denemeler tükendiğinde kayıt başarısız olarak işaretlenir ve loglanır; sessizce düşmez")]
    public async Task Isaretlenir_Basarisiz_DenemelerTukendiginde()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        DateTimeOffset simdi = An;

        // Yapılandırılan sayı 3; üç turda tükenmeli.
        for (int deneme = 0; deneme < 3; deneme++)
        {
            await IsleAsync(new SahteTasiyici { Kural = _ => SendResult.Transient("x") }, simdi: simdi);

            NotificationMessage mesaj = (await MesajlarAsync())[0];
            simdi = mesaj.NextAttemptAt ?? simdi;
        }

        NotificationMessage son = (await MesajlarAsync())[0];

        Assert.Equal(NotificationStatus.Failed, son.Status);
        Assert.Equal(3, son.AttemptCount);
        Assert.NotNull(son.FailureCode);
    }

    [Fact(DisplayName = "0009/AC-17: Kalıcı bir hatada — alıcı adresi reddedildiğinde — yeniden deneme yapılmaz; kayıt derhal başarısız işaretlenir")]
    public async Task Denenmez_KaliciHatada()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        await IsleAsync(new SahteTasiyici { Kural = _ => SendResult.Permanent("adres reddedildi") });

        NotificationMessage mesaj = (await MesajlarAsync())[0];

        Assert.Equal(NotificationStatus.Failed, mesaj.Status);
        Assert.Equal(1, mesaj.AttemptCount);
        Assert.Null(mesaj.NextAttemptAt);
    }

    /// <summary>
    /// Taşıyıcının kendi beklemesine güvenilmez: sağlayıcı bağlantıyı açık tutup hiç cevap
    /// vermeyebilir ve kuyruk tek bir mesajda süresiz asılı kalırdı. Test gerçek saate
    /// değil YAPILANDIRILAN değere bakıyor (C9).
    /// </summary>
    [Fact(DisplayName = "0009/AC-18: Gönderim yapılandırılan zaman aşımını aştığında geçici hata sayılır ve yeniden denenir")]
    public async Task Gecici_Sayilir_ZamanAsimi()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        await IsleAsync(new AskidaTasiyici(), zamanAsimiSaniye: 1);

        NotificationMessage mesaj = (await MesajlarAsync())[0];

        Assert.Equal(NotificationStatus.Queued, mesaj.Status);
        Assert.Equal(NotificationErrors.SendTimedOut, mesaj.FailureCode);
        Assert.NotNull(mesaj.NextAttemptAt);
    }

    [Fact(DisplayName = "0009/AC-32: İlanın slug'ı çözülemediğinde — ilan kaldırılmışsa — galeri bildirimi bağlantısız gönderilir ve gönderim başarısız sayılmaz")]
    public async Task Gonderilir_Baglantisiz_SlugCozulemezse()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync(TemplateKeys.LeadReceivedGallery, "tr");

        SahteTasiyici tasiyici = new();
        await IsleAsync(tasiyici, slug: null);

        OutboundMessage mesaj = tasiyici.Gonderilenler[0];

        // Bağlantı satırı tamamen düştü: sarkan bir "İlan:" etiketi kalmadı.
        Assert.DoesNotContain("https://", mesaj.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("İlan:", mesaj.Body, StringComparison.Ordinal);

        // Ve gönderim BAŞARISIZ SAYILMADI: galeri yine de talepten haberdar oldu.
        Assert.Contains("Toyota Corolla", mesaj.Body, StringComparison.Ordinal);
        Assert.Equal(NotificationStatus.Sent, (await MesajlarAsync())[0].Status);
    }

    private async Task KuyrugaAsync(string templateKey, string locale)
    {
        await using NotificationsDbContext context = database.CreateContext();

        context.Messages.Add(NotificationMessage.Queue(
            TalepId, templateKey, locale, NotificationChannel.Email, An));

        await context.SaveChangesAsync();
    }

    private async Task IsleAsync(
        INotificationTransport tasiyici,
        LeadNotificationDto? talep = null,
        BranchNotificationContactDto? sube = null,
        string? slug = "toyota-corolla-2019-abc123",
        DateTimeOffset? simdi = null,
        int zamanAsimiSaniye = 30)
    {
        await using NotificationsDbContext context = database.CreateContext();

        NotificationDispatcher dispatcher = new(
            context,
            tasiyici,
            new TemplateResolver(context, NullLogger<TemplateResolver>.Instance),
            new SahteTalepSorgusu(talep ?? Talep()),
            new SahteSubeSorgusu(sube ?? new BranchNotificationContactDto(SubeId, "Kadıköy Şubesi", "kadikoy@ornek.com")),
            new SahteSlugSorgusu(slug),
            Secenekler(zamanAsimiSaniye),
            new SabitSaat(simdi ?? An),
            NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchDueAsync(CancellationToken.None);
    }

    private static LeadNotificationDto Talep(string locale = "tr") =>
        new(TalepId, "ziyaretci@ornek.com", locale, "Toyota Corolla", SubeId, IlanId);

    private static NotificationsOptions Secenekler(int zamanAsimiSaniye) =>
        NotificationsOptions.Create(new NotificationsSetup(
            ConnectionString: "yok",
            DefaultLocale: "tr",
            SetupNotificationAddress: "galeri@ornek.com",
            PublicBaseUrl: "https://ornek.com",
            SenderAddress: "bildirim@ornek.com",
            SmtpHost: "localhost",
            SmtpPort: 25,
            SmtpUser: "kullanici",
            SmtpPassword: "sir",
            SmtpUseStartTls: false,
            MaximumAttempts: 3,
            RetryBaseSeconds: 10,
            SendTimeoutSeconds: zamanAsimiSaniye)).Value;

    private async Task<List<NotificationMessage>> MesajlarAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages.AsNoTracking().OrderBy(message => message.TemplateKey).ToListAsync();
    }

    private sealed class SahteTasiyici : INotificationTransport
    {
        internal List<OutboundMessage> Gonderilenler { get; } = [];

        internal Func<OutboundMessage, SendResult> Kural { get; init; } = _ => SendResult.Success();

        public Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            SendResult sonuc = Kural(message);

            if (sonuc.Succeeded)
            {
                Gonderilenler.Add(message);
            }

            return Task.FromResult(sonuc);
        }
    }

    /// <summary>Hiç cevap vermeyen sağlayıcı: zaman aşımı dışında dönüşü yok.</summary>
    private sealed class AskidaTasiyici : INotificationTransport
    {
        public async Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return SendResult.Success();
        }
    }

    private sealed class SahteTalepSorgusu(LeadNotificationDto? talep) : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult(talep);
    }

    private sealed class SahteSubeSorgusu(BranchNotificationContactDto? sube) : IBranchContactQueries
    {
        public Task<BranchNotificationContactDto?> FindContactAsync(
            Guid branchId,
            string locale,
            CancellationToken cancellationToken) => Task.FromResult(sube);
    }

    private sealed class SahteSlugSorgusu(string? slug) : IListingSlugQueries
    {
        public Task<string?> FindSlugAsync(Guid listingId, CancellationToken cancellationToken) =>
            Task.FromResult(slug);
    }

    private sealed class SabitSaat(DateTimeOffset an) : IClock
    {
        public DateTimeOffset UtcNow { get; } = an;
    }
}
