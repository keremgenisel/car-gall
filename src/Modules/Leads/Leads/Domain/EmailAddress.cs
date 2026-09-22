using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Ziyaretçinin e-posta adresi (R4, AC-5). Opsiyoneldir — ama verildiyse biçimi doğrulanır.
///
/// `Dealership`'te aynı adı taşıyan bir tip var ve bu KOPYA BİLİNÇLİDİR: sınır boyunca
/// tekrar tercihtir, ortak tip çıkarmak değil (Yasak Listesi 10). Orası şubenin kurumsal
/// adresini doğruluyor, burası ziyaretçinin girdiğini; ikisi ayrı ayrı değişebilmelidir.
///
/// Doğrulama bilinçli olarak dardır: tek `@`, iki yanında da içerik, alan adında nokta.
/// RFC 5322'yi tam uygulayan bir regex bu kriteri daha doğru yapmaz, yalnız okunamaz yapar.
/// </summary>
internal readonly record struct EmailAddress
{
    internal const string InvalidFormat = "email_address.invalid_format";

    /// <summary>RFC 5321'in yol (path) üst sınırı.</summary>
    internal const int MaximumLength = 254;

    private EmailAddress(string value) => Value = value;

    internal string Value { get; }

    internal static Result<EmailAddress> Create(string value)
    {
        if (value is not { Length: > 2 and <= MaximumLength })
        {
            return Result.Failure<EmailAddress>(new Error(InvalidFormat));
        }

        string[] parts = value.Split('@');

        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length < 3)
        {
            return Result.Failure<EmailAddress>(new Error(InvalidFormat));
        }

        int dot = parts[1].IndexOf('.', StringComparison.Ordinal);

        if (dot <= 0 || dot == parts[1].Length - 1)
        {
            return Result.Failure<EmailAddress>(new Error(InvalidFormat));
        }

        if (value.Contains(' ', StringComparison.Ordinal))
        {
            return Result.Failure<EmailAddress>(new Error(InvalidFormat));
        }

        return Result.Success(new EmailAddress(value));
    }

    public override string ToString() => Value;
}
