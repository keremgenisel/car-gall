using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// 0010b **D28**'in tripwire'ı.
///
/// <para>Vitrin okuma modeli locale → METİN tutuyor ve projektör yalnız `locale'li metin`
/// tipini oraya taşıyor; tutar, tarih, tam sayı, evet/hayır ve seçenek tiplerinin metin
/// hâli LOCALE'E BAĞLI ve biçimlendirme uçta yapılır (`docs/conventions.md` § 7).</para>
///
/// <para><b>Bugün bedel sıfır</b>: public opsiyonel alan yok. Bu test birisi bir para
/// alanını public yaptığında sessiz kalmamak için var — o gün değer vitrine HİÇ
/// gitmezdi ve kimse fark etmezdi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinTipKapisiTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Yoktur_VitrineTasinamayanTipteBirPublicAlan()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        List<VehicleFieldSetting> ayarlar = await context.VehicleFieldSettings
            .AsNoTracking()
            .ToListAsync();

        string[] sorunlular =
        [
            .. ayarlar
                .Where(setting => !setting.IsCore && setting.IsOpen && setting.IsPublic)
                .Where(setting =>
                    !VehicleFieldDefinitions.TryGet(setting.FieldKey, out VehicleFieldDefinition tanim)
                    || tanim.Kind is not FieldValueKind.LocalizedText)
                .Select(setting => setting.FieldKey)
        ];

        Assert.True(
            sorunlular.Length == 0,
            $"""
             Vitrine açılmış ama okuma modeline taşınamayan alan(lar): {string.Join(", ", sorunlular)}.
             Okuma modeli locale → metin tutuyor; bu tiplerin metin hâli locale'e bağlı ve
             biçimlendirme uçta yapılıyor. Doğru çözüm okuma modeline tipli bir kanal açmak
             (0002'nin sözleşmesi genişler) — alanı sessizce public bırakmak değil.
             """);
    }
}
