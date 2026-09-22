using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests;

/// <summary>
/// Object mother: nesne kurulumu testin gövdesini boğmaz (docs/testing.md § 4). Test
/// edilen alan her testte AÇIKÇA yazılır; gerisi buradan gelir.
/// </summary>
internal sealed class StaffBuilder
{
    internal static readonly DateTimeOffset VarsayilanAn = new(2026, 9, 22, 8, 30, 0, TimeSpan.Zero);

    /// <summary>Testlerin parola özetlemesi hızlı olsun diye düşük; üretim değeri yapılandırmadan gelir.</summary>
    internal const int TestIterasyon = 1_000;

    private string? ad = "Ayşe Yılmaz";
    private string kimlik = "ayse@ornek.com";
    private string parola = "cok-guclu-parola";
    private StaffRole rol = StaffRole.Manager;
    private string locale = "tr";
    private Guid? subeId;

    internal StaffBuilder Ad(string? value)
    {
        ad = value;
        return this;
    }

    internal StaffBuilder Kimlik(string value)
    {
        kimlik = value;
        return this;
    }

    internal StaffBuilder Parola(string value)
    {
        parola = value;
        return this;
    }

    internal StaffBuilder Rol(StaffRole value)
    {
        rol = value;
        return this;
    }

    internal StaffBuilder Locale(string value)
    {
        locale = value;
        return this;
    }

    internal StaffBuilder Sube(Guid? value)
    {
        subeId = value;
        return this;
    }

    internal Result<Staff> Olustur() => Staff.Add(
        ad,
        LoginId.Create(kimlik).Value,
        PasswordHash.Create(parola, TestIterasyon),
        rol,
        BuildingBlocks.Locale.Create(locale).Value,
        subeId,
        VarsayilanAn);

    internal Staff Kur() => Olustur().Value;
}
