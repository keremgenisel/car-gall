using System.Net.Sockets;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Domain;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>
/// D6 — SMTP'yi konuşan tek sınıf. Portun arkasında durur (<see cref="INotificationTransport"/>);
/// sağlayıcı değişirse değişen yer YALNIZ burasıdır.
///
/// <para><b>Bu sınıf tekrar denemez.</b> Tekrar deneme kuyruğun işi (R10): süreç çökse de
/// yaşayan bir kayıt gerekir, taşıyıcının içindeki bir döngü çökümde buharlaşır. Buradaki
/// tek sorumluluk, başarısızlığı <b>sınıflandırmaktır</b> (R11) — çünkü sınıfı yalnız SMTP
/// cevabını gören taraf bilebilir.</para>
///
/// <para><b>Zaman aşımını da burası KOYMAZ</b> (AC-18): çağıran bir
/// <c>CancellationToken</c> ile sınırlar. Sağlayıcı bağlantıyı açık tutup hiç cevap
/// vermeyebilir ve MailKit'in kendi <c>Timeout</c>'u soket seviyesindedir — protokol
/// seviyesinde asılı kalan bir oturumu kesmez.</para>
///
/// <para><b>Her gönderim yeni bir bağlantı açar.</b> Bağlantı havuzlamak daha hızlı olurdu
/// ama paylaşılan bir <c>SmtpClient</c> iş parçacığı güvenli değildir ve 0007'de öğrenilen
/// ders tam olarak buydu: paylaşılan durum, iki eşzamanlı yazanı olduğu an bozulur. Kuyruk
/// turda en çok elli mesaj işliyor; buradaki maliyet ölçülebilir bir sorun olmadan
/// değiştirilmeyecek.</para>
/// </summary>
internal sealed class SmtpNotificationTransport(
    NotificationsOptions options,
    ILogger<SmtpNotificationTransport> logger) : INotificationTransport
{
    public async Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        MimeMessage mime = Compose(message);

        using SmtpClient client = new();

        try
        {
            await client.ConnectAsync(
                options.SmtpHost,
                options.SmtpPort,
                options.SmtpUseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            await client.AuthenticateAsync(options.SmtpUser, options.SmtpPassword, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            return SendResult.Success();
        }
        catch (SmtpCommandException exception)
        {
            // Sağlayıcının kendi cevabı: `5xx` kalıcı, `4xx` geçici. SMTP'nin bu ayrımı
            // RFC 5321 § 4.2.1'de tanımlıdır ve tahmin edilmesi gerekmez.
            bool permanent = (int)exception.StatusCode >= 500;

            logger.LogWarning(
                exception,
                "SMTP sağlayıcısı mesajı reddetti: {StatusCode} {ErrorCode} {Permanent}",
                (int)exception.StatusCode,
                exception.ErrorCode,
                permanent);

            return permanent
                ? SendResult.Permanent(NotificationErrors.RecipientRejected)
                : SendResult.Transient(NotificationErrors.ProviderUnavailable);
        }
        catch (AuthenticationException exception)
        {
            // Kimlik bilgisi yanlış: yeniden denemek düzeltmez ama bu bir YAPILANDIRMA
            // hatasıdır, alıcının suçu değil — ayrıca loglanır ki galeri mesajın neden
            // gitmediğini adres sanmasın.
            logger.LogError(exception, "SMTP kimlik doğrulaması başarısız: {Host}", options.SmtpHost);

            return SendResult.Permanent(NotificationErrors.ProviderUnavailable);
        }
        catch (NotSupportedException exception)
        {
            // Sunucu istenen kimlik doğrulamasını veya TLS'i hiç duyurmuyor. Bu bir
            // yapılandırma uyuşmazlığıdır: tekrar denemek aynı sonucu verir.
            logger.LogError(
                exception,
                "SMTP sunucusu istenen kimlik doğrulamasını veya TLS'i desteklemiyor: {Host} {StartTls}",
                options.SmtpHost,
                options.SmtpUseStartTls);

            return SendResult.Permanent(NotificationErrors.ProviderUnavailable);
        }
        catch (Exception exception) when (exception is SmtpProtocolException or SocketException or IOException)
        {
            // Ağ, TLS veya protokol düştü: sağlayıcı bir şey söylemedi bile. Bunlar tam
            // olarak tekrar denemenin işe yaradığı hatalardır.
            logger.LogWarning(exception, "SMTP bağlantısı kurulamadı veya düştü: {Host}", options.SmtpHost);

            return SendResult.Transient(NotificationErrors.ProviderUnavailable);
        }
    }

    /// <summary>
    /// Gönderen adresi YAPILANDIRMADAN gelir, şablondan değil: şablonlar çeviri verisidir
    /// ve kimden gittiği bir dağıtım kararıdır (C8).
    ///
    /// Gövde düz metindir. HTML V1'in kapsamı dışında (X2) ve düz metin, kaçırılmış bir
    /// yer tutucunun e-posta istemcisinde çalıştırılabilir bir şeye dönüşme ihtimalini de
    /// ortadan kaldırır.
    /// </summary>
    private MimeMessage Compose(OutboundMessage message)
    {
        MimeMessage mime = new()
        {
            Subject = message.Subject,
            Body = new TextPart("plain") { Text = message.Body },
        };

        mime.From.Add(MailboxAddress.Parse(options.SenderAddress));
        mime.To.Add(MailboxAddress.Parse(message.Recipient));

        return mime;
    }
}
