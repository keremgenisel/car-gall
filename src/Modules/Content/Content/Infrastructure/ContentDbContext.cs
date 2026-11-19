using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Infrastructure;

/// <summary>`0017` — `Content`'in tek veri sahibi bağlamı; yalnız <c>content</c> şeması (C1, C9).</summary>
internal sealed class ContentDbContext(DbContextOptions<ContentDbContext> options) : DbContext(options)
{
    internal const string Schema = "content";

    /// <summary>İçerik sayfaları.</summary>
    internal DbSet<ContentPage> Pages => Set<ContentPage>();

    /// <summary>SSS girdileri.</summary>
    internal DbSet<FaqEntry> FaqEntries => Set<FaqEntry>();

    /// <summary>`0022` — ana sayfa metinleri (tek kayıt).</summary>
    internal DbSet<HomeTexts> HomeTexts => Set<HomeTexts>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
    }
}
