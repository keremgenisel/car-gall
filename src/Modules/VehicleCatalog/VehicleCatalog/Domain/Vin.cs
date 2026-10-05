using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Şase numarası (docs/domain.md **A5**). Galeri genelinde TEKİLDİR ve `Arşiv` kayıtları
/// bu kontrolden muaftır — tekillik burada değil, komut katmanında kilit altında
/// doğrulanır (0010a **D11**): muafiyet `Listing.Status`'e bağlı ve bu değer tipi onu
/// göremez.
///
/// <para><b>Her zaman GİZLİ alandır</b> (**F4**): hiçbir vitrin yanıtında bulunmaz, boş
/// değer olarak da bulunmaz (AC-18). Yönetim yanıtında bulunur ve o yanıt oturum
/// gerektirir (AC-19, C20).</para>
///
/// <para><b>Biçim bilinçli olarak gevşek.</b> ISO 3779 on yedi karakter der ama ithal,
/// klasik ve bazı ticari araçlarda bu tutmuyor; dar bir kalıp personeli gerçek bir aracı
/// sisteme sokamaz hâle getirirdi. Denetlenen şey uzunluk ve boşluk — R11'in istediği üst
/// sınır.</para>
/// </summary>
internal readonly record struct Vin
{
    /// <summary>Boş veya iç boşluk taşıyan şase. <b>Uzunluk DEĞİL</b> — bkz. <see cref="TooLong"/>.</summary>
    internal const string InvalidFormat = "vin.invalid_format";

    /// <summary>
    /// Üst uzunluk sınırı aşıldı (R11). Biçim hatasından AYRI bir koddur ve bu bir
    /// incelik değil: tek koda eşlendiğinde personel boş bıraktığı alanda "uzunluk
    /// aşıldı" mesajı görüyordu ve şaseyi KISALTMAYA çalışıyordu — oysa sorun başkaydı.
    /// </summary>
    internal const string TooLong = "vin.too_long";

    /// <summary>R11 — serbest metin alanlarının üst uzunluk sınırı.</summary>
    internal const int MaximumLength = 32;

    private Vin(string value) => Value = value;

    internal string Value { get; }

    internal static Result<Vin> Create(string value)
    {
        string trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            return Result.Failure<Vin>(new Error(InvalidFormat));
        }

        if (trimmed.Length > MaximumLength)
        {
            return Result.Failure<Vin>(new Error(TooLong));
        }

        // İç boşluk taşıyan şase, aynı aracın iki farklı yazımla iki kez girilmesine yol
        // açar ve tekillik kontrolü ikisini farklı sayar.
        if (trimmed.Any(char.IsWhiteSpace))
        {
            return Result.Failure<Vin>(new Error(InvalidFormat));
        }

        return Result.Success(new Vin(trimmed));
    }

    public override string ToString() => Value;
}
