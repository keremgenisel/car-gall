using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Media.Infrastructure;

/// <summary>
/// `dotnet ef` için. Çalışma anında KULLANILMAZ: bağlantı dizesi burada bir yer
/// tutucudur ve migration üretmek dışında hiçbir yoldan geçmez.
/// </summary>
internal sealed class MediaDesignTimeFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<MediaDbContext> options = new();
        options.UseNpgsql(
            "Host=localhost;Database=cargallery;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", MediaDbContext.Schema));

        return new MediaDbContext(options.Options);
    }
}
