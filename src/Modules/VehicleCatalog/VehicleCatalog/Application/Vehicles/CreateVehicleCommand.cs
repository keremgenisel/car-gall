using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>Panelin gönderdiği araç kaydı. Para BİRİMİ yoktur — o yapılandırmadan gelir (C3).</summary>
internal sealed record CreateVehicleRequest(
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    IReadOnlyDictionary<string, string> Color,
    int ModelYear,
    int MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string Vin);

/// <summary>
/// Araç kaydı oluşturma. **C17 gereği agregatın üzerine kurulu bir komut katmanıdır**:
/// `Vehicle.Create` ve `Listing.CreateDraft` olduğu gibi çağrılıyor, kararları değişmiyor.
///
/// <para><b>Doğrulama sırası bilinçli:</b> önce BİÇİM (ucuz, ağ yok), sonra VARLIK
/// (referans ve şube sorguları), en son yazma. Tersi olsaydı biçimsiz bir istek için altı
/// referans sorgusu atılırdı.</para>
///
/// <para><b>Altı referans alanı AYRI AYRI doğrulanır</b> (AC-5) ve hata hangi alanın
/// düştüğünü söyler: "bir şey yanlış" diyen bir kod personele hangisini düzelteceğini
/// söylemez (AC-42).</para>
/// </summary>
internal sealed class CreateVehicleCommand(
    VehicleCatalogDbContext context,
    VehicleWriteRules rules,
    IClock clock)
{
    /// <summary>R17 — yeni kayıt VARSAYILAN vitrin sıralama ağırlığıyla açılır (X17).</summary>
    internal const int DefaultShowcaseSortWeight = 0;

    /// <summary>PostgreSQL: tekillik ihlali.</summary>
    private const string UniqueViolation = "23505";

    internal async Task<Result<Guid>> ExecuteAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<Vin> vin = Vin.Create(request.Vin);

        if (vin.IsFailure)
        {
            // Üç başarısızlık nedeni (boş · uzun · iç boşluk) tek koda eşlenmiyor: boş
            // bırakılmış bir alana "uzunluk aşıldı" demek personeli yanlış düzeltmeye
            // yöneltiyordu — şaseyi kısaltmaya çalışıyor, oysa sorun boşluk.
            return Failure(
                vin.Error.Code == Vin.TooLong ? VehicleErrors.TextTooLong : VehicleErrors.InvalidVin,
                VehicleFieldNames.Vin);
        }

        Result color = rules.ValidateColor(request.Color);

        if (color.IsFailure)
        {
            return Result.Failure<Guid>(color.Error);
        }

        Result<VehicleSpecification> specification = VehicleWriteRules.BuildSpecification(
            request.BodyType,
            request.FuelType,
            request.TransmissionType,
            request.DrivetrainType,
            request.TrimLevel);

        if (specification.IsFailure)
        {
            return Result.Failure<Guid>(specification.Error);
        }

        Result<ListingPrice> price = rules.BuildPrice(request.PriceAmount, request.PriceOnRequest);

        if (price.IsFailure)
        {
            return Result.Failure<Guid>(price.Error);
        }

        Result references = await rules.ValidateReferencesAsync(
            request.VehicleModelId,
            request.BodyType,
            request.FuelType,
            request.TransmissionType,
            request.DrivetrainType,
            request.TrimLevel,
            cancellationToken);

        if (references.IsFailure)
        {
            return Result.Failure<Guid>(references.Error);
        }

        Result branch = await rules.ValidateBranchAsync(request.BranchId, cancellationToken);

        if (branch.IsFailure)
        {
            return Result.Failure<Guid>(branch.Error);
        }

        Result<Vehicle> vehicle = Vehicle.Create(
            request.BranchId,
            request.VehicleModelId,
            specification.Value,
            VehicleWriteRules.BuildColor(request.Color),
            request.ModelYear,
            request.MileageKilometers,
            price.Value,
            vin.Value,
            clock.UtcNow);

        if (vehicle.IsFailure)
        {
            // Agregatın hatası alan adı TAŞIMAZ — `Domain` panelin alan adlarını bilmez.
            // Eşlemeyi burada yapmak AC-42'nin "hatalı alan işaretlenir" sözünü kilometre
            // ve model yılı için de tutturuyor; aksi hâlde form genel hataya düşerdi ve
            // personel hangisini düzelteceğini bilemezdi.
            return Failure(vehicle.Error.Code, vehicle.Error.Code switch
            {
                Vehicle.NegativeMileage => VehicleFieldNames.Mileage,
                _ => VehicleFieldNames.ModelYear,
            });
        }

        context.Vehicles.Add(vehicle.Value);
        context.Listings.Add(Listing.CreateDraft(vehicle.Value.Id, DefaultShowcaseSortWeight));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsVinConflict(exception))
        {
            // AC-16 — tekilliği VERİTABANI söylüyor. Önce "var mı" diye bakıp sonra yazmak
            // iki eşzamanlı isteğin ikisinin de geçmesine izin verirdi: okuma ile yazma
            // arasında kilit yok ve kilitlenecek bir satır da henüz yok.
            return Failure(VehicleErrors.DuplicateVin, VehicleFieldNames.Vin);
        }

        return Result.Success(vehicle.Value.Id.Value);
    }

    internal static bool IsVinConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == UniqueViolation
        && postgres.ConstraintName == "ux_vehicles_vin";

    private static Result<Guid> Failure(string code, string field) =>
        Result.Failure<Guid>(Error.With(code, "field", field));
}
