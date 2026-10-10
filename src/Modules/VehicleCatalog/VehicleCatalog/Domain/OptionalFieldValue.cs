using System.Globalization;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Opsiyonel alan değerinin tipi. Küme KAPALIDIR (0010b **R6**, Karar **D17**): dosya,
/// liste ve medya tipleri bu spec'in dışındadır (X22).
/// </summary>
internal enum FieldValueKind
{
    Text = 1,
    LocalizedText = 2,
    Integer = 3,
    Amount = 4,
    Boolean = 5,
    Date = 6,
    Option = 7,
}

/// <summary>
/// Opsiyonel alanın TİPLİ değeri (0010b **D17**, **C23**).
///
/// <para><b>Neden tek bir "metin" kovası yetmiyor.</b> Bugün tanımlı tek opsiyonel alan
/// tramer — bir <b>tutar</b>. Parayı metin olarak saklamak `AGENTS.md` Altın Kural 4 ve
/// Yasak Listesi 26'nın doğrudan ihlali ve hata SESSİZ: tutar bir dizge olarak girer,
/// hiçbir test kırılmaz.</para>
///
/// <para>Tip başına yalnız bir taşıyıcı dolu olur; geri kalanı <c>null</c>'dır. Depo
/// tarafında her taşıyıcının kendi kolonu vardır (**D22**) — vitrin okuma modeli parayı
/// zaten aynı üçlüyle tutuyor.</para>
/// </summary>
internal sealed record OptionalFieldValue
{
    internal const string InvalidValue = "optional_field.invalid_value";
    internal const string KindMismatch = "optional_field.kind_mismatch";

    /// <summary>R11 ile aynı sınıftan bir üst sınır: serbest metin sınırsız olamaz.</summary>
    internal const int MaximumTextLength = 500;

    private OptionalFieldValue(FieldValueKind kind) => Kind = kind;

    internal FieldValueKind Kind { get; }

    internal string? Text { get; private init; }

    internal LocalizedText? Localized { get; private init; }

    internal long? Integer { get; private init; }

    internal Money? Amount { get; private init; }

    internal bool? Boolean { get; private init; }

    internal DateOnly? Date { get; private init; }

    internal string? Option { get; private init; }

    internal static OptionalFieldValue OfText(string value) =>
        new(FieldValueKind.Text) { Text = value };

    internal static OptionalFieldValue OfLocalizedText(LocalizedText value) =>
        new(FieldValueKind.LocalizedText) { Localized = value };

    internal static OptionalFieldValue OfInteger(long value) =>
        new(FieldValueKind.Integer) { Integer = value };

    internal static OptionalFieldValue OfAmount(Money value) =>
        new(FieldValueKind.Amount) { Amount = value };

    internal static OptionalFieldValue OfBoolean(bool value) =>
        new(FieldValueKind.Boolean) { Boolean = value };

    internal static OptionalFieldValue OfDate(DateOnly value) =>
        new(FieldValueKind.Date) { Date = value };

    internal static OptionalFieldValue OfOption(string slug) =>
        new(FieldValueKind.Option) { Option = slug };

    /// <summary>
    /// Panelden gelen HAM METNİ beyan edilen tipe çevirir (AC-11, AC-12).
    ///
    /// <para><b>Ayrıştırma her zaman `InvariantCulture` ile.</b> Personelin locale'ine göre
    /// ayrıştırmak, `1.250` değerinin Türkçe'de bin iki yüz elli, İngilizce'de bir nokta
    /// iki beş olmasına yol açardı — ve hangi dilde girildiği istekte yazmıyor. Uç ham
    /// sayıyı gönderir, biçimlendirme uçta yapılır (`docs/conventions.md` § 7).</para>
    ///
    /// <para><b>Locale'li metin bu yoldan GEÇMEZ:</b> onun taşıyıcısı tek bir dize değil,
    /// locale → metin eşlemesidir. Çağıran <see cref="OfLocalizedText"/> kullanır ve
    /// buraya locale'li tiple gelmek <see cref="KindMismatch"/> ile reddedilir.</para>
    /// </summary>
    internal static Result<OptionalFieldValue> Parse(FieldValueKind kind, string raw, CatalogCurrency currency)
    {
        string trimmed = raw?.Trim() ?? string.Empty;

        if (trimmed.Length > MaximumTextLength)
        {
            return Result.Failure<OptionalFieldValue>(new Error(InvalidValue));
        }

        switch (kind)
        {
            case FieldValueKind.Text:
                return Result.Success(OfText(trimmed));

            case FieldValueKind.Option:
                return Result.Success(OfOption(trimmed));

            case FieldValueKind.Integer:
                return long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number)
                    ? Result.Success(OfInteger(number))
                    : Result.Failure<OptionalFieldValue>(new Error(InvalidValue));

            case FieldValueKind.Amount:
                if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
                {
                    return Result.Failure<OptionalFieldValue>(new Error(InvalidValue));
                }

                Result<Money> money = Money.Create(amount, currency.Code, currency.MinorUnitExponent);

                // `Money` sessiz yuvarlamayı reddediyor: KWD'de üç ondalıklı bir tutar iki
                // ondalığa kırpılsaydı hata yalnızca gözle fark edilirdi.
                return money.IsSuccess
                    ? Result.Success(OfAmount(money.Value))
                    : Result.Failure<OptionalFieldValue>(new Error(InvalidValue));

            case FieldValueKind.Boolean:
                return bool.TryParse(trimmed, out bool flag)
                    ? Result.Success(OfBoolean(flag))
                    : Result.Failure<OptionalFieldValue>(new Error(InvalidValue));

            case FieldValueKind.Date:
                // Yalnız ISO 8601. "15/03/2024" hem 15 Mart hem 3 Ekim olabilir ve hangisi
                // olduğu istekte yazmıyor; belirsiz biçimi kabul etmek sessiz veri hatasıdır.
                return DateOnly.TryParseExact(
                    trimmed,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly date)
                    ? Result.Success(OfDate(date))
                    : Result.Failure<OptionalFieldValue>(new Error(InvalidValue));

            default:
                return Result.Failure<OptionalFieldValue>(new Error(KindMismatch));
        }
    }
}

/// <summary>
/// Kurulumun tek para birimi (`docs/architecture.md` § Karar Defteri: kurulum başına tek,
/// sabit). `Domain` yapılandırmayı okuyamaz, bu yüzden değer parametre olarak gelir.
/// </summary>
internal readonly record struct CatalogCurrency(string Code, int MinorUnitExponent);
