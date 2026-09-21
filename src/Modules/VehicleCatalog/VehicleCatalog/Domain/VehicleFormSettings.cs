namespace CarGallery.Modules.VehicleCatalog.Domain;

internal enum FieldFormState
{
    Closed = 0,
    Optional = 1,
    Required = 2,
}

internal enum FieldPublicVisibility
{
    Hidden = 0,
    Visible = 1,
}

/// <summary>
/// Alan başına İKİ eksen: form durumu ve public görünürlük (docs/architecture.md § 4).
/// İkinci eksen gereklidir — galeri bir bilgiyi kayıt altına alıp vitrinde göstermek
/// istemeyebilir; tek eksenli toggle galeriye alanı tamamen kapattırır ve veri kaybı doğurur.
/// </summary>
internal sealed class VehicleFieldSetting
{
    internal required string FieldKey { get; init; }

    internal required FieldFormState FormState { get; set; }

    internal required FieldPublicVisibility PublicVisibility { get; set; }

    /// <summary>Çekirdek alanlar kapatılamaz (F4, Yasak Listesi 42).</summary>
    internal required bool IsCore { get; init; }

    internal bool IsOpen => FormState is not FieldFormState.Closed;

    internal bool IsPublic => PublicVisibility is FieldPublicVisibility.Visible;

    /// <summary>
    /// Bir alan filtrede görünür ⇔ açık VE public VE doğası gereği filtrelenebilir.
    /// Filtre üçüncü eksen değildir, türetilir (docs/architecture.md § 4).
    /// </summary>
    internal bool IsFilterable => IsOpen && IsPublic;
}

internal static class VehicleFieldKeys
{
    internal const string BodyType = "body_type";
}
