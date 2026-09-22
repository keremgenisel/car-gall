using CarGallery.BuildingBlocks.Schema;

namespace CarGallery.Host.Schema;

/// <summary>
/// Şema iki ayrı yerde ele alınır ve bu ayrım kararın kendisidir:
/// **uygulayan kurulum adımıdır, çalışan uygulama yalnız doğrular.**
///
/// Gerekçe `docs/decisions/0002-migration-uygulama-stratejisi.md`'de. Kısaca:
/// docs/architecture.md § 5 şema başına ayrı PostgreSQL rolü istiyor ve sınırın
/// veritabanı seviyesinde dayatılmasını bekliyor. Uygulama migration uygularsa, uygulama
/// rolünün her şemada kalıcı DDL yetkisi olması gerekir ve o sınır süse dönüşür.
/// </summary>
public static class ModuleSchemaOperations
{
    /// <summary>Kurulum adımının argümanı. Uygulanır ve süreç biter; sunucu açılmaz.</summary>
    public const string MigrateArgument = "--migrate";

    /// <summary>
    /// Her modülün bekleyen migration'larını uygular. Modüller DI'dan gelir; elle
    /// tutulan bir liste yoktur, dolayısıyla unutulabilecek bir adım da yoktur.
    /// </summary>
    public static async Task<int> ApplyModuleSchemasAsync(
        this IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();

        IModuleSchema[] schemas = [.. scope.ServiceProvider.GetServices<IModuleSchema>()];

        if (schemas.Length == 0)
        {
            // Hiç şema bulamayan bir kurulum adımı da "başarılı" derdi; sessiz başarı
            // en pahalı yanlıştır.
            throw new InvalidOperationException(
                "Kayıtlı modül şeması bulunamadı; kurulum adımı hiçbir şey uygulamadan bitemez.");
        }

        foreach (IModuleSchema schema in schemas)
        {
            logger.LogInformation("Şema uygulanıyor: {Module}", schema.Name);
            await schema.ApplyAsync(cancellationToken);
        }

        logger.LogInformation("{Count} modül şeması güncel.", schemas.Length);

        return 0;
    }

    /// <summary>
    /// Çalışan uygulama migration UYGULAMAZ; yalnız uygulanmış mı diye bakar ve
    /// eksikse AÇIKÇA reddeder.
    ///
    /// Sessizce devam etmek, hatayı ilk sorguya — yani ilk ziyaretçiye — ertelerdi ve
    /// o hata "tablo yok" diye okunur, "kurulum adımı atlandı" diye değil.
    /// </summary>
    public static async Task EnsureModuleSchemasAreCurrentAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();

        List<string> pending = [];

        foreach (IModuleSchema schema in scope.ServiceProvider.GetServices<IModuleSchema>())
        {
            if (await schema.HasPendingAsync(cancellationToken))
            {
                pending.Add(schema.Name);
            }
        }

        if (pending.Count > 0)
        {
            throw new InvalidOperationException(
                $"Şu modüllerin şeması güncel değil: {string.Join(", ", pending)}. "
                + $"Uygulamayı başlatmadan önce kurulum adımını çalıştırın: dotnet run --project src/Host -- {MigrateArgument}");
        }
    }
}
