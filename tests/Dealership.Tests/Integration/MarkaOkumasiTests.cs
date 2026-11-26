using System.Data.Common;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// `0026` **D17** — Host'un marka okuması: görünen ad ve logo kimliği tek sorguda; bültenin
/// tükettiği `IGalleryDisplayNameQueries` değişmez. Her test kendi işleminde, commit edilmeden.
/// </summary>
[Collection(DealershipDatabaseCollection.Name)]
public sealed class MarkaOkumasiTests(DealershipDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;

    [Fact(DisplayName = "0026/AC-76: Görünen adı ve logo kimliğini döndüren ayrı arayüz tek bir veritabanı sorgusuyla çalışır.")]
    public async Task TekSorgu_AdVeLogo()
    {
        KomutSayaci sayac = new();
        await using DealershipDbContext context = database.CreateContext(sayac);
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        Guid logo = Guid.CreateVersion7();
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dealership.galleries SET logo_media_asset_id = {logo}, display_names = {"""{"tr":"Marka Oto","ar":"ماركة"}"""}::jsonb");
        sayac.Komutlar.Clear();

        GalleryBrandingDto? marka = await new PublicGalleryQueries(context, Tr).FindBrandingAsync("ar", CancellationToken.None);

        Assert.Equal(new GalleryBrandingDto("ماركة", logo), marka);
        string komut = Assert.Single(sayac.Komutlar);
        Assert.Contains("logo_media_asset_id", komut, StringComparison.Ordinal);
        Assert.DoesNotContain("commercial_name", komut, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0026/AC-76: Logo atanmamış galeride marka okuması adı ve `null` logo kimliğini döner.")]
    public async Task LogoYok_Null()
    {
        await using DealershipDbContext context = database.CreateContext();
        await using IDbContextTransaction islem = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("UPDATE dealership.galleries SET logo_media_asset_id = NULL");

        GalleryBrandingDto? marka = await new PublicGalleryQueries(context, Tr).FindBrandingAsync("tr", CancellationToken.None);

        Assert.NotNull(marka);
        Assert.Null(marka.LogoMediaAssetId);
        Assert.False(string.IsNullOrWhiteSpace(marka.DisplayName));
    }

    [Fact(DisplayName = "0026/AC-75: `IGalleryDisplayNameQueries.FindDisplayNameAsync`'in dönüş tipi bu karttan önceki hâlindedir (`Task<string?>`).")]
    public void DonusTipi_Degismez()
    {
        System.Reflection.MethodInfo metot = typeof(IGalleryDisplayNameQueries).GetMethod(nameof(IGalleryDisplayNameQueries.FindDisplayNameAsync))!;

        Assert.Equal(typeof(Task<string>), metot.ReturnType);
        Assert.Single(typeof(IGalleryDisplayNameQueries).GetMethods());
    }

    private sealed class KomutSayaci : DbCommandInterceptor
    {
        internal List<string> Komutlar { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Komutlar.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
