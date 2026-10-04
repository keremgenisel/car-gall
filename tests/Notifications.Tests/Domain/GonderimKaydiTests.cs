using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;

namespace CarGallery.Modules.Notifications.Tests.Domain;

/// <summary>
/// Kuyruk satırının durum geçişleri. Veritabanı yok, sahte yok — saf domain.
///
/// Bunlar da henüz KRİTER testi değil: kriterlerin tarif ettiği gönderim davranışı
/// dispatcher ile gelecek. Buradaki testler yalnız agregatın kendi kurallarını koruyor.
/// </summary>
public sealed class GonderimKaydiTests
{
    private static readonly DateTimeOffset An = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private const int Esik = 3;

    private static readonly TimeSpan Aralik = TimeSpan.FromSeconds(10);

    [Fact]
    public void Artan_Aralik_GeciciHatada()
    {
        NotificationMessage mesaj = Kuyruga();

        Assert.True(mesaj.Fail(SendFailureKind.Transient, "x", Esik, Aralik, An).IsSuccess);
        DateTimeOffset birinci = mesaj.NextAttemptAt!.Value;

        Assert.True(mesaj.Fail(SendFailureKind.Transient, "x", Esik, Aralik, An).IsSuccess);
        DateTimeOffset ikinci = mesaj.NextAttemptAt!.Value;

        // Sabit aralık, geçici bir kesintide bütün kuyruğu aynı anda sağlayıcıya yığardı.
        Assert.True(ikinci - An > birinci - An);
    }

    [Fact]
    public void Isaretlenir_Basarisiz_DenemelerTukendiginde()
    {
        NotificationMessage mesaj = Kuyruga();

        for (int deneme = 0; deneme < Esik; deneme++)
        {
            Assert.True(mesaj.Fail(SendFailureKind.Transient, "x", Esik, Aralik, An).IsSuccess);
        }

        Assert.Equal(NotificationStatus.Failed, mesaj.Status);
        Assert.Null(mesaj.NextAttemptAt);
    }

    [Fact]
    public void Denenmez_KaliciHatada()
    {
        NotificationMessage mesaj = Kuyruga();

        Assert.True(mesaj.Fail(SendFailureKind.Permanent, "x", Esik, Aralik, An).IsSuccess);

        // Geçersiz bir adrese beş kez denemek kuyruğu kalıcı hatalarla doldururdu.
        Assert.Equal(NotificationStatus.Failed, mesaj.Status);
        Assert.Equal(1, mesaj.AttemptCount);
        Assert.Null(mesaj.NextAttemptAt);
    }

    /// <summary>
    /// Koruma olmasaydı geç gelen bir sonuç `CompletedAt`'ı ezer, `AttemptCount`'u artırır
    /// ve geçmiş sessizce yalan söylerdi.
    /// </summary>
    [Fact]
    public void Reddedilir_TamamlanmisMesajinYenidenIsaretlenmesi()
    {
        NotificationMessage gonderilen = Kuyruga();
        Assert.True(gonderilen.MarkSent(An).IsSuccess);

        Result tekrar = gonderilen.MarkSent(An.AddMinutes(1));
        Result sonraGelenHata = gonderilen.Fail(SendFailureKind.Transient, "x", Esik, Aralik, An);

        Assert.True(tekrar.IsFailure);
        Assert.Equal(NotificationErrors.AlreadyCompleted, tekrar.Error.Code);
        Assert.True(sonraGelenHata.IsFailure);

        // Durum ve sayaç korundu.
        Assert.Equal(NotificationStatus.Sent, gonderilen.Status);
        Assert.Equal(0, gonderilen.AttemptCount);
        Assert.Equal(An, gonderilen.CompletedAt);
    }

    [Fact]
    public void Reddedilir_KanalsizMesajinIsaretlenmesi()
    {
        NotificationMessage kanalsiz = NotificationMessage.NoChannel(
            Guid.CreateVersion7(), TemplateKeys.LeadReceivedVisitor, "tr", NotificationChannel.Email, An);

        Assert.Equal(NotificationStatus.NoChannel, kanalsiz.Status);
        Assert.True(kanalsiz.MarkSent(An).IsFailure);
    }

    private static NotificationMessage Kuyruga() => NotificationMessage.Queue(
        Guid.CreateVersion7(), TemplateKeys.LeadReceivedGallery, "tr", NotificationChannel.Email, An);
}
