using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// `0016a` C10 — 13 parçanın KODLARI ve SIRASI. Sıra hem okunabilir listenin hem panel
/// formunun sırasıdır; tablonun tek evi `specs/0016a` C10'dur (**D6**). Kod çevrilmez,
/// görünen ad uçta çeviri kataloğundan gelir (C5, C6).
/// </summary>
internal static class PaintChangeParts
{
    internal static readonly IReadOnlyList<string> Ordered =
    [
        "front_bumper",
        "hood",
        "roof",
        "trunk_lid",
        "rear_bumper",
        "left_front_fender",
        "left_front_door",
        "left_rear_door",
        "left_rear_fender",
        "right_front_fender",
        "right_front_door",
        "right_rear_door",
        "right_rear_fender",
    ];
}

/// <summary>`0016a` R10 · **D1** — dört durum; "belirtilmedi" diye bir durum YOKTUR.</summary>
internal static class PaintChangeStates
{
    internal const string Original = "original";
    internal const string LocalPainted = "local_painted";
    internal const string Painted = "painted";
    internal const string Replaced = "replaced";

    internal static readonly IReadOnlySet<string> All =
        new HashSet<string>([Original, LocalPainted, Painted, Replaced], StringComparer.Ordinal);
}

internal sealed record PaintChangeEntry(string Part, string State);

/// <summary>
/// `0016a` R9–R11 — parça haritası. Değer ya 13 parçanın TAMAMINI taşır ya da hiç yoktur
/// (**D7**): yarım harita, galerinin vermediği bir beyanı ("bilinmeyen parçalar orijinal")
/// sessizce üretirdi. Girdi sırası ne olursa olsun girdiler C10 sırasıyla saklanır (AC-6).
/// </summary>
internal sealed class PaintChangeMap
{
    /// <summary>
    /// Eksik parça var: harita reddedilir ve eksikler KOD olarak, C10 sırasıyla döner
    /// (AC-8). Parametre <c>parts</c> virgülle ayrılmış koddur — `BuildingBlocks`'un
    /// <c>Error</c>'ı dizi taşımıyor (`0012b` D24'ün deseni).
    /// </summary>
    internal const string Incomplete = "vehicle.paint_change_map_incomplete";

    private PaintChangeMap(IReadOnlyList<PaintChangeEntry> entries) => Entries = entries;

    /// <summary>Her zaman 13 eleman, C10 sırasıyla.</summary>
    internal IReadOnlyList<PaintChangeEntry> Entries { get; }

    /// <summary>
    /// Kurallar, sırayla:
    /// <list type="number">
    /// <item>Tanınmayan parça ya da durum kodu (harfe duyarlı — AC-12) → <see cref="OptionalFieldValue.InvalidValue"/>.</item>
    /// <item>Eksik parça → <see cref="Incomplete"/> + eksikler (AC-7, AC-8, AC-11'in ikinci hâli).</item>
    /// <item>Aynı parça birden çok kez (13'ü de varken fazladan) → <see cref="OptionalFieldValue.InvalidValue"/> (AC-11).</item>
    /// </list>
    /// </summary>
    internal static Result<PaintChangeMap> Create(IReadOnlyList<PaintChangeEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        HashSet<string> knownParts = new(PaintChangeParts.Ordered, StringComparer.Ordinal);

        if (entries.Any(entry => entry is null
            || !knownParts.Contains(entry.Part)
            || !PaintChangeStates.All.Contains(entry.State)))
        {
            return Result.Failure<PaintChangeMap>(new Error(OptionalFieldValue.InvalidValue));
        }

        HashSet<string> given = new(entries.Select(entry => entry.Part), StringComparer.Ordinal);
        List<string> missing = [.. PaintChangeParts.Ordered.Where(part => !given.Contains(part))];

        if (missing.Count > 0)
        {
            return Result.Failure<PaintChangeMap>(Error.With(Incomplete, "parts", string.Join(',', missing)));
        }

        if (entries.Count != PaintChangeParts.Ordered.Count)
        {
            return Result.Failure<PaintChangeMap>(new Error(OptionalFieldValue.InvalidValue));
        }

        Dictionary<string, string> byPart = entries.ToDictionary(entry => entry.Part, entry => entry.State, StringComparer.Ordinal);

        return Result.Success(new PaintChangeMap(
            [.. PaintChangeParts.Ordered.Select(part => new PaintChangeEntry(part, byPart[part]))]));
    }
}
