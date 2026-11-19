namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D9** — kaynak başına GEÇERLİ istek bütçesi (öneri: 10 dakikada 5). Biçimi geçersiz
/// istek bu bütçeyi tüketmez (AC-13); kaba istek sınırı ayrıdır ve boru hattındadır.
///
/// Anahtar KİŞİSEL VERİDİR (IP): yalnız bellekteki geçici sayaçta durur; kalıcı kayda ve loga
/// yazılmaz (AC-15, `0005` C17).
/// </summary>
internal interface INewsletterRequestQuota
{
    bool TryAcquire(string clientKey);
}
