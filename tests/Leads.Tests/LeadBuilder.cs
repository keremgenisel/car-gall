using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Tests;

/// <summary>
/// Object mother: nesne kurulumu testin gövdesini boğmaz (docs/testing.md § 4). Test
/// edilen alan her testte AÇIKÇA yazılır; gerisi buradan gelir.
/// </summary>
internal sealed class LeadBuilder
{
    internal static readonly DateTimeOffset VarsayilanAn =
        new(2026, 9, 22, 8, 30, 0, TimeSpan.Zero);

    internal static readonly Guid VarsayilanSubeId = Guid.Parse("0192c000-0000-7000-8000-000000000001");

    private LeadType tip = LeadType.InformationRequest;
    private string? ad = "Ayşe Yılmaz";
    private string? telefon = "+905321112233";
    private string? eposta;
    private string? mesaj;
    private Guid subeId = VarsayilanSubeId;
    private string locale = "tr";
    private bool izin = true;
    private string anahtar = "0192c000-0000-7000-8000-00000000aaaa";
    private VehicleSnapshot? anlikGoruntu;

    internal LeadBuilder Tip(LeadType value)
    {
        tip = value;
        return this;
    }

    internal LeadBuilder Ad(string? value)
    {
        ad = value;
        return this;
    }

    internal LeadBuilder Telefon(string? value)
    {
        telefon = value;
        return this;
    }

    internal LeadBuilder Eposta(string? value)
    {
        eposta = value;
        return this;
    }

    internal LeadBuilder Mesaj(string? value)
    {
        mesaj = value;
        return this;
    }

    internal LeadBuilder Sube(Guid value)
    {
        subeId = value;
        return this;
    }

    internal LeadBuilder Locale(string value)
    {
        locale = value;
        return this;
    }

    internal LeadBuilder Izin(bool value)
    {
        izin = value;
        return this;
    }

    internal LeadBuilder IdempotentlikAnahtari(string value)
    {
        anahtar = value;
        return this;
    }

    internal LeadBuilder AnlikGoruntu(VehicleSnapshot value)
    {
        anlikGoruntu = value;
        return this;
    }

    internal Result<ContactDetails> Iletisim() => ContactDetails.Create(ad, telefon, eposta, mesaj);

    internal Result<Lead> Olustur()
    {
        Result<ContactDetails> iletisim = Iletisim();

        if (iletisim.IsFailure)
        {
            return Result.Failure<Lead>(iletisim.Error);
        }

        return Lead.Capture(
            tip,
            iletisim.Value,
            anlikGoruntu ?? VarsayilanAnlikGoruntu(),
            subeId,
            BuildingBlocks.Locale.Create(locale).Value,
            izin,
            anahtar,
            VarsayilanAn);
    }

    internal static VehicleSnapshot VarsayilanAnlikGoruntu() => VehicleSnapshot.Create(
        Guid.Parse("0192c000-0000-7000-8000-0000000000b1"),
        "Toyota Corolla",
        PriceSnapshot.Of(Money.Create(1_250_000m, "TRY", 2).Value)).Value;

    /// <summary>
    /// Para testlerinde KWD (3 ondalık) en az bir kez kullanılır — `decimal(_,2)` hatası
    /// yalnız orada görünür (docs/testing.md § 4).
    /// </summary>
    internal static VehicleSnapshot KuveytAnlikGoruntusu() => VehicleSnapshot.Create(
        Guid.Parse("0192c000-0000-7000-8000-0000000000b2"),
        "Toyota Corolla",
        PriceSnapshot.Of(Money.Create(1250.500m, "KWD", 3).Value)).Value;

    internal static VehicleSnapshot FiyatSorunuzAnlikGoruntusu() => VehicleSnapshot.Create(
        Guid.Parse("0192c000-0000-7000-8000-0000000000b3"),
        "Toyota Corolla",
        PriceSnapshot.OnRequest()).Value;
}
