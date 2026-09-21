using System.Net;
using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class FieldSettingsEndpointTests(ShowcaseDatabase database)
{
    [Fact(DisplayName = "AC-11: Daraltma çubuğu yalnızca kasa tipi eksenini sunar; alan ayarları yanıtındaki başka hiçbir alan eksen olarak sunulmaz")]
    public async Task YalnizKasaTipiFiltrelenebilir_AlanAyarlariYanitinda()
    {
        await SetBodyTypeVisibilityAsync(FieldPublicVisibility.Visible);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument document = JsonDocument.Parse(
            await api.Client.GetStringAsync("/api/catalog/field-settings"));

        List<string> filterable =
        [
            .. document.RootElement.GetProperty("fields").EnumerateArray()
                .Where(field => field.GetProperty("isFilterable").GetBoolean())
                .Select(field => field.GetProperty("fieldKey").GetString()!)
        ];

        Assert.Equal(["body_type"], filterable);
    }

    [Fact(DisplayName = "AC-12: Kasa tipi alanı alan ayarlarında public değilken daraltma çubuğu hiç gösterilmez; liste daraltmasız çalışmaya devam eder")]
    public async Task FiltrelenemezBildirilir_KasaTipiPublicDegilken()
    {
        await SetBodyTypeVisibilityAsync(FieldPublicVisibility.Hidden);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument document = JsonDocument.Parse(
            await api.Client.GetStringAsync("/api/catalog/field-settings"));

        JsonElement bodyType = document.RootElement.GetProperty("fields").EnumerateArray()
            .Single(field => field.GetProperty("fieldKey").GetString() == "body_type");

        Assert.False(bodyType.GetProperty("isPublic").GetBoolean());
        Assert.False(bodyType.GetProperty("isFilterable").GetBoolean());

        // Kapı yalnız çubuğu gizler; liste daraltmasız çalışmaya devam eder.
        HttpResponseMessage liste = await api.Client.GetAsync("/api/catalog/showcase-listings");
        Assert.Equal(HttpStatusCode.OK, liste.StatusCode);

        await SetBodyTypeVisibilityAsync(FieldPublicVisibility.Visible);
    }

    private async Task SetBodyTypeVisibilityAsync(FieldPublicVisibility visibility)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        VehicleFieldSetting setting = await context.VehicleFieldSettings
            .SingleAsync(field => field.FieldKey == VehicleFieldKeys.BodyType);

        setting.PublicVisibility = visibility;
        await context.SaveChangesAsync();
    }
}
