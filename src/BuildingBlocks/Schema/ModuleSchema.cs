using Microsoft.EntityFrameworkCore;

namespace CarGallery.BuildingBlocks.Schema;

/// <summary>
/// Her modülün kendi `DbContext`'i için tek satırlık kayıt. Modülün iç tipleri
/// `internal` kalır; Host onları hiç görmez, yalnız bu arayüzü görür.
/// </summary>
public sealed class ModuleSchema<TDbContext>(TDbContext context, string name) : IModuleSchema
    where TDbContext : DbContext
{
    public string Name { get; } = name;

    public Task ApplyAsync(CancellationToken cancellationToken) =>
        context.Database.MigrateAsync(cancellationToken);

    public async Task<bool> HasPendingAsync(CancellationToken cancellationToken) =>
        (await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
}
