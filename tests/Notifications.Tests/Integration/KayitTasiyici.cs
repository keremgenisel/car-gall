using CarGallery.Modules.Notifications.Application;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>Gönderilenleri tutan taşıyıcı; kuralı testin verdiği sonuca göre döner.</summary>
internal sealed class KayitTasiyici : INotificationTransport
{
    internal List<OutboundMessage> Gonderilenler { get; } = [];

    internal Func<OutboundMessage, SendResult> Kural { get; set; } = _ => SendResult.Success();

    public Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        SendResult sonuc = Kural(message);

        if (sonuc.Succeeded)
        {
            Gonderilenler.Add(message);
        }

        return Task.FromResult(sonuc);
    }

    internal OutboundMessage Alici(string adres) => Gonderilenler.Single(message => message.Recipient == adres);
}

/// <summary>Uyarıları yakalayan log — şablon düşüşü loglanır mı (AC-81)?</summary>
internal sealed class YakalayanLog<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    internal List<(Microsoft.Extensions.Logging.LogLevel Seviye, string Metin)> Kayitlar { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Kayitlar.Add((logLevel, formatter(state, exception)));
    }
}
