namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// C17/AC-32'nin sayacı: "aynı IP'den 10 dakikada en çok 5 **talep**".
///
/// Kriterin cümlesindeki <b>"kaydedildikten sonra"</b> bilinçlidir ve bu arayüzün varlık
/// sebebidir. Boru hattındaki hız sınırı permit'i handler'dan ÖNCE tüketir, yani İSTEK
/// sayar: sunucu tarafı doğrulamaya düşen dürüst bir ziyaretçi (telefonu E.164 dışında,
/// ilanı düşmüş, tipi reddedilmiş) beş denemede bütçesini bitirir ve hiç talep
/// bırakamadan on dakika beklerdi. Spec'in Intent'i tam olarak bu kaybı önlemek için
/// yazıldı.
///
/// Bu sayaç yalnız KAYIT OLUŞAN istekte ilerler. Kaba tekrarı kesme işi ayrı ve daha
/// gevşek bir istek sınırındadır (R16, Karar D4).
///
/// Anahtar KİŞİSEL VERİDİR: yalnız bellekte, geçici sayaçta durur; kalıcı kayda ve loga
/// yazılmaz (C17, docs/conventions.md § 12).
/// </summary>
internal interface ILeadCaptureQuota
{
    /// <summary>Pencerede yer varsa bir hak ayırır ve <c>true</c> döner.</summary>
    bool TryAcquire(string clientKey);

    /// <summary>Ayrılan hakkı geri verir: kayıt oluşmadıysa bütçe harcanmamalıdır.</summary>
    void Release(string clientKey);
}
