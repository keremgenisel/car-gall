using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// Şubenin saat dilimi, **IANA bölge kimliği** olarak (R2, C9).
///
/// Sabit saat farkı (`+03:00`) kabul edilmez: yaz saati uygulaması ve kural değişiklikleri
/// yalnızca bölge kimliğinde doğru izlenir; sabit fark B4'ü ("şu an açık mı") sessizce
/// yanlış hesaplatır ve hata yılda iki kez, birkaç saatliğine ortaya çıkar.
///
/// D8: doğrulama işletim sisteminin tzdata'sına bakar. Paket eksikse GEÇERLİ bölge kimliği
/// de reddedilir; bu yüzden tzdata'nın varlığı ayrıca bir testle korunur.
/// </summary>
internal readonly record struct TimeZoneId
{
    internal const string InvalidZone = "time_zone.invalid_zone";

    private TimeZoneId(string value) => Value = value;

    internal string Value { get; }

    internal static Result<TimeZoneId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('/', StringComparison.Ordinal))
        {
            // IANA kimliği daima "Bölge/Şehir" biçimindedir; "+03:00" ve "UTC" buradan düşer.
            return Result.Failure<TimeZoneId>(new Error(InvalidZone));
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(value, out _))
        {
            return Result.Failure<TimeZoneId>(new Error(InvalidZone));
        }

        return Result.Success(new TimeZoneId(value));
    }

    public override string ToString() => Value;
}
