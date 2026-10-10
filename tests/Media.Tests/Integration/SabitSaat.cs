using CarGallery.BuildingBlocks;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Tarih ve saat testlerinde `IClock` SABİTLENİR; gerçek saate bağlı test yazılmaz
/// (`docs/testing.md` § 4).
/// </summary>
internal sealed class SabitSaat(DateTimeOffset simdi) : IClock
{
    private DateTimeOffset simdi = simdi;

    public DateTimeOffset UtcNow => simdi;

    internal void Ilerle(TimeSpan sure) => simdi = simdi.Add(sure);
}
