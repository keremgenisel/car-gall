using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Tests;

/// <summary>Tarih testlerinde saat sabitlenir; gerçek saate bağlı test yazılmaz (docs/testing.md § 4).</summary>
internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    internal void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
}
