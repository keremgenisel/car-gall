using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleReference.Application;

/// <summary>
/// `0021` — kurulumun etkin dilleri, yapılandırmadan (kod hiçbir dili varsaymaz). Marka ve
/// model görünen adlarının dil anahtarları bu listeye karşı denetlenir; etkin olmayan dil
/// bir alan hatasıdır, sessizce saklanan bir metin değil (`0020`'nin kuralı).
/// </summary>
internal sealed record ReferenceLocales(IReadOnlyList<Locale> Enabled)
{
    internal bool IsEnabled(string code) => Enabled.Any(locale => locale.Code == code);
}
