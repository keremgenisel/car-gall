using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class VehicleTests
{
    [Fact]
    public void KapakGorseli_SiraliListeninIlkElemanidir()
    {
        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();
        Guid ilk = Guid.CreateVersion7();
        Guid ikinci = Guid.CreateVersion7();

        vehicle.AttachMediaAsset(ilk);
        vehicle.AttachMediaAsset(ikinci);

        Assert.Equal(ilk, vehicle.CoverAssetId);
    }

    /// <summary>
    /// <b>`0012a` D6/D11 ile güncellendi — ve bu, kapanmış bir spec'in testine bilerek
    /// dokunulan yerdir.</b>
    ///
    /// <para>Önceki hâli parametresiz <c>EnsurePublishable()</c> çağırıyordu ve metot
    /// yalnız SAYIYA bakıyordu. `0011` X7 o bedeli açıkça kabul etmişti: *"yüklenmiş ama
    /// işlenmemiş bir görseli olan araç yayına alınabilir görünür"*. `0012a` D6 şartı
    /// **A2** ve **M3**'e göre genişletti; hazırlık bilgisi `Media`'dan gelip parametre
    /// olarak giriyor (D11), çünkü `Domain` dış dünyayı bilmez.</para>
    ///
    /// <para>Ölçüldü: metodun bu testten başka çağıranı yoktu — yani imza değişikliği
    /// yalnız burayı etkiledi ve D6 bunu zaten kabul etmişti.</para>
    /// </summary>
    [Fact(DisplayName = "0012a/AC-19: Hiç bağlı varlığı olmayan araç medya koşulunu karşılamaz")]
    public void Karsilamaz_MedyaKosulu_HicBagliVarlikYoksa()
    {
        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();

        Result result = vehicle.EnsurePublishable(new HashSet<Guid>());

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.MediaRequired, result.Error.Code);
    }

    [Fact(DisplayName = "0012a/AC-20: Bağlı varlıklarının hepsi `işleniyor` olan araç medya koşulunu karşılamaz")]
    public void Karsilamaz_MedyaKosulu_HepsiIsleniyorsa()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        // Hazır küme BOŞ: `Media`'ya göre üçü de henüz işleniyor.
        Result result = vehicle.EnsurePublishable(new HashSet<Guid>());

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.MediaRequired, result.Error.Code);
        Assert.Equal(3, varliklar.Length);
    }

    /// <summary>
    /// CLARIFY turunun <b>Tuzak 1</b>'i tam burada: `AreAllReadyAsync` ile ölçen bir
    /// uygulama bu aracı **reddederdi**, oysa **A2** "en az bir hazır" istiyor.
    /// </summary>
    [Fact(DisplayName = "0012a/AC-21: Biri `hazır`, ikisi `işleniyor` olan araç medya koşulunu **karşılar**")]
    public void Karsilar_MedyaKosulu_BiriHazirsa()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Result result = vehicle.EnsurePublishable(new HashSet<Guid> { varliklar[1] });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "0012a/AC-22: Bağlı varlıklarının hepsi `başarısız` olan araç medya koşulunu karşılamaz")]
    public void Karsilamaz_MedyaKosulu_HepsiBasarisizsa()
    {
        (Vehicle vehicle, _) = UcVarlikliArac();

        // Başarısız varlık `Media`'nın hazır kümesinde HİÇ bulunmaz; kesişim boş.
        Result result = vehicle.EnsurePublishable(new HashSet<Guid>());

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.MediaRequired, result.Error.Code);
    }

    /// <summary>
    /// Karar yalnız BAĞLI varlıklara göre veriliyor: çağıran fazlasını verse bile başka
    /// bir aracın hazır fotoğrafı bu aracı yayına uygun yapmaz.
    /// </summary>
    [Fact]
    public void Karsilamaz_MedyaKosulu_HazirVarlikBaskaAracinsa()
    {
        (Vehicle vehicle, _) = UcVarlikliArac();

        Result result = vehicle.EnsurePublishable(new HashSet<Guid> { Guid.CreateVersion7() });

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.MediaRequired, result.Error.Code);
    }

    [Fact(DisplayName = "0012a/AC-3: Bağlı fotoğraflar personelin verdiği sırada durur ve sıra kalıcıdır")]
    public void Durur_BagliFotograflar_VerilenSirada()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Assert.Equal(varliklar, vehicle.MediaAssetIds);

        Guid[] yeniSira = [varliklar[2], varliklar[0], varliklar[1]];

        Assert.True(vehicle.ReplaceMediaAssets(yeniSira).IsSuccess);
        Assert.Equal(yeniSira, vehicle.MediaAssetIds);
    }

    [Fact(DisplayName = "0012a/AC-5: Sıra değiştiğinde kapak da değişir")]
    public void Degisir_Kapak_SiraDegistiginde()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Assert.Equal(varliklar[0], vehicle.CoverAssetId);

        vehicle.ReplaceMediaAssets([varliklar[2], varliklar[1], varliklar[0]]);

        Assert.Equal(varliklar[2], vehicle.CoverAssetId);
    }

    [Fact(DisplayName = "0012a/AC-6: Personel bağlı bir fotoğrafı çıkarabilir; aracın listesi onu artık içermez")]
    public void Icermez_Liste_CikarilanFotografi()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Assert.True(vehicle.ReplaceMediaAssets([varliklar[0], varliklar[2]]).IsSuccess);

        Assert.Equal(2, vehicle.MediaAssetIds.Count);
        Assert.DoesNotContain(varliklar[1], vehicle.MediaAssetIds);
    }

    [Fact(DisplayName = "0012a/AC-8: Aynı varlık bir araca iki kez bağlanamaz")]
    public void Baglanamaz_AyniVarlik_IkiKez()
    {
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Result result = vehicle.ReplaceMediaAssets([varliklar[0], varliklar[0]]);

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.DuplicateMediaAsset, result.Error.Code);

        // Reddedilen istek listeyi BOZMAMALI: domain ya tamamını alır ya hiçbirini.
        Assert.Equal(varliklar, vehicle.MediaAssetIds);
    }

    /// <summary>
    /// AC-4'ün tip üzerinden kapısı: kapak ayrı bir alan DEĞİL, türetilmiş bir özellik
    /// (D2). İleride biri `CoverAssetId`'yi yazılabilir yaparsa burası kırmızı verir ve
    /// aynı olgu iki yerde tutulmaya başlamaz.
    /// </summary>
    [Fact(DisplayName = "0012a/AC-4: Sıralı listenin ilk elemanı kapak görselidir; ayrı bir kapak alanı yoktur")]
    public void Yoktur_AyriKapakAlani()
    {
        System.Reflection.PropertyInfo kapak = typeof(Vehicle).GetProperty(
            nameof(Vehicle.CoverAssetId),
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic)!;

        Assert.NotNull(kapak);
        Assert.Null(kapak.SetMethod);

        // Ve gerçekten sıralı listenin ilk elemanı.
        (Vehicle vehicle, Guid[] varliklar) = UcVarlikliArac();

        Assert.Equal(varliklar[0], vehicle.CoverAssetId);
    }

    /// <summary>Üç varlığı sırayla bağlanmış bir araç; sıra iddiası için kimlikler de dönüyor.</summary>
    private static (Vehicle Vehicle, Guid[] Varliklar) UcVarlikliArac()
    {
        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();
        Guid[] varliklar = [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()];

        Assert.True(vehicle.ReplaceMediaAssets(varliklar).IsSuccess);

        return (vehicle, varliklar);
    }

    [Fact]
    public void LokasyonAtamasiBekler_SubeKapandiginda()
    {
        Vehicle vehicle = new VehicleBuilder().Build();

        vehicle.MarkAwaitingBranchAssignment();

        Assert.True(vehicle.AwaitingBranchAssignment);
    }

    [Fact]
    public void BeklemeBiter_YeniSubeAtandiginda()
    {
        Vehicle vehicle = new VehicleBuilder().Build();
        vehicle.MarkAwaitingBranchAssignment();
        Guid yeniSube = Guid.CreateVersion7();

        vehicle.AssignBranch(yeniSube);

        Assert.False(vehicle.AwaitingBranchAssignment);
        Assert.Equal(yeniSube, vehicle.BranchId);
    }

    [Fact]
    public void Olusturulamaz_NegatifKilometreyle()
    {
        Result<Vehicle> result = Vehicle.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            VehicleSpecification.Create(
                BodyTypeSlug.Create("suv").Value,
                "diesel",
                "automatic",
                "front-wheel-drive",
                "base").Value,
            LocalizedText.Empty,
            2020,
            -1,
            ListingPrice.OnRequest(),
            Vin.Create("TESTVIN0000000001").Value,
            DateTimeOffset.UnixEpoch);

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.NegativeMileage, result.Error.Code);
    }

    [Theory]
    [InlineData("SUV")]
    [InlineData("station wagon")]
    [InlineData("-sedan")]
    [InlineData("")]
    public void Reddedilir_KanonikOlmayanKasaTipiSlugu(string slug)
    {
        Result<BodyTypeSlug> result = BodyTypeSlug.Create(slug);

        Assert.True(result.IsFailure);
        Assert.Equal(BodyTypeSlug.InvalidSlug, result.Error.Code);
    }
}
