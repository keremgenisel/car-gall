using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Çalıştırılan SQL'i kaydeder. "Bu sorgu şu kolonları okuyor" iddiası ancak ÜRETİM
/// yolunun ürettiği metinle kanıtlanır; testin kendi kurduğu bir sorguyu ölçmek yalnız
/// EF'in çalıştığını gösterir.
/// </summary>
internal sealed class SorguYakalayici : DbCommandInterceptor
{
    private readonly List<string> sorgular = [];

    internal IReadOnlyList<string> Sorgular => sorgular;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        sorgular.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
