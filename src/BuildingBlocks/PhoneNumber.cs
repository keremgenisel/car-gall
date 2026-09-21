namespace CarGallery.BuildingBlocks;

/// <summary>
/// E.164 biçiminde telefon numarası (docs/architecture.md § 1 — BuildingBlocks değer tipleri).
///
/// Serbest metin kabul edilmez: numara biçimi ülkeye göre değişir ve E.164 dışına çıkıldığında
/// WhatsApp derin bağlantısı SESSİZCE kırılır — hata yalnızca ziyaretçi tıkladığında görünür.
/// </summary>
public readonly record struct PhoneNumber
{
    public const string InvalidFormat = "phone_number.invalid_format";

    private const int MinimumDigits = 8;
    private const int MaximumDigits = 15;

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (value is not { Length: >= MinimumDigits + 1 and <= MaximumDigits + 1 } || value[0] != '+')
        {
            return Result.Failure<PhoneNumber>(new Error(InvalidFormat));
        }

        // E.164: ülke kodu 0 ile başlamaz.
        if (value[1] == '0')
        {
            return Result.Failure<PhoneNumber>(new Error(InvalidFormat));
        }

        foreach (char character in value[1..])
        {
            if (character is < '0' or > '9')
            {
                return Result.Failure<PhoneNumber>(new Error(InvalidFormat));
            }
        }

        return Result.Success(new PhoneNumber(value));
    }

    public override string ToString() => Value;
}
