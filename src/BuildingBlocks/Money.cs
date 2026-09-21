namespace CarGallery.BuildingBlocks;

/// <summary>
/// Para: tutar + ISO 4217 kodu + minor unit üssü (docs/domain.md P2).
/// Çıplak <see cref="decimal"/> bir parayı temsil edemez; KWD/BHD/JOD/OMR üç ondalıklıdır.
/// </summary>
public readonly record struct Money
{
    public const string InvalidCurrencyCode = "money.invalid_currency_code";
    public const string InvalidMinorUnitExponent = "money.invalid_minor_unit_exponent";
    public const string AmountPrecisionExceeded = "money.amount_precision_exceeded";
    public const string NegativeAmount = "money.negative_amount";
    public const string CurrencyMismatch = "money.currency_mismatch";

    /// <summary>Saklama <c>decimal(19,4)</c> olduğu için üs 4'ü aşamaz (docs/conventions.md § 7).</summary>
    private const int MaximumMinorUnitExponent = 4;

    private Money(decimal amount, string currencyCode, int minorUnitExponent)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
        MinorUnitExponent = minorUnitExponent;
    }

    public decimal Amount { get; }

    public string CurrencyCode { get; }

    public int MinorUnitExponent { get; }

    public static Result<Money> Create(decimal amount, string currencyCode, int minorUnitExponent)
    {
        if (!IsIso4217Shaped(currencyCode))
        {
            return Result.Failure<Money>(new Error(InvalidCurrencyCode));
        }

        if (minorUnitExponent is < 0 or > MaximumMinorUnitExponent)
        {
            return Result.Failure<Money>(new Error(InvalidMinorUnitExponent));
        }

        if (amount < 0m)
        {
            return Result.Failure<Money>(new Error(NegativeAmount));
        }

        // Sessiz yuvarlama veri kaybıdır: KWD'de 1250,5005 tutarı iki ondalığa kırpılırsa
        // hata yalnızca gözle fark edilir. Kırpmayı reddedip çağırana Round'u bırakıyoruz.
        if (decimal.Round(amount, minorUnitExponent) != amount)
        {
            return Result.Failure<Money>(new Error(AmountPrecisionExceeded));
        }

        return Result.Success(new Money(amount, currencyCode, minorUnitExponent));
    }

    /// <summary>Yuvarlama daima açıkça istenir; varsayılana güvenilmez (docs/conventions.md § 7).</summary>
    public static Result<Money> Round(decimal amount, string currencyCode, int minorUnitExponent, MidpointRounding mode)
    {
        if (minorUnitExponent is < 0 or > MaximumMinorUnitExponent)
        {
            return Result.Failure<Money>(new Error(InvalidMinorUnitExponent));
        }

        return Create(decimal.Round(amount, minorUnitExponent, mode), currencyCode, minorUnitExponent);
    }

    public Result<Money> Add(Money other) => CurrencyCode == other.CurrencyCode
        ? Create(Amount + other.Amount, CurrencyCode, MinorUnitExponent)
        : Result.Failure<Money>(new Error(CurrencyMismatch));

    private static bool IsIso4217Shaped(string currencyCode)
    {
        if (currencyCode is not { Length: 3 })
        {
            return false;
        }

        foreach (char character in currencyCode)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return true;
    }
}
