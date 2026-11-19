namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Geri almanın dönebileceği üç durum ve onların sözleşmedeki kodları. Kod, panelin zaten
/// kullandığı durum kodudur (`ListingStatus` adı) — ikinci bir sözlük açılmıyor.
///
/// <para>Agregatın yanında, komutta değil: hangi durumların geri almanın hedefi olabileceği
/// A3'ün bilgisidir (C2).</para>
/// </summary>
internal static class SaleRevertStatuses
{
    internal static bool TryParse(string? code, out ListingStatus status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(code)
            || !Enum.TryParse(code, ignoreCase: false, out ListingStatus parsed)
            || !Enum.IsDefined(parsed)
            || parsed is not (ListingStatus.Published or ListingStatus.Reserved or ListingStatus.Draft)
            || !string.Equals(parsed.ToString(), code, StringComparison.Ordinal))
        {
            return false;
        }

        status = parsed;
        return true;
    }
}
