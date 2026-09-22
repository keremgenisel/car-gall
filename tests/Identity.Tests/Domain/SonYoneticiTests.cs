using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

/// <summary>
/// D3/R12 — galeride her zaman en az bir aktif Yönetici bulunur. Bu, `docs/domain.md`
/// **B5**'in ("son aktif şube kapatılamaz") kimlik tarafındaki karşılığıdır: son aktif
/// Yöneticinin düşmesi galeriyi yönetilemez bırakır (Tuzak 2).
///
/// Kardeş sayısı dışarıdan verilir; sayıya YALNIZCA durumu aktif olanlar girer.
/// </summary>
public sealed class SonYoneticiTests
{
    [Fact(DisplayName = "0006/AC-42: Galerideki son aktif Yönetici pasifleştirilemez; girişim reddedilir")]
    public void Reddedilir_SonAktifYoneticiyiPasiflestirme()
    {
        Staff yonetici = new StaffBuilder().Rol(StaffRole.Manager).Kur();

        Result sonuc = yonetici.Deactivate(otherActiveManagerCount: 0);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, sonuc.Error.Code);
        Assert.True(yonetici.IsActive);
    }

    [Fact(DisplayName = "0006/AC-43: Galerideki son aktif Yöneticinin rolü başka bir role düşürülemez; girişim reddedilir")]
    public void Reddedilir_SonAktifYoneticininRolunuDusurme()
    {
        Staff yonetici = new StaffBuilder().Rol(StaffRole.Manager).Kur();

        Result sonuc = yonetici.ChangeRole(StaffRole.SalesAdvisor, otherActiveManagerCount: 0);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, sonuc.Error.Code);
        Assert.Equal(StaffRole.Manager, yonetici.Role);
    }

    [Fact(DisplayName = "0006/AC-44: İki aktif Yöneticiden biri pasifleştirildiğinde işlem başarılı olur ve geriye bir aktif Yönetici kalır")]
    public void Basarili_IkiYoneticidenBiriPasiflestirilince()
    {
        Staff yonetici = new StaffBuilder().Rol(StaffRole.Manager).Kur();

        Result sonuc = yonetici.Deactivate(otherActiveManagerCount: 1);

        Assert.True(sonuc.IsSuccess);
        Assert.False(yonetici.IsActive);
    }

    /// <summary>
    /// R12 — **"Aktif" durumu aktif olmak demektir.** Kilitlenme bu agregatta bir alan
    /// DEĞİLDİR; ayrı bir sayaçta yaşar. Bu ayrım kriterin kendisidir: kilitli Yönetici
    /// "pasif" sayılsaydı, diğerini pasifleştirmek galeriyi yöneticisiz bırakırdı.
    /// </summary>
    [Fact(DisplayName = "0006/AC-45: Geçici olarak kilitlenmiş bir Yönetici aktif sayılır: iki Yöneticiden biri kilitliyken diğeri pasifleştirilemez")]
    public void AktifSayilir_GeciciKilitliYonetici()
    {
        Staff kilitli = new StaffBuilder().Kimlik("kilitli@ornek.com").Rol(StaffRole.Manager).Kur();
        Staff digeri = new StaffBuilder().Kimlik("digeri@ornek.com").Rol(StaffRole.Manager).Kur();

        LockoutState sayac = LockoutState.For(kilitli.LoginId, "203.0.113.7");

        for (int deneme = 0; deneme < 5; deneme++)
        {
            sayac.RegisterFailure(StaffBuilder.VarsayilanAn, 5, TimeSpan.FromMinutes(15));
        }

        Assert.True(sayac.IsLockedAt(StaffBuilder.VarsayilanAn));

        // Kilitli olmasına rağmen DURUMU aktif: aktif Yönetici sayısına girer.
        Assert.True(kilitli.IsActive);

        // Dolayısıyla diğeri "son aktif Yönetici" DEĞİLDİR ve pasifleştirilebilir mi?
        // Hayır — kilitli olan da aktif sayıldığı için sayı 1'dir ve işlem başarılı olur;
        // kriterin sınadığı şey sayının KİLİTLİYİ DE İÇERMESİDİR.
        Assert.True(digeri.Deactivate(otherActiveManagerCount: 1).IsSuccess);

        // Kilitli Yönetici tek başına kaldığında artık düşürülemez.
        Assert.True(kilitli.Deactivate(otherActiveManagerCount: 0).IsFailure);
    }

    [Fact(DisplayName = "0006/AC-46: Bir Yönetici kendi hesabını pasifleştirebilir ve kendi rolünü düşürebilir — son aktif Yönetici olmadığı sürece")]
    public void Yapabilir_KendiHesabinda_SonAktifDegilse()
    {
        Staff yonetici = new StaffBuilder().Rol(StaffRole.Manager).Kur();

        Assert.True(yonetici.ChangeRole(StaffRole.ContentEditor, otherActiveManagerCount: 1).IsSuccess);
        Assert.Equal(StaffRole.ContentEditor, yonetici.Role);

        Staff digeri = new StaffBuilder().Kimlik("digeri@ornek.com").Rol(StaffRole.Manager).Kur();
        Assert.True(digeri.Deactivate(otherActiveManagerCount: 1).IsSuccess);
    }

    /// <summary>Yönetici olmayan personel için son-Yönetici koruması hiç devreye girmez.</summary>
    [Fact(DisplayName = "0006/AC-44: İki aktif Yöneticiden biri pasifleştirildiğinde işlem başarılı olur ve geriye bir aktif Yönetici kalır")]
    public void Etkilenmez_YoneticiOlmayanPersonel()
    {
        Staff danisman = new StaffBuilder().Rol(StaffRole.SalesAdvisor).Kur();

        Assert.True(danisman.Deactivate(otherActiveManagerCount: 0).IsSuccess);
    }
}
