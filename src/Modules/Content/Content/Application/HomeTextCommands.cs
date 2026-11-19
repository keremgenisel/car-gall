using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Content.Application;

/// <summary>Bir dilin ana sayfa metinleri girdisi: alan anahtarı → değer; eksik anahtar boştur.</summary>
internal sealed record HomeTextInput(string Locale, IReadOnlyDictionary<string, string?> Values);

/// <summary>
/// `0022` — ana sayfa metinlerinin yazma yolu (C1). Kayıt bir sürüm taşır; okunan sürüm
/// güncel değilse ya da aynı sürümden paralel bir kayıt önce yazdıysa <c>concurrency.lost</c>
/// (R8 — `409`). Her alan opsiyoneldir; yalnız boşluktan oluşan değer boştur ve boş olmayan
/// değer kırpılmaz (R2, D14). Uzunluk kod noktasıyla sayılır (R4, D15). Bir alan bile
/// reddedilirse hiçbir dil yazılmaz.
/// </summary>
internal sealed class HomeTextCommands(ContentDbContext context, ContentLocaleOptions locales)
{
    /// <summary>Verilen dillerin altı alanını yazar; verilmeyen dil olduğu gibi kalır.</summary>
    internal async Task<Result<int>> SaveAsync(
        int version,
        IReadOnlyList<HomeTextInput> texts,
        CancellationToken cancellationToken)
    {
        HomeTexts kayit = await context.HomeTexts.FirstAsync(item => item.Id == HomeTexts.SingletonId, cancellationToken);

        if (kayit.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        FieldErrors errors = Validate(texts);

        if (errors.Any)
        {
            return Result.Failure<int>(errors.ToError());
        }

        foreach (HomeTextInput input in texts)
        {
            kayit.SetTexts(
                input.Locale,
                HomeTextField.All.ToDictionary(
                    alan => alan.Key,
                    alan => ContentRules.Normalize(input.Values.GetValueOrDefault(alan.Key)),
                    StringComparer.Ordinal));
        }

        kayit.Touch();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Ölçüldü (AC-25'in paralel testi): dilin satırı henüz yokken aynı sürümden iki
            // kayıt İKİSİ DE satır ekler; ikincisi sürüm kontrolüne değil satırın birincil
            // anahtarına takılır. Sebep aynı — paralel kayıt kaybedildi — ve sonuç da aynı: `409`.
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        return Result.Success(kayit.Version);
    }

    private FieldErrors Validate(IReadOnlyList<HomeTextInput> texts)
    {
        FieldErrors errors = new();

        foreach (HomeTextInput input in texts)
        {
            if (!locales.IsEnabled(input.Locale))
            {
                errors.Add(input.Locale, ContentErrors.LocaleNotEnabled);
                continue;
            }

            foreach (HomeTextField alan in HomeTextField.All)
            {
                string deger = ContentRules.Normalize(input.Values.GetValueOrDefault(alan.Key));

                if (ContentRules.Length(deger) > alan.MaxLength)
                {
                    errors.Add($"{input.Locale}.{alan.Key}", alan.TooLongCode);
                }
            }
        }

        return errors;
    }
}
