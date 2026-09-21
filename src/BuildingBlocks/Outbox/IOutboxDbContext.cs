using Microsoft.EntityFrameworkCore;

namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>Yayıncı modülün `DbContext`'i bunu uygular; outbox tablosu onun şemasındadır.</summary>
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}

/// <summary>Alıcı modülün `DbContext`'i bunu uygular; inbox tablosu onun şemasındadır.</summary>
public interface IInboxDbContext
{
    DbSet<InboxEntry> InboxEntries { get; }
}
