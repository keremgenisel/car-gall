namespace CarGallery.Modules.Content.Domain;

/// <summary>
/// `0017` — içerik sayfası: sistem sayfası (<see cref="SystemKey"/> dolu) ya da serbest
/// sayfa. Metin ve yayın durumu DİL BAŞINADIR (R4, R5); bir dilin satırı yoksa o dil
/// "metinsiz taslak"tır (R3). Eski slug'lar yalnız yayındaki dilde slug değişince
/// kaydedilir (R14, AC-53).
/// </summary>
internal sealed class ContentPage
{
    private ContentPage()
    {
    }

    /// <summary>Sayfanın kimliği.</summary>
    internal Guid Id { get; private set; }

    /// <summary>Sistem sayfası kimliği (<c>about</c>, <c>privacy</c>, <c>cookies</c>, <c>faq</c>); serbest sayfada <c>null</c>.</summary>
    internal string? SystemKey { get; private set; }

    /// <summary>`0017` R10/C14 — iyimser eşzamanlılık sürümü; her kayıtta bir artar.</summary>
    internal int Version { get; private set; }

    /// <summary>Oluşturulma anı; panel serbest sayfaları bu sırayla listeler.</summary>
    internal DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Dil başına metin ve durum.</summary>
    internal List<PageLocalization> Localizations { get; private set; } = [];

    /// <summary>Dil başına kaydedilmiş eski slug'lar (R14).</summary>
    internal List<PageOldSlug> OldSlugs { get; private set; } = [];

    /// <summary>Sistem sayfası mı (silinemez — R3).</summary>
    internal bool IsSystem => SystemKey is not null;

    /// <summary>Galerinin açtığı serbest sayfa (R2, D4).</summary>
    internal static ContentPage CreateFree(DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        SystemKey = null,
        Version = 1,
        CreatedAt = now,
    };

    /// <summary>Kurulumun metinsiz sistem sayfası (migration tohumu ve testler için).</summary>
    internal static ContentPage CreateSystem(Guid id, string key, DateTimeOffset now) => new()
    {
        Id = id,
        SystemKey = key,
        Version = 1,
        CreatedAt = now,
    };

    /// <summary>Dilin satırı; yoksa <c>null</c> (metinsiz taslak).</summary>
    internal PageLocalization? Localization(string locale) =>
        Localizations.Find(item => item.Locale == locale);

    /// <summary>Dil yayında mı.</summary>
    internal bool IsPublished(string locale) => Localization(locale)?.Status == PublicationStatus.Published;

    /// <summary>
    /// Bir dilin metnini yazar. Dil yayındaysa ve slug değişiyorsa ESKİ slug kaydedilir
    /// (R14); taslaktaki değişiklik kaydedilmez (AC-53). Sayfa kendi eski slug'ına
    /// dönerse o slug eski listeden çıkar (AC-55). Doğrulama çağıranın işidir (D30).
    /// </summary>
    internal void SetTexts(string locale, string title, string body, string slug)
    {
        PageLocalization? satir = Localization(locale);

        if (satir is null)
        {
            Localizations.Add(new PageLocalization(locale, title, body, slug, PublicationStatus.Draft));
            return;
        }

        if (satir.Status == PublicationStatus.Published
            && satir.Slug.Length > 0
            && !string.Equals(satir.Slug, slug, StringComparison.Ordinal)
            && !OldSlugs.Exists(item => item.Locale == locale && item.Slug == satir.Slug))
        {
            OldSlugs.Add(new PageOldSlug(locale, satir.Slug));
        }

        OldSlugs.RemoveAll(item => item.Locale == locale && item.Slug == slug);
        satir.Title = title;
        satir.Body = body;
        satir.Slug = slug;
    }

    /// <summary>R6 — dili yayına alır; şartı çağıran doğrular.</summary>
    internal void Publish(string locale) => Require(locale).Status = PublicationStatus.Published;

    /// <summary>R8 — dili taslağa döndürür; metin korunur. Satırı olmayan dil zaten taslaktır.</summary>
    internal void Unpublish(string locale)
    {
        if (Localization(locale) is PageLocalization satir)
        {
            satir.Status = PublicationStatus.Draft;
        }
    }

    /// <summary>Bir sonraki sürüm; kayıt eşzamanlılık belirteciyle yazılır (C14).</summary>
    internal void Touch() => Version++;

    private PageLocalization Require(string locale)
    {
        PageLocalization? satir = Localization(locale);

        if (satir is null)
        {
            satir = new PageLocalization(locale, string.Empty, string.Empty, string.Empty, PublicationStatus.Draft);
            Localizations.Add(satir);
        }

        return satir;
    }
}

/// <summary>Sayfanın bir dildeki metni ve durumu.</summary>
internal sealed class PageLocalization(string locale, string title, string body, string slug, PublicationStatus status)
{
    /// <summary>Dil kodu.</summary>
    internal string Locale { get; private set; } = locale;

    /// <summary>Başlık; boş = girilmemiş.</summary>
    internal string Title { get; set; } = title;

    /// <summary>Gövde (R16'nın alt kümesi, ham).</summary>
    internal string Body { get; set; } = body;

    /// <summary>Latin slug; boş = girilmemiş (C7).</summary>
    internal string Slug { get; set; } = slug;

    /// <summary>Yayın durumu.</summary>
    internal PublicationStatus Status { get; set; } = status;

    /// <summary>R6 — üç alanın da dolu olup olmadığı.</summary>
    internal bool IsComplete => Title.Length > 0 && Body.Length > 0 && Slug.Length > 0;
}

/// <summary>Bir dilde kaydedilmiş eski slug (R14).</summary>
internal sealed record PageOldSlug(string Locale, string Slug);

/// <summary>
/// `0017` R18/D20 — SSS girdisi tek birimdir: soru, cevap ve durum dil başına; SIRA tek ve
/// globaldir.
/// </summary>
internal sealed class FaqEntry
{
    private FaqEntry()
    {
    }

    /// <summary>Girdinin kimliği.</summary>
    internal Guid Id { get; private set; }

    /// <summary>Global sıra; küçük önce.</summary>
    internal int Position { get; set; }

    /// <summary>İyimser eşzamanlılık sürümü (R10).</summary>
    internal int Version { get; private set; }

    /// <summary>Dil başına soru, cevap ve durum.</summary>
    internal List<FaqLocalization> Localizations { get; private set; } = [];

    /// <summary>Yeni girdi; sıranın SONUNA eklenir (D36).</summary>
    internal static FaqEntry Create(int position) => new()
    {
        Id = Guid.CreateVersion7(),
        Position = position,
        Version = 1,
    };

    /// <summary>Dilin satırı; yoksa <c>null</c>.</summary>
    internal FaqLocalization? Localization(string locale) =>
        Localizations.Find(item => item.Locale == locale);

    /// <summary>Dilin soru ve cevabını yazar.</summary>
    internal void SetTexts(string locale, string question, string answer)
    {
        if (Localization(locale) is FaqLocalization satir)
        {
            satir.Question = question;
            satir.Answer = answer;
            return;
        }

        Localizations.Add(new FaqLocalization(locale, question, answer, PublicationStatus.Draft));
    }

    /// <summary>Dili yayına alır; şartı çağıran doğrular (D29).</summary>
    internal void Publish(string locale)
    {
        FaqLocalization? satir = Localization(locale);

        if (satir is null)
        {
            satir = new FaqLocalization(locale, string.Empty, string.Empty, PublicationStatus.Draft);
            Localizations.Add(satir);
        }

        satir.Status = PublicationStatus.Published;
    }

    /// <summary>Dili taslağa döndürür.</summary>
    internal void Unpublish(string locale)
    {
        if (Localization(locale) is FaqLocalization satir)
        {
            satir.Status = PublicationStatus.Draft;
        }
    }

    /// <summary>Bir sonraki sürüm.</summary>
    internal void Touch() => Version++;
}

/// <summary>SSS girdisinin bir dildeki soru ve cevabı.</summary>
internal sealed class FaqLocalization(string locale, string question, string answer, PublicationStatus status)
{
    /// <summary>Dil kodu.</summary>
    internal string Locale { get; private set; } = locale;

    /// <summary>Soru; boş = girilmemiş.</summary>
    internal string Question { get; set; } = question;

    /// <summary>Cevap (R16'nın alt kümesi).</summary>
    internal string Answer { get; set; } = answer;

    /// <summary>Yayın durumu.</summary>
    internal PublicationStatus Status { get; set; } = status;

    /// <summary>D29 — soru ve cevabın ikisi de dolu mu.</summary>
    internal bool IsComplete => Question.Length > 0 && Answer.Length > 0;
}
