namespace CarGallery.BuildingBlocks.Schema;

/// <summary>
/// Bir modülün veritabanı şeması. Host onu <b>enjeksiyonla</b> bulur; elle tutulan bir
/// liste yoktur.
///
/// Bu tercih bilinçlidir: modül başına bir kayıt satırı, o modülü hem UYGULAMA hem
/// DOĞRULAMA yoluna aynı anda sokar. Host'ta elle sayılan bir liste olsaydı, yeni bir
/// modül eklendiğinde o liste güncellenmeyi unutur ve şema sessizce oluşmazdı — sorun
/// ancak ilk sorguda, üretimde görünürdü.
///
/// `BuildingBlocks`'a konabilir, çünkü iş kuralı değil altyapıdır — `IOutboxDbContext`
/// ile aynı sınıftan (docs/architecture.md § 1).
/// </summary>
public interface IModuleSchema
{
    /// <summary>Hata mesajlarında görünen ad; operatör hangi modül olduğunu bilmeli.</summary>
    string Name { get; }

    /// <summary>Bekleyen migration'ları uygular. YALNIZCA kurulum adımından çağrılır.</summary>
    Task ApplyAsync(CancellationToken cancellationToken);

    /// <summary>Uygulanmamış migration var mı? Çalışan uygulama yalnız bunu sorar.</summary>
    Task<bool> HasPendingAsync(CancellationToken cancellationToken);
}
