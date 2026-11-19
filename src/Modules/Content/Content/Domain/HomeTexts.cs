namespace CarGallery.Modules.Content.Domain;

/// <summary>
/// `0022` R1 — ana sayfa metinleri: kurulumda TEK kayıt, metinler dil başına. Kayıt yalnız
/// galerinin yazdığı değeri taşır; boş alan "varsayılana düş" demektir ve varsayılan metin
/// çeviri kataloğunda kalır (C2, D3). Yayın akışı yoktur: kaydetmek yayındır (D10).
/// </summary>
internal sealed class HomeTexts
{
    /// <summary>Kurulumun tek kaydının sabit kimliği (migration tohumu).</summary>
    internal static readonly Guid SingletonId = Guid.Parse("0192c000-0000-7000-8000-0000c0a70100");

    private HomeTexts()
    {
    }

    /// <summary>Kaydın kimliği; her zaman <see cref="SingletonId"/>.</summary>
    internal Guid Id { get; private set; }

    /// <summary>`0022` R8 — iyimser eşzamanlılık sürümü; her kayıtta bir artar.</summary>
    internal int Version { get; private set; }

    /// <summary>Dil başına metinler; satırı olmayan dilin bütün alanları boştur.</summary>
    internal List<HomeTextLocalization> Localizations { get; private set; } = [];

    /// <summary>Kurulumun boş kaydı (testler için; üretimde migration tohumlar).</summary>
    internal static HomeTexts CreateEmpty() => new() { Id = SingletonId, Version = 1 };

    /// <summary>Dilin satırı; yoksa <c>null</c>.</summary>
    internal HomeTextLocalization? Localization(string locale) =>
        Localizations.Find(item => item.Locale == locale);

    /// <summary>Bir dilin altı alanını yazar. Doğrulama ve boşluk normalizasyonu çağıranın işidir.</summary>
    internal void SetTexts(string locale, IReadOnlyDictionary<string, string> values)
    {
        HomeTextLocalization? satir = Localization(locale);

        if (satir is null)
        {
            satir = new HomeTextLocalization(locale);
            Localizations.Add(satir);
        }

        foreach (HomeTextField alan in HomeTextField.All)
        {
            satir.Set(alan.Key, values.TryGetValue(alan.Key, out string? deger) ? deger : string.Empty);
        }
    }

    /// <summary>Bir sonraki sürüm; kayıt eşzamanlılık belirteciyle yazılır (R8).</summary>
    internal void Touch() => Version++;
}

/// <summary>Ana sayfa metinlerinin bir dildeki altı alanı; boş = varsayılan metin.</summary>
internal sealed class HomeTextLocalization(string locale)
{
    /// <summary>Dil kodu.</summary>
    internal string Locale { get; private set; } = locale;

    /// <summary>Giriş başlığı.</summary>
    internal string IntroTitle { get; private set; } = string.Empty;

    /// <summary>Giriş açıklaması; satır sonu korunur (R3).</summary>
    internal string IntroBody { get; private set; } = string.Empty;

    /// <summary>"Aracınızı bulun" bölüm başlığı.</summary>
    internal string FormTitle { get; private set; } = string.Empty;

    /// <summary>"Öne çıkan araçlar" bölüm başlığı.</summary>
    internal string FeaturedTitle { get; private set; } = string.Empty;

    /// <summary>Stok boşken gösterilen mesajın başlığı.</summary>
    internal string EmptyTitle { get; private set; } = string.Empty;

    /// <summary>Stok boşken gösterilen mesajın açıklaması; satır sonu korunur (R3).</summary>
    internal string EmptyBody { get; private set; } = string.Empty;

    /// <summary>Alanın değeri.</summary>
    internal string Get(string key) => key switch
    {
        HomeTextField.IntroTitleKey => IntroTitle,
        HomeTextField.IntroBodyKey => IntroBody,
        HomeTextField.FormTitleKey => FormTitle,
        HomeTextField.FeaturedTitleKey => FeaturedTitle,
        HomeTextField.EmptyTitleKey => EmptyTitle,
        HomeTextField.EmptyBodyKey => EmptyBody,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Tanınmayan ana sayfa alanı."),
    };

    /// <summary>Alanın değerini yazar.</summary>
    internal void Set(string key, string value)
    {
        switch (key)
        {
            case HomeTextField.IntroTitleKey:
                IntroTitle = value;
                break;
            case HomeTextField.IntroBodyKey:
                IntroBody = value;
                break;
            case HomeTextField.FormTitleKey:
                FormTitle = value;
                break;
            case HomeTextField.FeaturedTitleKey:
                FeaturedTitle = value;
                break;
            case HomeTextField.EmptyTitleKey:
                EmptyTitle = value;
                break;
            case HomeTextField.EmptyBodyKey:
                EmptyBody = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, "Tanınmayan ana sayfa alanı.");
        }
    }
}

/// <summary>
/// `0022` R1, R4 — altı alanın KİMLİĞİ ve sınırı kodda tek yerde. Anahtar API gövdesinin,
/// hata alanının (<c>tr.introTitle</c>) ve vitrin etiketinin ortak adıdır.
/// </summary>
internal sealed record HomeTextField(string Key, bool IsDescription)
{
    internal const string IntroTitleKey = "introTitle";
    internal const string IntroBodyKey = "introBody";
    internal const string FormTitleKey = "formTitle";
    internal const string FeaturedTitleKey = "featuredTitle";
    internal const string EmptyTitleKey = "emptyTitle";
    internal const string EmptyBodyKey = "emptyBody";

    /// <summary>R1'in sırasıyla altı alan.</summary>
    internal static IReadOnlyList<HomeTextField> All { get; } =
    [
        new(IntroTitleKey, false),
        new(IntroBodyKey, true),
        new(FormTitleKey, false),
        new(FeaturedTitleKey, false),
        new(EmptyTitleKey, false),
        new(EmptyBodyKey, true),
    ];

    /// <summary>R4 — alanın kod noktası sınırı: başlık 120, açıklama 500 (D5).</summary>
    internal int MaxLength => IsDescription ? ContentRules.HomeDescriptionMax : ContentRules.HomeTitleMax;

    /// <summary>Sınırı aşan değerin hata kodu.</summary>
    internal string TooLongCode => IsDescription ? ContentErrors.DescriptionTooLong : ContentErrors.TitleTooLong;
}
