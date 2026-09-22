using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// `VehicleCatalog` ve `VehicleReference` bu test projesinin sınırının ötesindedir
/// (docs/testing.md § 1); ikisi de sahtelenir. Sahteler DEĞİŞTİRİLEBİLİR: L2'nin sorduğu
/// soru "kaynak değişince talep ne yapıyor" olduğu için, kaynağın değişebilir olması
/// testin kendisidir.
/// </summary>
internal sealed class FakeListingSnapshotSource : IListingSnapshotSource
{
    internal const string GecerliSlug = "toyota-corolla-2019-abc123";

    internal static readonly Guid ListingId = Guid.Parse("0192c000-0000-7000-8000-0000000000b1");
    internal static readonly Guid ModelId = Guid.Parse("0192c000-0000-7000-8000-0000000000c1");
    internal static readonly Guid SubeId = Guid.Parse("0192c000-0000-7000-8000-000000000001");

    private ListingSnapshot? current = new(
        ListingId,
        IsReserved: false,
        ModelId,
        PriceSnapshot.Of(Money.Create(1_250_000m, "TRY", 2).Value),
        SubeId);

    public Task<ListingSnapshot?> FindAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult(slug == GecerliSlug ? current : null);

    internal void FiyatiDegistir(decimal tutar) =>
        current = current! with { Price = PriceSnapshot.Of(Money.Create(tutar, "TRY", 2).Value) };

    internal void FiyatSorunuzYap() => current = current! with { Price = PriceSnapshot.OnRequest() };

    internal void KwdYap(decimal tutar) =>
        current = current! with { Price = PriceSnapshot.Of(Money.Create(tutar, "KWD", 3).Value) };

    internal void RezerveYap() => current = current! with { IsReserved = true };

    /// <summary>Araç arşivlendi veya satıldı: vitrin okuma modelinde satırı artık yok.</summary>
    internal void VitrindenDusur() => current = null;
}

internal sealed class FakeCanonicalModelNameSource : ICanonicalModelNameSource
{
    private string? name = "Toyota Corolla";

    public Task<string?> FindAsync(Guid vehicleModelId, CancellationToken cancellationToken) =>
        Task.FromResult(vehicleModelId == FakeListingSnapshotSource.ModelId ? name : null);

    internal void Kaybet() => name = null;
}

internal sealed class TalepOlusturmaHarness(LeadsDatabase database)
{
    internal FakeListingSnapshotSource Ilanlar { get; } = new();

    internal FakeCanonicalModelNameSource ModelAdlari { get; } = new();

    internal FixedClock Saat { get; } = new(LeadBuilder.VarsayilanAn);

    internal static CaptureLeadCommand Komut(
        string? slug = FakeListingSnapshotSource.GecerliSlug,
        string? tip = "information_request",
        string? locale = "tr",
        string? ad = "Ayşe Yılmaz",
        string? telefon = "+905321112233",
        string? eposta = null,
        string? mesaj = null,
        bool izin = true,
        string? anahtar = "0192c000-0000-7000-8000-00000000aaaa") =>
        new(slug, tip, locale, ad, telefon, eposta, mesaj, izin, anahtar);

    internal async Task<Result<Guid>> CalistirAsync(CaptureLeadCommand komut)
    {
        await using LeadsDbContext context = database.CreateContext();

        CaptureLead handler = new(
            context,
            database.CreateUnitOfWork(context, Saat),
            Ilanlar,
            ModelAdlari,
            new LeadLocaleOptions(
            [
                BuildingBlocks.Locale.Create("tr").Value,
                BuildingBlocks.Locale.Create("ar").Value,
                BuildingBlocks.Locale.Create("en").Value,
            ]),
            Saat);

        return await handler.HandleAsync(komut, CancellationToken.None);
    }
}
