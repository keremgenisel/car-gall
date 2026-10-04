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

        // AC-32 — değeri BOŞ olan bir yer tutucunun bulunduğu satır tamamen düşer.
        //
        // Gerekçe: ilan kaldırılmışsa slug çözülemez ve bildirim bağlantısız gider. Boş
        // değeri olduğu gibi koysaydık e-postada "İlan: " diye sarkan bir etiket kalırdı;
        // etiketi koda yazıp gizlemek ise C6'yı ihlal ederdi (kullanıcıya görünen metin
        // koda gömülmez). Satırı düşürmek, kararı ŞABLONDA bırakır.
        string[] satirlar = template.Split('\n');
        List<string> kalan = [];

        foreach (string satir in satirlar)
        {
            string islenmis = satir;
            bool bosDustu = false;

            foreach (KeyValuePair<string, string> value in values)
            {
                string yerTutucu = string.Create(CultureInfo.InvariantCulture, $"{{{value.Key}}}");

                if (!islenmis.Contains(yerTutucu, StringComparison.Ordinal))
                {
                    continue;
                }

                if (value.Value.Length == 0)
                {
                    bosDustu = true;
                    break;
                }

                islenmis = islenmis.Replace(yerTutucu, value.Value, StringComparison.Ordinal);
            }

            if (!bosDustu)
            {
                kalan.Add(islenmis);
            }
        }

        return string.Join('\n', kalan).TrimEnd();
    }
}
