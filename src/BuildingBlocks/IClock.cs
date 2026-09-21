namespace CarGallery.BuildingBlocks;

/// <summary>
/// <c>DateTime.Now</c> / <c>DateTime.UtcNow</c> doğrudan kullanılmaz (Yasak Listesi 30);
/// saat enjekte edilir, böylece test saate bağlı kalmaz.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
