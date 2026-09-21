namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Yayıncının KENDİ şemasındaki outbox satırı (architecture.md § 2.C). İş verisiyle AYNI
/// transaction'da yazılır: değişiklik kaydedildiyse duyuru da kaydedilmiştir, değişiklik
/// geri alındıysa duyuru da alınmamıştır (R11, AC-25).
/// </summary>
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }

    /// <summary>Olayın sözleşme adı; dağıtıcı tipi bundan çözer.</summary>
    public required string Type { get; init; }

    public required string Payload { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>D5(b) — yeniden denemeler artan aralıkla yapılır.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>D5(c) — denemeler tükendiğinde işaretlenir; satır SESSİZCE düşürülmez.</summary>
    public DateTimeOffset? FailedAt { get; set; }

    public string? LastError { get; set; }
}

/// <summary>
/// Alıcı tarafında işlenmiş olay kaydı (D4). Koruma olayın KİMLİĞİNE dayanır, alıcının
/// işleminin rastlantısal olarak tekrara dayanıklı olmasına değil: bugünkü iki tüketici
/// doğaları gereği idempotent ama bu bir tasarım değil şanstır — sayaç artıran veya
/// bildirim gönderen ilk tüketici geldiğinde kırılır.
///
/// Alıcının KENDİ şemasında yaşar; başka modülün inbox'ına yazılmaz (Yasak Listesi 16).
/// </summary>
public sealed class InboxEntry
{
    public required Guid EventId { get; init; }

    /// <summary>Aynı olayı birden çok alıcı işler; koruma alıcı başınadır.</summary>
    public required string HandlerName { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }
}
