using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// 0009 D2 — galeri bildiriminin hedefi. Seed şubelerinden biri e-posta taşıyor, diğeri
/// taşımıyor; ikisi de kurulum verisi olarak migration'dan geliyor ve ikisi de gerçek bir
/// kenar durumu temsil ediyor.
/// </summary>
[Collection(DealershipDatabaseCollection.Name)]
public sealed class SubeIletisimSorgusuTests(DealershipDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;

    [Fact(DisplayName = "0009/AC-4: Şubenin e-posta adresi varsa bildirim o adrese gider")]
    public async Task Doner_SubeEpostasi_Varsa()
    {
        BranchNotificationContactDto? iletisim = await SorAsync("kadikoy-subesi");

        Assert.NotNull(iletisim);
        Assert.Equal("kadikoy@otogaleri.com.tr", iletisim.Email);
        Assert.Equal("Kadıköy Şubesi", iletisim.Name);
    }

    [Fact(DisplayName = "0009/AC-5: Şubenin e-posta adresi yoksa bildirim kurulum bildirim adresine gider")]
    public async Task Doner_NullEposta_Yoksa()
    {
        BranchNotificationContactDto? iletisim = await SorAsync("atasehir-subesi");

        Assert.NotNull(iletisim);
        Assert.Null(iletisim.Email);
        Assert.Equal("Ataşehir Şubesi", iletisim.Name);
    }

    [Fact(DisplayName = "0009/AC-5: Şubenin e-posta adresi yoksa bildirim kurulum bildirim adresine gider")]
    public async Task Doner_Null_VarOlmayanSube()
    {
        await using DealershipDbContext context = database.CreateContext();

        Assert.Null(await new BranchQueries(context, Tr)
            .FindContactAsync(Guid.NewGuid(), "tr", CancellationToken.None));
    }

    private async Task<BranchNotificationContactDto?> SorAsync(string slug)
    {
        await using DealershipDbContext context = database.CreateContext();

        Guid subeId = await context.Branches
            .AsNoTracking()
            .Where(branch => branch.Slug == BranchSlug.Create(slug).Value)
            .Select(branch => branch.Id.Value)
            .SingleAsync();

        return await new BranchQueries(context, Tr).FindContactAsync(subeId, "tr", CancellationToken.None);
    }
}
