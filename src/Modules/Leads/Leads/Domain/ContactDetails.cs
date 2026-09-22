using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Talebin KİŞİSEL alanları (R5, R13). Ayrı bir tip olmalarının tek nedeni budur: kayıt,
/// kişisel alanlarını istatistiksel alanlarından ayırt edecek biçimde durmak zorundadır
/// (docs/domain.md **L6**, AC-29). Dört alan da boşaltılabilir; boşaltma kaydı silmez.
///
/// Dördü de null olabilir. Bu, doğrulamanın gevşek olmasından değil, anonimleştirmenin
/// bir SONRAKİ hâl olmasından kaynaklanır: <see cref="Create"/> R4'ü dayatır,
/// <see cref="Erase"/> onu geri alır.
/// </summary>
internal readonly record struct ContactDetails
{
    internal const int MaximumNameLength = 120;
    internal const int MaximumMessageLength = 2000;

    private ContactDetails(string? fullName, string? phone, string? email, string? message)
    {
        FullName = fullName;
        Phone = phone;
        Email = email;
        Message = message;
    }

    internal string? FullName { get; init; }

    /// <summary>E.164 biçiminde (C8); doğrulaması <see cref="PhoneNumber"/>'dadır.</summary>
    internal string? Phone { get; init; }

    internal string? Email { get; init; }

    /// <summary>R5 — opsiyonel serbest metin; mesajsız talep kaydedilir (AC-7).</summary>
    internal string? Message { get; init; }

    /// <summary>
    /// R4 — ad zorunlu, telefon VEYA e-postadan en az biri zorunlu. Doğrulama sırası
    /// ziyaretçinin formu doldurma sırasını izler: önce ad, sonra kanallar, sonra mesaj.
    /// </summary>
    internal static Result<ContactDetails> Create(
        string? fullName,
        string? phone,
        string? email,
        string? message)
    {
        string? name = Normalize(fullName);

        if (name is null)
        {
            return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.MissingName, "fullName"));
        }

        if (name.Length > MaximumNameLength)
        {
            return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.NameTooLong, "fullName"));
        }

        string? phoneInput = Normalize(phone);
        string? emailInput = Normalize(email);

        if (phoneInput is null && emailInput is null)
        {
            return Result.Failure<ContactDetails>(
                LeadErrors.ForField(LeadErrors.MissingContactChannel, "phone"));
        }

        string? storedPhone = null;

        if (phoneInput is not null)
        {
            Result<PhoneNumber> parsed = PhoneNumber.Create(phoneInput);

            if (parsed.IsFailure)
            {
                return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.InvalidPhone, "phone"));
            }

            storedPhone = parsed.Value.Value;
        }

        string? storedEmail = null;

        if (emailInput is not null)
        {
            if (emailInput.Length > EmailAddress.MaximumLength)
            {
                return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.EmailTooLong, "email"));
            }

            Result<EmailAddress> parsed = EmailAddress.Create(emailInput);

            if (parsed.IsFailure)
            {
                return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.InvalidEmail, "email"));
            }

            storedEmail = parsed.Value.Value;
        }

        string? storedMessage = Normalize(message);

        if (storedMessage is { Length: > MaximumMessageLength })
        {
            return Result.Failure<ContactDetails>(LeadErrors.ForField(LeadErrors.MessageTooLong, "message"));
        }

        return Result.Success(new ContactDetails(name, storedPhone, storedEmail, storedMessage));
    }

    /// <summary>
    /// KVKK anonimleştirmesinin kişisel yarısı (**L6**, R13, AC-29). Yalnız bu dört alanı
    /// boşaltır; kaydın kendisi ve istatistiksel alanları ona hiç dokunmadan kalır.
    /// Eylemi çalıştıracak yönetim yüzeyi bu spec'te YOKTUR (X4) — burada duran yetenektir,
    /// çünkü AC-29 "boşaltıldığında" diyor ve bunu kanıtlayacak bir yol gerekir.
    /// </summary>
    internal ContactDetails Erase() => new(null, null, null, null);

    internal bool IsErased() =>
        FullName is null && Phone is null && Email is null && Message is null;

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
