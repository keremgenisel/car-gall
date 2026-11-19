namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// `0018` **D13** — Bülten Dönemi. İki tür kayıt var:
/// <list type="bullet">
/// <item><see cref="NewsletterPeriodKind.Baseline"/> — bu kartın ilk açılışında bir kez
/// yazılır; zamanı DEVREYE GİRİŞ ANIDIR ve aday eşiği odur (**D4**). Sonraki açılışlar onu
/// değiştirmez (AC-72).</item>
/// <item><see cref="NewsletterPeriodKind.Regular"/> — işlenmiş bir dönem anı. Aynı an iki kez
/// işlenmez: tekillik veritabanındadır (AC-69, AC-70).</item>
/// </list>
/// </summary>
internal sealed class NewsletterPeriod
{
    internal Guid Id { get; init; }

    internal NewsletterPeriodKind Kind { get; init; }

    /// <summary>Dönem anı (UTC, C9). Başlangıç kaydında devreye giriş anı.</summary>
    internal DateTimeOffset CutoffAt { get; init; }

    internal DateTimeOffset ProcessedAt { get; init; }

    internal static NewsletterPeriod Baseline(DateTimeOffset now) =>
        new() { Id = Guid.CreateVersion7(), Kind = NewsletterPeriodKind.Baseline, CutoffAt = now, ProcessedAt = now };

    internal static NewsletterPeriod Regular(DateTimeOffset cutoffAt, DateTimeOffset now) =>
        new() { Id = Guid.CreateVersion7(), Kind = NewsletterPeriodKind.Regular, CutoffAt = cutoffAt, ProcessedAt = now };
}

internal enum NewsletterPeriodKind
{
    Baseline = 1,
    Regular = 2,
}

/// <summary>
/// `0018` **D21** — dönem ↔ ilan: dönem işlenirken Yayında olan ve o döneme atanan ilan.
/// Dağıtımda küme yeniden hesaplanmaz; bu kayıttan okunur. Kişisel veri taşımaz.
/// </summary>
internal sealed class NewsletterPeriodListing
{
    internal Guid PeriodId { get; init; }

    internal Guid ListingId { get; init; }

    /// <summary>Sıralama için ilk yayın zamanı (**D11**: ilk yayını en yeni önce).</summary>
    internal DateTimeOffset PublishedAt { get; init; }
}

/// <summary>
/// `0018` **D4** — Bülten Adayı: ilk kez yayınlanmış ve henüz bir döneme atanmamış ilan.
/// İlan kimliği tekildir: yeniden yayın ve tekrar teslim ikinci aday üretmez.
/// </summary>
internal sealed class NewsletterCandidate
{
    internal Guid ListingId { get; init; }

    internal DateTimeOffset PublishedAt { get; init; }

    internal DateTimeOffset RecordedAt { get; init; }

    internal NewsletterCandidateStatus Status { get; private set; }

    internal Guid? PeriodId { get; private set; }

    internal static NewsletterCandidate Record(Guid listingId, DateTimeOffset publishedAt, DateTimeOffset now) =>
        new()
        {
            ListingId = listingId,
            PublishedAt = publishedAt,
            RecordedAt = now,
            Status = NewsletterCandidateStatus.Pending,
        };

    internal void AssignTo(Guid periodId)
    {
        Status = NewsletterCandidateStatus.Assigned;
        PeriodId = periodId;
    }

    /// <summary>**D21** — dönem anında Yayında değildi; bir sonraki döneme KALMAZ.</summary>
    internal void Eliminate(Guid periodId)
    {
        Status = NewsletterCandidateStatus.Eliminated;
        PeriodId = periodId;
    }
}

internal enum NewsletterCandidateStatus
{
    Pending = 1,
    Assigned = 2,
    Eliminated = 3,
}
