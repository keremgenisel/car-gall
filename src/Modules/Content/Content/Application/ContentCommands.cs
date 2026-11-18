using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Content.Application;

/// <summary>Bir dilin sayfa metni girdisi; <c>null</c> ve boşluk = boş (D32).</summary>
internal sealed record PageTextInput(string Locale, string? Title, string? Body, string? Slug);

/// <summary>Bir dilin SSS girdisi metni.</summary>
internal sealed record FaqTextInput(string Locale, string? Question, string? Answer);

/// <summary>
/// `0017` — `Content`'in yazma yolu (C1). Her yazma bir sürüm taşır; okunan sürüm güncel
/// değilse ya da aynı sürümden paralel bir kayıt önce yazdıysa `concurrency.lost` (R10,
/// C14 — `409`). Dolu her alan her kayıtta doğrulanır, taslak boş alan taşıyabilir (D30);
/// yayındaki dilde zorunlu alan boşaltılamaz (D31).
/// </summary>
internal sealed class ContentCommands(ContentDbContext context, ContentLocaleOptions locales, IClock clock)
{
    /// <summary>Serbest sayfa oluşturur; bütün dilleri taslaktır (R1, AC-2).</summary>
    internal async Task<Result<(Guid Id, int Version)>> CreatePageAsync(
        IReadOnlyList<PageTextInput> texts,
        CancellationToken cancellationToken)
    {
        ContentPage page = ContentPage.CreateFree(clock.UtcNow);
        FieldErrors errors = await ValidatePageTextsAsync(page, texts, cancellationToken);

        if (errors.Any)
        {
            return Result.Failure<(Guid, int)>(errors.ToError());
        }

        Apply(page, texts);
        context.Pages.Add(page);

        Result kayit = await SaveAsync(texts.Select(item => $"{item.Locale}.slug"), cancellationToken);

        return kayit.IsSuccess ? Result.Success((page.Id, page.Version)) : Result.Failure<(Guid, int)>(kayit.Error);
    }

    /// <summary>Sayfanın verilen dillerdeki metnini yazar; yayındaki dilde hemen yayına girer (R7).</summary>
    internal async Task<Result<int>> SavePageAsync(
        Guid id,
        int version,
        IReadOnlyList<PageTextInput> texts,
        CancellationToken cancellationToken)
    {
        ContentPage? page = await LoadPageAsync(id, cancellationToken);

        if (page is null)
        {
            return Result.Failure<int>(new Error(ContentErrors.PageNotFound));
        }

        if (page.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        FieldErrors errors = await ValidatePageTextsAsync(page, texts, cancellationToken);

        if (errors.Any)
        {
            return Result.Failure<int>(errors.ToError());
        }

        Apply(page, texts);
        page.Touch();

        Result kayit = await SaveAsync(texts.Select(item => $"{item.Locale}.slug"), cancellationToken);

        return kayit.IsSuccess ? Result.Success(page.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>R6 — dili yayına alır; başlık, gövde ve slug'ın üçü de dolu olmalı.</summary>
    internal async Task<Result<int>> PublishPageAsync(Guid id, int version, string locale, CancellationToken cancellationToken)
    {
        ContentPage? page = await LoadPageAsync(id, cancellationToken);
        Error? hata = Precheck(page is null ? null : page.Version, version, locale, ContentErrors.PageNotFound);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        PageLocalization? satir = page!.Localization(locale);
        FieldErrors errors = new();

        if (satir is null || satir.Title.Length == 0)
        {
            errors.Add($"{locale}.title", ContentErrors.FieldRequired);
        }

        if (satir is null || satir.Body.Length == 0)
        {
            errors.Add($"{locale}.body", ContentErrors.FieldRequired);
        }

        if (satir is null || satir.Slug.Length == 0)
        {
            errors.Add($"{locale}.slug", ContentErrors.FieldRequired);
        }

        if (errors.Any)
        {
            return Result.Failure<int>(errors.ToError());
        }

        page.Publish(locale);
        page.Touch();

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success(page.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>R8 — dili taslağa döndürür; metin korunur. Sistem sayfasında da yapılabilir (AC-23).</summary>
    internal async Task<Result<int>> UnpublishPageAsync(Guid id, int version, string locale, CancellationToken cancellationToken)
    {
        ContentPage? page = await LoadPageAsync(id, cancellationToken);
        Error? hata = Precheck(page is null ? null : page.Version, version, locale, ContentErrors.PageNotFound);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        page!.Unpublish(locale);
        page.Touch();

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success(page.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>R8 — kalıcı silme; yalnız serbest sayfa. Eski slug'lar da gider (D13).</summary>
    internal async Task<Result> DeletePageAsync(Guid id, int version, CancellationToken cancellationToken)
    {
        ContentPage? page = await LoadPageAsync(id, cancellationToken);

        if (page is null)
        {
            return Result.Failure(new Error(ContentErrors.PageNotFound));
        }

        if (page.IsSystem)
        {
            return Result.Failure(new Error(ContentErrors.SystemPageNotDeletable));
        }

        if (page.Version != version)
        {
            return Result.Failure(new Error(ConcurrencyErrors.Lost));
        }

        context.Pages.Remove(page);

        return await SaveAsync([], cancellationToken);
    }

    /// <summary>SSS girdisi oluşturur; sıranın sonuna eklenir (D36).</summary>
    internal async Task<Result<(Guid Id, int Version)>> CreateFaqEntryAsync(
        IReadOnlyList<FaqTextInput> texts,
        CancellationToken cancellationToken)
    {
        FaqEntry entry = FaqEntry.Create(await NextPositionAsync(cancellationToken));
        FieldErrors errors = ValidateFaqTexts(entry, texts);

        if (errors.Any)
        {
            return Result.Failure<(Guid, int)>(errors.ToError());
        }

        Apply(entry, texts);
        context.FaqEntries.Add(entry);

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success((entry.Id, entry.Version)) : Result.Failure<(Guid, int)>(kayit.Error);
    }

    /// <summary>SSS girdisinin metnini yazar (R10'un kuralı SSS için de geçerli).</summary>
    internal async Task<Result<int>> SaveFaqEntryAsync(
        Guid id,
        int version,
        IReadOnlyList<FaqTextInput> texts,
        CancellationToken cancellationToken)
    {
        FaqEntry? entry = await LoadFaqAsync(id, cancellationToken);

        if (entry is null)
        {
            return Result.Failure<int>(new Error(ContentErrors.FaqEntryNotFound));
        }

        if (entry.Version != version)
        {
            return Result.Failure<int>(new Error(ConcurrencyErrors.Lost));
        }

        FieldErrors errors = ValidateFaqTexts(entry, texts);

        if (errors.Any)
        {
            return Result.Failure<int>(errors.ToError());
        }

        Apply(entry, texts);
        entry.Touch();

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success(entry.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>D29 — SSS girdisini yayına alır; soru ve cevabın ikisi de dolu olmalı.</summary>
    internal async Task<Result<int>> PublishFaqEntryAsync(Guid id, int version, string locale, CancellationToken cancellationToken)
    {
        FaqEntry? entry = await LoadFaqAsync(id, cancellationToken);
        Error? hata = Precheck(entry is null ? null : entry.Version, version, locale, ContentErrors.FaqEntryNotFound);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        FaqLocalization? satir = entry!.Localization(locale);
        FieldErrors errors = new();

        if (satir is null || satir.Question.Length == 0)
        {
            errors.Add($"{locale}.question", ContentErrors.FieldRequired);
        }

        if (satir is null || satir.Answer.Length == 0)
        {
            errors.Add($"{locale}.answer", ContentErrors.FieldRequired);
        }

        if (errors.Any)
        {
            return Result.Failure<int>(errors.ToError());
        }

        entry.Publish(locale);
        entry.Touch();

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success(entry.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>SSS girdisinin dilini taslağa döndürür.</summary>
    internal async Task<Result<int>> UnpublishFaqEntryAsync(Guid id, int version, string locale, CancellationToken cancellationToken)
    {
        FaqEntry? entry = await LoadFaqAsync(id, cancellationToken);
        Error? hata = Precheck(entry is null ? null : entry.Version, version, locale, ContentErrors.FaqEntryNotFound);

        if (hata is not null)
        {
            return Result.Failure<int>(hata);
        }

        entry!.Unpublish(locale);
        entry.Touch();

        Result kayit = await SaveAsync([], cancellationToken);

        return kayit.IsSuccess ? Result.Success(entry.Version) : Result.Failure<int>(kayit.Error);
    }

    /// <summary>R8 — SSS girdisini kalıcı siler.</summary>
    internal async Task<Result> DeleteFaqEntryAsync(Guid id, int version, CancellationToken cancellationToken)
    {
        FaqEntry? entry = await LoadFaqAsync(id, cancellationToken);

        if (entry is null)
        {
            return Result.Failure(new Error(ContentErrors.FaqEntryNotFound));
        }

        if (entry.Version != version)
        {
            return Result.Failure(new Error(ConcurrencyErrors.Lost));
        }

        context.FaqEntries.Remove(entry);

        return await SaveAsync([], cancellationToken);
    }

    /// <summary>
    /// R18/D36 — girdiyi bir yukarı ya da aşağı taşır: komşusuyla yer değiştirir. Uçtaki
    /// girdi o yönde taşınmaz ve sıra dönmez; istek başarılıdır ama sıra değişmez (AC-78).
    /// Sıra metnin sürümünü değiştirmez: sıralamak, açık bir düzenleme formunu bayatlatmaz.
    /// </summary>
    internal async Task<Result> MoveFaqEntryAsync(Guid id, bool up, CancellationToken cancellationToken)
    {
        List<FaqEntry> sira = await context.FaqEntries
            .OrderBy(entry => entry.Position)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

        int index = sira.FindIndex(entry => entry.Id == id);

        if (index < 0)
        {
            return Result.Failure(new Error(ContentErrors.FaqEntryNotFound));
        }

        int komsu = up ? index - 1 : index + 1;

        if (komsu < 0 || komsu >= sira.Count)
        {
            return Result.Success();
        }

        // Konumlar önce 0..n-1'e normalleştirilir: silmenin bıraktığı boşluk ya da eşit
        // konum, yer değiştirmeyi etkisiz kılmasın.
        for (int i = 0; i < sira.Count; i++)
        {
            sira[i].Position = i;
        }

        (sira[index].Position, sira[komsu].Position) = (sira[komsu].Position, sira[index].Position);

        return await SaveAsync([], cancellationToken);
    }

    private Error? Precheck(int? currentVersion, int version, string locale, string notFoundCode)
    {
        if (currentVersion is null)
        {
            return new Error(notFoundCode);
        }

        if (!locales.IsEnabled(locale))
        {
            FieldErrors errors = new();
            errors.Add(locale, ContentErrors.LocaleNotEnabled);
            return errors.ToError();
        }

        return currentVersion != version ? new Error(ConcurrencyErrors.Lost) : null;
    }

    private async Task<FieldErrors> ValidatePageTextsAsync(
        ContentPage page,
        IReadOnlyList<PageTextInput> texts,
        CancellationToken cancellationToken)
    {
        FieldErrors errors = new();

        foreach (PageTextInput input in texts)
        {
            if (!locales.IsEnabled(input.Locale))
            {
                errors.Add(input.Locale, ContentErrors.LocaleNotEnabled);
                continue;
            }

            string title = ContentRules.Normalize(input.Title);
            string body = ContentRules.Normalize(input.Body);
            string slug = ContentRules.Normalize(input.Slug);
            string onek = input.Locale;

            if (ContentRules.Length(title) > ContentRules.TitleMax)
            {
                errors.Add($"{onek}.title", ContentErrors.TitleTooLong);
            }

            if (ContentRules.Length(body) > ContentRules.BodyMax)
            {
                errors.Add($"{onek}.body", ContentErrors.BodyTooLong);
            }
            else if (ContentRules.LinkError(body) is string bodyError)
            {
                errors.Add($"{onek}.body", bodyError);
            }

            if (slug.Length > 0)
            {
                if (ContentRules.SlugError(slug) is string slugError)
                {
                    errors.Add($"{onek}.slug", slugError);
                }
                else if (await SlugTakenAsync(page.Id, onek, slug, cancellationToken))
                {
                    errors.Add($"{onek}.slug", ContentErrors.SlugTaken);
                }
            }

            // D31 — yayındaki dilde zorunlu alan boşaltılamaz; dil sessizce taslağa düşmez.
            if (page.IsPublished(onek))
            {
                if (title.Length == 0)
                {
                    errors.Add($"{onek}.title", ContentErrors.FieldRequired);
                }

                if (body.Length == 0)
                {
                    errors.Add($"{onek}.body", ContentErrors.FieldRequired);
                }

                if (slug.Length == 0)
                {
                    errors.Add($"{onek}.slug", ContentErrors.FieldRequired);
                }
            }
        }

        return errors;
    }

    private FieldErrors ValidateFaqTexts(FaqEntry entry, IReadOnlyList<FaqTextInput> texts)
    {
        FieldErrors errors = new();

        foreach (FaqTextInput input in texts)
        {
            if (!locales.IsEnabled(input.Locale))
            {
                errors.Add(input.Locale, ContentErrors.LocaleNotEnabled);
                continue;
            }

            string question = ContentRules.Normalize(input.Question);
            string answer = ContentRules.Normalize(input.Answer);
            string onek = input.Locale;

            if (ContentRules.Length(question) > ContentRules.QuestionMax)
            {
                errors.Add($"{onek}.question", ContentErrors.QuestionTooLong);
            }

            if (ContentRules.Length(answer) > ContentRules.AnswerMax)
            {
                errors.Add($"{onek}.answer", ContentErrors.AnswerTooLong);
            }
            else if (ContentRules.LinkError(answer) is string answerError)
            {
                errors.Add($"{onek}.answer", answerError);
            }

            if (entry.Localization(onek)?.Status == PublicationStatus.Published)
            {
                if (question.Length == 0)
                {
                    errors.Add($"{onek}.question", ContentErrors.FieldRequired);
                }

                if (answer.Length == 0)
                {
                    errors.Add($"{onek}.answer", ContentErrors.FieldRequired);
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// R13/R14/D30 — slug o dilde başka bir sayfanın (taslak dahil) güncel slug'ı ya da
    /// kaydedilmiş eski slug'ı mı. Sayfanın KENDİ eski slug'ı serbesttir (AC-55).
    /// </summary>
    private async Task<bool> SlugTakenAsync(Guid pageId, string locale, string slug, CancellationToken cancellationToken)
    {
        bool guncel = await context.Pages
            .Where(page => page.Id != pageId)
            .SelectMany(page => page.Localizations)
            .AnyAsync(satir => satir.Locale == locale && satir.Slug == slug, cancellationToken);

        if (guncel)
        {
            return true;
        }

        return await context.Pages
            .Where(page => page.Id != pageId)
            .SelectMany(page => page.OldSlugs)
            .AnyAsync(eski => eski.Locale == locale && eski.Slug == slug, cancellationToken);
    }

    private static void Apply(ContentPage page, IEnumerable<PageTextInput> texts)
    {
        foreach (PageTextInput input in texts)
        {
            page.SetTexts(
                input.Locale,
                ContentRules.Normalize(input.Title),
                ContentRules.Normalize(input.Body),
                ContentRules.Normalize(input.Slug));
        }
    }

    private static void Apply(FaqEntry entry, IEnumerable<FaqTextInput> texts)
    {
        foreach (FaqTextInput input in texts)
        {
            entry.SetTexts(input.Locale, ContentRules.Normalize(input.Question), ContentRules.Normalize(input.Answer));
        }
    }

    private async Task<int> NextPositionAsync(CancellationToken cancellationToken) =>
        (await context.FaqEntries.MaxAsync(entry => (int?)entry.Position, cancellationToken) ?? -1) + 1;

    private Task<ContentPage?> LoadPageAsync(Guid id, CancellationToken cancellationToken) =>
        context.Pages.FirstOrDefaultAsync(page => page.Id == id, cancellationToken);

    private Task<FaqEntry?> LoadFaqAsync(Guid id, CancellationToken cancellationToken) =>
        context.FaqEntries.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    /// <summary>
    /// Kaydeder. Paralel kayıt kaybı (`DbUpdateConcurrencyException`) `409` olur (AC-32);
    /// iki sayfanın aynı anda aynı slug'ı alma yarışını veritabanının tekil indeksi yakalar
    /// ve o da slug alanının hatası olur.
    /// </summary>
    private async Task<Result> SaveAsync(IEnumerable<string> slugFields, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error(ConcurrencyErrors.Lost));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            FieldErrors errors = new();

            foreach (string field in slugFields)
            {
                errors.Add(field, ContentErrors.SlugTaken);
            }

            return Result.Failure(errors.Any ? errors.ToError() : new Error(ConcurrencyErrors.Lost));
        }
    }
}
