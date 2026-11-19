namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// `0018` **D27** — İzin Olayı: istek, teyit ve çıkış; zamanı (UTC) ve abonelik kimliği.
/// <b>Adres YOKTUR</b> (C5) ve kayıt yalnız eklenir: veritabanı tetikleyicisi güncellemeyi ve
/// silmeyi reddeder (**D34**). Olası bir İYS entegrasyonu (X8) bu kayda dayanır.
/// </summary>
internal sealed class ConsentEvent
{
    internal Guid Id { get; init; }

    internal Guid SubscriptionId { get; init; }

    internal ConsentEventKind Kind { get; init; }

    internal DateTimeOffset OccurredAt { get; init; }

    internal static ConsentEvent Record(Guid subscriptionId, ConsentEventKind kind, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SubscriptionId = subscriptionId,
            Kind = kind,
            OccurredAt = now,
        };
}

internal enum ConsentEventKind
{
    Requested = 1,
    Confirmed = 2,
    Unsubscribed = 3,
}
