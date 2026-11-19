namespace CarGallery.Modules.Content.Domain;

/// <summary>
/// `0019` — Yayınlanmış Metin Sürümü (`docs/domain.md`): KVKK aydınlatma metninin bir dilde
/// yayına girdiği andaki değişmez kopyası. Yalnız eklenir; düzenleyen ya da silen bir yol
/// yoktur ve veritabanı da reddeder (C5, D11, D18). Kişisel veri taşımaz — metni yayınlayan
/// personel de yazılmaz (C4, X7).
/// </summary>
internal sealed class PrivacyNoticeVersion
{
    private PrivacyNoticeVersion()
    {
        Locale = string.Empty;
        Title = string.Empty;
        Body = string.Empty;
    }

    /// <summary>Sürümün kimliği; talep bunu tutar (R3).</summary>
    internal Guid Id { get; private set; }

    /// <summary>Metnin dili.</summary>
    internal string Locale { get; private set; }

    /// <summary>O anki başlık, saklandığı gibi.</summary>
    internal string Title { get; private set; }

    /// <summary>O anki gövde, saklandığı gibi — kırpılmaz (D16).</summary>
    internal string Body { get; private set; }

    /// <summary>Kayda geçtiği an: işlemi yürüten saatin değeri (D26/B13).</summary>
    internal DateTimeOffset RecordedAt { get; private set; }

    /// <summary>
    /// Başlangıç sürümü mü (R7, D6): kart yayına girdiğinde zaten yayında olan metnin kaydı.
    /// Anı "bu andan itibaren kayıtlı" demektir, "bu anda yayımlandı" değil. Uygulama yolu
    /// başlangıç sürümü doğurmaz; onu yalnız migration yazar.
    /// </summary>
    internal bool IsInitial { get; private set; }

    /// <summary>Editörün yayınıyla doğan sürüm (R1).</summary>
    internal static PrivacyNoticeVersion Record(string locale, string title, string body, DateTimeOffset recordedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Locale = locale,
        Title = title,
        Body = body,
        RecordedAt = recordedAt,
        IsInitial = false,
    };
}
