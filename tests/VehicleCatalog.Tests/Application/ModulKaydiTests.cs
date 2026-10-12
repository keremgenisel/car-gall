using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0012c` **C9**/**D55** — modül **kendi** bağımlılığını kendi kaydeder.
///
/// <para>Bu test `0012b`'nin kaçan hatasının kapısı. Ölçüm şuydu:
/// `ListingTransitionCommands` `InvariantGuardOptions<VehicleCatalogDbContext>`'ı kurucusunda istiyordu ama
/// `VehicleCatalogModule` onu kaydetmiyordu. Uygulama yalnızca `IdentityModule` ve
/// `DealershipModule` aynı tipi singleton olarak kaydettiği için ayakta kalıyordu; o iki
/// satırdan biri kalksaydı katalogun <b>bütün</b> durum geçişleri çalışma zamanında
/// düşerdi.</para>
///
/// <para><b>Neden hiçbir test yakalamadı:</b> entegrasyon testleri komutu elle kuruyor ve
/// `EsZamanlilikKuraliTests` yalnız guard ÇAĞRISINI denetliyor. Kabı kuran yol hiç
/// sınanmıyordu — aynı aile #52'nin bedeli ödenen tuzağıydı.</para>
///
/// <para><b>Aradığı tip artık MODÜLE ÖZEL</b> (**D57**): kayıt D55'le geldiğinde seçenek
/// hâlâ non-generic'ti ve üç modül aynı tipi kaydediyordu — kap tek, son kayıt kazanıyordu.
/// Bu test o gün de yeşildi, çünkü "kendi kaydeder" sorusunu doğru soruyor ama "kaydettiği
/// tip benim mi" sorusunu soramıyordu. Tip parametresi ikisini aynı soruya indiriyor;
/// çakışmayı ölçen kapı `Architecture.Tests/InvariantGuardSecenekleriTests`.</para>
/// </summary>
public sealed class ModulKaydiTests
{
    /// <summary>
    /// Kayıt DEFTERİ okunuyor, kap kurulmuyor: komutu çözmek `Media`, `Dealership` ve
    /// `VehicleReference` kayıtlarını da isterdi ve test o zaman modülün kendi kaydını
    /// değil, Host'un tamamını ölçerdi.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-42: `VehicleCatalog` `InvariantGuardOptions<VehicleCatalogDbContext>`'ı **kendi** kaydeder; başka modülün kaydına bağımlı değildir")]
    public void Kaydeder_KendiKilitSecenegini()
    {
        ServiceCollection services = new();

        services.AddVehicleCatalog(
            "Host=localhost;Database=yok",
            defaultLocale: "tr",
            enabledLocales: ["tr"],
            currencyCode: "TRY",
            currencyMinorUnitExponent: 2,
            adminPageSize: 25,
            adminMaxPageSize: 100);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(InvariantGuardOptions<VehicleCatalogDbContext>));

        // Başka modülün tipi burada OLAMAZ: kayıt kendi `DbContext`'iyle yapılıyor.
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType.Name == "InvariantGuardOptions`1"
                && descriptor.ServiceType.GenericTypeArguments[0] != typeof(VehicleCatalogDbContext));
    }
}
