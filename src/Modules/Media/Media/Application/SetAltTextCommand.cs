using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// `0012a` D3/D16 — `alt` metninin yazma komutu.
///
/// <para><b>`0011` alanı tuttu, ucu bilerek açmadı</b> (D16): `alt` metni araç bağlamında
/// anlamlı ("beyaz BMW 320i, önden") ve yükleme anında personel o bağlamı kurmamış olur.
/// Giriş `0012a`'nın medya ekranıyla geliyor.</para>
///
/// <para><b>Uç `Media` modülünde</b> çünkü metin `MediaAsset`'in alanıdır; başka modülün
/// şemasına yazmak Yasak Listesi 1'dir. `VehicleCatalog` metnin kopyasını tutmaz
/// (`0012a` C5).</para>
///
/// <para><b>Bütünüyle yerine koyar</b> (D16): eksik locale <b>boş</b> sayılır ve bir dili
/// boş bırakmak geçerli bir durumdur (R4). Locale başına kısmi güncelleme yüzeyi ikiye
/// katlar ve "boşa çek" ile "dokunma" ayrımını API'ye taşırdı.</para>
/// </summary>
internal sealed class SetAltTextCommand(MediaDbContext context, MediaOptions options, IClock clock)
{
    /// <summary>
    /// `alt` metninin bir locale için üst sınırı. Görme engelli ziyaretçiye okunan kısa bir
    /// tanım; sınırsız bırakmak ekran okuyucuda paragraf uzunluğunda bir metin demekti.
    /// </summary>
    internal const int MaximumLength = 200;

    internal async Task<Result> ExecuteAsync(
        Guid mediaAssetId,
        IReadOnlyDictionary<string, string> altText,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(altText);

        List<KeyValuePair<Locale, string>> degerler = [];

        foreach ((string kod, string metin) in altText)
        {
            Result<Locale> locale = Locale.Create((kod ?? string.Empty).Trim());

            // AC-14 — etkin OLMAYAN bir locale için yazılamaz. Ayrıştırılamayan kod da
            // burada düşüyor: ikisi de aynı cevabı hak ediyor ("bu dil yazılamaz") ve iki
            // ayrı kod üretmek uca iki ayrı çeviri borcu yüklerdi.
            if (locale.IsFailure || !options.EnabledLocales.Contains(locale.Value.Code))
            {
                return Result.Failure(Error.With(
                    MediaErrors.LocaleNotEnabled, "locale", kod ?? string.Empty));
            }

            string kirpilmis = (metin ?? string.Empty).Trim();

            if (kirpilmis.Length > MaximumLength)
            {
                return Result.Failure(Error.With(
                    MediaErrors.AltTextTooLong, "locale", locale.Value.Code));
            }

            // Boş metin SAKLANMAZ, atlanır: R4 bir dilin boş kalmasını geçerli sayıyor ve
            // boş dizeyi saklamak "yazılmış ama boş" ile "yazılmamış"ı ayırt edilemez
            // kılardı — `vehicle.badge.missingLocales` türü bir tamlık ölçümü o farkı
            // okumak zorunda.
            if (kirpilmis.Length > 0)
            {
                degerler.Add(new KeyValuePair<Locale, string>(locale.Value, kirpilmis));
            }
        }

        MediaAsset? asset = await context.Assets
            .FirstOrDefaultAsync(value => value.Id == mediaAssetId, cancellationToken);

        if (asset is null)
        {
            return Result.Failure(new Error(MediaErrors.NotFound));
        }

        asset.SetAltText(LocalizedText.From(degerler), clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
