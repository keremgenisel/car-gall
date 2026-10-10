namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Opsiyonel alanın tanımı: anahtarı ve TİPİ (0010b **R6**).
/// Açık/kapalı ve public olup olmadığı burada DEĞİL, `VehicleFieldSetting`'tedir —
/// orası operatörün ekseni, burası kodun.
/// </summary>
internal sealed record VehicleFieldDefinition(string FieldKey, FieldValueKind Kind);

/// <summary>
/// Alan → tip kayıt defteri (0010b Karar **D21**).
///
/// <para><b>Neden kodda, ayar tablosunda değil.</b> <b>F5</b> / Yasak Listesi 40 alan
/// listesini "kodda tanımlı, sabit, versiyonlu" ilan ediyor ve `vehicle_field_settings`
/// yapılandırmasının kendi yorumu da bunu yazıyor. Tip bir operatör ayarı DEĞİLDİR:
/// galeri bir alanın tipini değiştirirse o alanda saklanmış bütün değerler anlamsızlaşır
/// ve bozulma sessiz olur.</para>
///
/// <para><b>AC-9'un kapısı burasıdır:</b> defterde olmayan bir anahtara değer yazılamaz.
/// "Tipi beyan edilmemiş alan" = burada kaydı olmayan alan.</para>
/// </summary>
internal static class VehicleFieldDefinitions
{
    private static readonly IReadOnlyDictionary<string, VehicleFieldDefinition> ByKey =
        new Dictionary<string, VehicleFieldDefinition>(StringComparer.Ordinal)
        {
            // Tramer — hasar kayıt TUTARI (docs/domain.md sözlüğü). Tipi `tutar`; `Money`
            // ile saklanır (D17) ve bu spec'in var oluş nedenlerinden biri budur.
            [VehicleFieldKeys.DamageRecordAmount] =
                new(VehicleFieldKeys.DamageRecordAmount, FieldValueKind.Amount),
        };

    /// <summary>Çekirdek alanlar bu defterde YOKTUR: onlar agregatın kendi alanlarıdır.</summary>
    internal static bool TryGet(string fieldKey, out VehicleFieldDefinition definition) =>
        ByKey.TryGetValue(fieldKey, out definition!);

    internal static IReadOnlyCollection<VehicleFieldDefinition> All => [.. ByKey.Values];
}
