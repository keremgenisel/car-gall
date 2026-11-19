using System.Text;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleReference.Domain;

/// <summary>
/// `0021` C3 · D10 — kanonik ad ve kısaltma kuralları TEK yerde. Marka ve model aynı kanonik
/// ad kuralına tabidir; yalnız hata kodunun alanı ve uzunluk sınırı farklıdır.
/// </summary>
internal static class ReferenceNames
{
    internal const int BrandCanonicalMaxLength = 80;

    internal const int ModelCanonicalMaxLength = 120;

    internal const int DisplayNameMaxLength = 120;

    /// <summary>
    /// Kanonik ad: kırpılmış, boş değil, yazdırılabilir ASCII (C3). "Škoda" reddedilir
    /// (AC-10): adres ve tekillik kanonik addan türer ve ASCII dışı harf, değişmez kültürde
    /// bile tek bir büyük/küçük harf karşılığına sahip olmayabilir.
    /// </summary>
    internal static Result<string> Canonical(string? raw, string prefix, int maxLength)
    {
        string value = (raw ?? string.Empty).Trim();

        if (value.Length == 0)
        {
            return Result.Failure<string>(new Error($"{prefix}.canonical_name_required"));
        }

        if (value.Length > maxLength)
        {
            return Result.Failure<string>(new Error($"{prefix}.canonical_name_too_long"));
        }

        if (value.Any(character => character is < ' ' or > '~') || !value.Any(char.IsAsciiLetterOrDigit))
        {
            return Result.Failure<string>(new Error($"{prefix}.canonical_name_invalid"));
        }

        return Result.Success(value);
    }

    /// <summary>
    /// Tekillik anahtarı. Değişmez kültürle büyük harf (C3): Türkçe kültürde "i" → "İ" olur ve
    /// "Civic" ile "CIVIC" iki ayrı ad sayılırdı.
    /// </summary>
    internal static string Key(string canonicalName) => canonicalName.ToUpperInvariant();

    /// <summary>
    /// D10 — küçük harf; harf ve rakam dışındaki her karakter dizisi tek <c>-</c>; baştaki ve
    /// sondaki <c>-</c> atılır. "Rolls-Royce &amp; Co." → <c>rolls-royce-co</c> (AC-11).
    /// Kanonik ad en az bir harf ya da rakam taşıdığı için sonuç hiçbir zaman boş değildir.
    /// </summary>
    internal static string Slug(string canonicalName)
    {
        StringBuilder builder = new(canonicalName.Length);
        bool pendingDash = false;

        foreach (char character in canonicalName)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }
        }

        return builder.ToString();
    }
}
