using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Personelin giriş kimliği (e-posta). İki değer taşır ve ikisi de gereklidir:
/// ziyaretçinin YAZDIĞI hâl ve tekilliğin kurulduğu NORMALİZE hâl (R1).
///
/// Normalizasyon <see cref="string.ToLowerInvariant"/> ile, yani KÜLTÜRE DUYARSIZ
/// yapılır. `ToLower()` veya SQL `lower()` Türkçe collation altında `I → ı` üretir;
/// `ALI@x.com` ile `ali@x.com` ayrı hesaplara düşer ya da `İSMAIL@x.com` ile
/// `ismail@x.com` çakışır (Yasak Listesi 27, Tuzak 9, AC-11..AC-13). Buradaki bedel bir
/// liste hatası değil, **iki kişinin aynı hesaba düşmesidir**.
///
/// Eşleştirme normalize değer üzerinden ve ORDINAL yapılır; veritabanında da arama
/// normalize kolon üzerindedir — ham kolon üzerinde `ILIKE`/`lower()` yasaktır
/// (docs/conventions.md § 6).
/// </summary>
internal readonly record struct LoginId
{
    internal const string InvalidFormat = "identity.invalid_login_id";

    /// <summary>RFC 5321'in yol (path) üst sınırı.</summary>
    internal const int MaximumLength = 254;

    private LoginId(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    /// <summary>Personelin yazdığı hâl; ekranda bu gösterilir.</summary>
    internal string Value { get; init; }

    /// <summary>Tekilliğin ve eşleştirmenin kurulduğu hâl.</summary>
    internal string Normalized { get; init; }

    internal static Result<LoginId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<LoginId>(new Error(InvalidFormat));
        }

        string trimmed = value.Trim();

        if (trimmed.Length > MaximumLength || !IsEmailShaped(trimmed))
        {
            return Result.Failure<LoginId>(new Error(InvalidFormat));
        }

        return Result.Success(new LoginId(trimmed, trimmed.ToLowerInvariant()));
    }

    /// <summary>
    /// Dar ve okunabilir: tek `@`, iki yanında da içerik, alan adında nokta, boşluk yok.
    /// RFC 5322'yi tam uygulayan bir regex bu kriteri daha doğru yapmaz, yalnız okunamaz
    /// yapar.
    /// </summary>
    private static bool IsEmailShaped(string value)
    {
        string[] parts = value.Split('@');

        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length < 3)
        {
            return false;
        }

        int dot = parts[1].IndexOf('.', StringComparison.Ordinal);

        if (dot <= 0 || dot == parts[1].Length - 1)
        {
            return false;
        }

        return !value.Contains(' ', StringComparison.Ordinal);
    }

    public override string ToString() => Value;
}
