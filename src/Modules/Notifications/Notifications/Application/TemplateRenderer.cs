using System.Globalization;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>
/// Şablon yer tutucularını doldurur.
///
/// <para><b>Neden ICU değil.</b> C7 "sayı veya çoğul İÇEREN cümleler" için ICU'yu zorunlu
/// kılıyor. Bu spec'in iki şablonunda sayı ve çoğul YOKTUR: model adı, şube adı ve bir
/// bağlantı — üçü de düz değiştirme. Sayı veya çoğul içeren bir şablon eklendiği gün
/// burası ICU'ya geçmek zorunda ve o gün bu yorum uyarı olarak durur.</para>
///
/// <para>Eksik yer tutucu SESSİZCE boş bırakılmaz: bilinmeyen anahtar olduğu gibi kalır
/// ve testte görünür. Boş bırakmak, ziyaretçiye yarım bir cümle göndermek olurdu.</para>
/// </summary>
internal static class TemplateRenderer
{
    internal static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        string sonuc = template;

        foreach (KeyValuePair<string, string> value in values)
        {
            sonuc = sonuc.Replace(
                string.Create(CultureInfo.InvariantCulture, $"{{{value.Key}}}"),
                value.Value,
                StringComparison.Ordinal);
        }

        return sonuc;
    }
}
