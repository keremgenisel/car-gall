using System.Reflection;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// AC-24'ün gözlem noktası C19'dadır: silme ucu YOKTUR ve yazılmayacaktır, dolayısıyla
/// kriter endpoint testiyle sınanamaz. İnvariant kodun ŞEKLİNDE yaşıyor — bu testler de
/// o şekli ölçüyor.
///
/// <para>Bulgu olarak geldi: invariant doğru kurulmuştu ama hiçbir test onu ölçmüyordu,
/// yani yarın birinin `RemovePriceChange` eklemesi hiçbir şeyi kırmazdı. Yorum satırı
/// kapı değildir.</para>
/// </summary>
public sealed class FiyatGecmisiInvariantTests
{
    private static readonly string[] SilmeFiilleri = ["Remove", "Delete", "Clear", "Erase", "Drop"];

    [Fact(DisplayName = "0010a/AC-24: Fiyat geçmişindeki bir kayıt silinemez ve düzeltilemez; gözlem noktası C19'dadır")]
    public void Silinemez_FiyatGecmisiKaydi()
    {
        // 1. `Vehicle` geçmişten kayıt çıkaran bir metot sunmuyor.
        MethodInfo[] silenler =
        [
            .. typeof(Vehicle)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => SilmeFiilleri.Any(fiil =>
                    method.Name.StartsWith(fiil, StringComparison.Ordinal)))
        ];

        Assert.True(
            silenler.Length == 0,
            $"`Vehicle` üzerinde kaldırma metodu var: {string.Join(", ", silenler.Select(m => m.Name))}");

        // 2. Geçmiş dışarıya SALT OKUNUR veriliyor; `List<PriceChange>` dönseydi çağıran
        //    kendisi silebilirdi ve agregatın invariantı kâğıt üstünde kalırdı.
        PropertyInfo gecmis = typeof(Vehicle).GetProperty(
            nameof(Vehicle.PriceChanges),
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)!;

        Assert.Equal(typeof(IReadOnlyList<PriceChange>), gecmis.PropertyType);
        Assert.Null(gecmis.SetMethod);
    }

    [Fact(DisplayName = "0010a/AC-24: Fiyat geçmişindeki bir kayıt silinemez ve düzeltilemez; gözlem noktası C19'dadır")]
    public void Duzeltilemez_FiyatGecmisiKaydi()
    {
        // `init` ERİŞİMCİSİ setter sayılmaz: nesne kurulduktan sonra yazılamaz. Aranan
        // şey, kayıt oluştuktan SONRA değer değiştirebilen bir erişimci.
        PropertyInfo[] yazilabilirler =
        [
            .. typeof(PriceChange)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(property => property.SetMethod is { } setter && !IsInitOnly(setter))
        ];

        Assert.True(
            yazilabilirler.Length == 0,
            $"`PriceChange` üzerinde kurulumdan sonra yazılabilen alan var: {string.Join(", ", yazilabilirler.Select(p => p.Name))}");
    }

    /// <summary>
    /// `init` erişimcisi, dönüş tipi `IsExternalInit` modifier'ı taşıyan bir `set`'tir;
    /// reflection'da ayrı bir bayrağı yok.
    /// </summary>
    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
}
