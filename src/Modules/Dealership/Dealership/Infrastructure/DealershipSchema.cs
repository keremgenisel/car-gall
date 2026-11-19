using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Schema;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Infrastructure;

/// <summary>
/// `0020` **C4** — kurulum adımı migration'ları uygular ve ARDINDAN görünen adın varsayılan
/// dildeki karşılığını, yoksa, ticari unvandan doldurur. Hiçbir kurulum görünen adsız kalmaz
/// (AC-7).
///
/// <para><b>Neden migration'ın içinde değil:</b> varsayılan dil kurulum yapılandırmasıdır
/// (Bilinen tuzak 4) ve migration onu bilemez; SQL'e <c>'tr'</c> yazmak iki dilli ya da
/// Arapça varsayılanlı bir kurulumda görünen adı yanlış dile koyardı. Kurulum adımı
/// yapılandırmayı bilir ve şema zaten burada uygulanıyor (ADR `0002`).</para>
///
/// <para>Adım <b>idempotenttir</b>: yalnız varsayılan dilde adı OLMAYAN satıra dokunur.
/// Yöneticinin panelden yazdığı ad bir sonraki kurulum adımında ezilmez.</para>
/// </summary>
internal sealed class DealershipSchema(DealershipDbContext context, Locale defaultLocale) : IModuleSchema
{
    private readonly ModuleSchema<DealershipDbContext> migrations = new(context, "Dealership");

    public string Name => migrations.Name;

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await migrations.ApplyAsync(cancellationToken);
        await BackfillDisplayNameAsync(context, defaultLocale, cancellationToken);
    }

    public Task<bool> HasPendingAsync(CancellationToken cancellationToken) =>
        migrations.HasPendingAsync(cancellationToken);

    /// <summary>
    /// Doldurma. Değer parametredir; dil kodu da ticari unvan da SQL metnine gömülmez.
    /// Yalnız `dealership` şemasına dokunur (C4).
    /// </summary>
    internal static Task<int> BackfillDisplayNameAsync(
        DealershipDbContext context,
        Locale defaultLocale,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            """
            UPDATE dealership.galleries
            SET display_names = display_names || jsonb_build_object({0}::text, commercial_name)
            WHERE NOT (display_names ? {0})
            """,
            [defaultLocale.Code],
            cancellationToken);
}
