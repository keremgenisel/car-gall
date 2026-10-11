using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// **A2**'nin yayın anında doğrulanabilen koşulları — saf, veritabanısız
/// (`docs/testing.md` § 1 — use-case testi).
/// </summary>
public sealed class YayinUygunluguTests
{
    private static readonly IReadOnlySet<string> AlanYok = new HashSet<string>(StringComparer.Ordinal);

    [Fact(DisplayName = "0012b/AC-3: Hazır medya varlığı olmayan araç yayına alınamaz; yanıt `vehicle.cannot_publish.no_media` taşır")]
    public void Alinamaz_Yayina_HazirMedyaYoksa()
    {
        Vehicle arac = new VehicleBuilder().WithoutMedia().Build();

        IReadOnlyList<Error> eksikler = PublishEligibility.Evaluate(arac, Hazir(), AlanYok);

        Assert.Equal([PublishErrors.NoMedia], eksikler.Select(error => error.Code));
    }

    /// <summary>
    /// **M3** — yalnız `Hazır` varlık A2'yi karşılar. `İşleniyor` bir varlık aracı yayına
    /// **yetiştirmez**; `0011` X7 bu bedeli açıkça kabul etmiş ve şartı `0012a` D6'ya
    /// bırakmıştı.
    /// </summary>
    [Fact(DisplayName = "0012b/AC-5: Yayın koşulu yalnız `Hazır` medya varlığını sayar; `işleniyor` ve `başarısız` varlıklar koşulu karşılamaz")]
    public void Saymaz_IsleniyorVeBasarisizVarliklari()
    {
        Vehicle arac = new VehicleBuilder().WithoutMedia().Build();
        Guid isleniyor = Guid.CreateVersion7();
        Guid basarisiz = Guid.CreateVersion7();
        arac.AttachMediaAsset(isleniyor);
        arac.AttachMediaAsset(basarisiz);

        // `Media`'nın hazır kümesi BOŞ: iki varlık da bağlı ama hiçbiri hazır değil.
        IReadOnlyList<Error> eksikler = PublishEligibility.Evaluate(arac, Hazir(), AlanYok);

        Assert.Contains(PublishErrors.NoMedia, eksikler.Select(error => error.Code));

        // Biri hazır olunca koşul KARŞILANIYOR — kesişim boş değil.
        Assert.Empty(PublishEligibility.Evaluate(arac, Hazir(isleniyor), AlanYok));
    }

    [Fact(DisplayName = "0012b/AC-4: Zorunlu işaretli bir opsiyonel alanı boş olan araç yayına alınamaz; yanıt `vehicle.cannot_publish.required_field` ve eksik alanların anahtarlarını taşır")]
    public void Alinamaz_Yayina_ZorunluAlanBossa()
    {
        Vehicle arac = new VehicleBuilder().WithoutMedia().Build();
        Guid varlik = Guid.CreateVersion7();
        arac.AttachMediaAsset(varlik);

        IReadOnlyList<Error> eksikler = PublishEligibility.Evaluate(
            arac,
            Hazir(varlik),
            new HashSet<string>(["damage_record_amount", "warranty_status"], StringComparer.Ordinal));

        Error eksik = Assert.Single(eksikler);

        Assert.Equal(PublishErrors.RequiredField, eksik.Code);

        // D24 — TEK kod, alan anahtarları YAPISAL PARAMETRE. Alan başına kod üretmek,
        // kodu veriye çevirirdi: küme çalışma zamanında değişiyor.
        Assert.Equal("damage_record_amount,warranty_status", eksik.Parameters!["fields"]);
    }

    [Fact(DisplayName = "0012b/AC-2: Lokasyon ataması bekleyen araç yayına alınamaz; yanıt `vehicle.cannot_publish.branch_unassigned` taşır")]
    public void Tasir_Kod_LokasyonAtamasiBekleyenArac()
    {
        // R21/D25 — bu red A2'nin DIŞINDA ve `PublishEligibility`'de değil, komutta
        // ölçülüyor: aracın bayrağına bakıyor. Burada yalnız kodun varlığı ve ailesi
        // sabitleniyor; davranışın kanıtı `GecisKomutlariTests`.
        Assert.StartsWith(PublishErrors.CannotPublish, PublishErrors.BranchUnassigned, StringComparison.Ordinal);
        Assert.Equal("vehicle.cannot_publish.branch_unassigned", PublishErrors.BranchUnassigned);
    }

    [Fact(DisplayName = "0012b/AC-6: Birden çok koşul sağlanmıyorsa yanıt eksiklerin **hepsini birlikte** taşır; ilk eksikte durmaz")]
    public void Tasir_TumEksikler_Birlikte()
    {
        Vehicle arac = new VehicleBuilder().WithoutMedia().Build();

        IReadOnlyList<Error> eksikler = PublishEligibility.Evaluate(
            arac,
            Hazir(),
            new HashSet<string>(["damage_record_amount"], StringComparer.Ordinal));

        Assert.Equal(
            [PublishErrors.NoMedia, PublishErrors.RequiredField],
            eksikler.Select(error => error.Code));

        // Ve toplanmış hâli üst kodu + `reasons` listesini taşıyor.
        Error toplu = PublishErrors.Aggregate(eksikler);

        Assert.Equal(PublishErrors.CannotPublish, toplu.Code);
        Assert.Equal(
            "vehicle.cannot_publish.no_media,vehicle.cannot_publish.required_field",
            toplu.Parameters!["reasons"]);
        Assert.Equal("damage_record_amount", toplu.Parameters["fields"]);
    }

    /// <summary>
    /// **D23**/**D42**'nin TRIPWIRE'ı — A2'nin iki koşulu için kod yazılmadı çünkü ikisi
    /// de **temsil edilemez**. Bu test o temsil edilemezliği sabitliyor: `ListingPrice`
    /// "fiyatsız" bir hâl üretebilir hâle gelirse kırmızıya döner ve kapı o gün yazılır.
    ///
    /// <para>Vakum bir kontrol yazmak yerine tripwire yazmanın nedeni: hiçbir zaman
    /// kırmızıya dönmeyen bir dal, olmayan bir kapıdır.</para>
    /// </summary>
    [Fact(DisplayName = "0012b/AC-10: A2'nin çekirdek alan ve fiyat koşulları yayın anında **temsil edilemez**; temsil edilebilir hâle gelirlerse kapı kırmızıya döner")]
    public void Temsil_Edilemez_FiyatsizVeCekirdeksizHal()
    {
        // `ListingPrice`'ın yalnız iki üreticisi var ve ikisi de dolu bir hâl üretiyor.
        IReadOnlyList<System.Reflection.MethodInfo> ureticiler =
        [
            .. typeof(ListingPrice)
                .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Where(method => method.ReturnType == typeof(ListingPrice))
        ];

        Assert.Equal(2, ureticiler.Count);

        ListingPrice sorunuz = ListingPrice.OnRequest();
        ListingPrice tutarli = ListingPrice.Of(Money.Create(1m, "TRY", 2).Value);

        Assert.True(sorunuz.IsOnRequest);
        Assert.NotNull(tutarli.Amount);

        // Çekirdek alanlar: `Vehicle.Create` dokuz zorunlu parametre alıyor ve hiçbiri
        // nullable değil — "çekirdek alanı boş araç" kurulamıyor.
        System.Reflection.ParameterInfo[] parametreler = typeof(Vehicle)
            .GetMethod("Create", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .GetParameters();

        Assert.DoesNotContain(parametreler, parametre => parametre.IsOptional);
    }

    private static IReadOnlySet<Guid> Hazir(params Guid[] kimlikler) => new HashSet<Guid>(kimlikler);
}
