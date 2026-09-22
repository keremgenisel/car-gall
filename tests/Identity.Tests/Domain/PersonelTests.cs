using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Domain.Events;

namespace CarGallery.Modules.Identity.Tests.Domain;

public sealed class PersonelTests
{
    [Fact(DisplayName = "AC-32: İlk parolasıyla giriş yapan personelden parolasını değiştirmesi istenir; değiştirmeden yönetim yüzeyinde çalışamaz")]
    public void ZorunluDegisimIsaretliDogar_YeniPersonel()
    {
        Staff personel = new StaffBuilder().Kur();

        Assert.True(personel.MustChangePassword);
    }

    [Fact(DisplayName = "AC-34: Personel kendi parolasını mevcut parolasını vererek değiştirir; mevcut parola yanlış verildiğinde değişiklik reddedilir")]
    public void Degistirir_MevcutParolaDogruVerildiginde()
    {
        Staff personel = new StaffBuilder().Parola("eski-parola-123").Kur();
        SecurityStamp oncekiDamga = personel.SecurityStamp;

        Result sonuc = personel.ChangeOwnPassword(
            "eski-parola-123",
            PasswordHash.Create("yeni-parola-123", StaffBuilder.TestIterasyon));

        Assert.True(sonuc.IsSuccess);
        Assert.False(personel.MustChangePassword);
        Assert.True(personel.PasswordHash.Matches("yeni-parola-123"));

        // R11/AC-22 — damga döner: o personelin TÜM yenileme hakları geçersizleşir.
        Assert.NotEqual(oncekiDamga, personel.SecurityStamp);
    }

    [Fact(DisplayName = "AC-34: Personel kendi parolasını mevcut parolasını vererek değiştirir; mevcut parola yanlış verildiğinde değişiklik reddedilir")]
    public void Reddedilir_MevcutParolaYanlisVerildiginde()
    {
        Staff personel = new StaffBuilder().Parola("eski-parola-123").Kur();
        SecurityStamp oncekiDamga = personel.SecurityStamp;

        Result sonuc = personel.ChangeOwnPassword(
            "yanlis-parola-123",
            PasswordHash.Create("yeni-parola-123", StaffBuilder.TestIterasyon));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.CurrentPasswordInvalid, sonuc.Error.Code);
        Assert.True(personel.PasswordHash.Matches("eski-parola-123"));
        Assert.Equal(oncekiDamga, personel.SecurityStamp);
    }

    [Fact(DisplayName = "AC-35: Yönetici bir personelin parolasını sıfırlar; personel sıfırlanan parolayla giriş yaptığında kendisinden parola değiştirmesi istenir")]
    public void ZorunluDegisimIsaretler_ParolaSifirlama()
    {
        Staff personel = new StaffBuilder().Kur();
        personel.ChangeOwnPassword("cok-guclu-parola", PasswordHash.Create("ikinci-parola-1", StaffBuilder.TestIterasyon));

        Assert.False(personel.MustChangePassword);

        SecurityStamp oncekiDamga = personel.SecurityStamp;
        personel.ResetPassword(PasswordHash.Create("sifirlanan-parola", StaffBuilder.TestIterasyon));

        Assert.True(personel.MustChangePassword);
        Assert.True(personel.PasswordHash.Matches("sifirlanan-parola"));
        Assert.NotEqual(oncekiDamga, personel.SecurityStamp);
    }

    [Fact(DisplayName = "AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public void Duyurulur_Pasiflestirildiginde()
    {
        Staff personel = new StaffBuilder().Rol(StaffRole.SalesAdvisor).Kur();

        personel.Deactivate(otherActiveManagerCount: 1);

        IDomainEvent tek = Assert.Single(personel.DomainEvents);
        StaffWasDeactivated duyuru = Assert.IsType<StaffWasDeactivated>(tek);
        Assert.Equal(personel.Id, duyuru.StaffId);
    }

    /// <summary>
    /// R14 — pasifleştirme bir GÜVENLİK işlemidir. Damganın dönmesi, o personelin bütün
    /// oturumlarını bir sonraki istekte geçersiz kılar (Tuzak 4).
    /// </summary>
    [Fact(DisplayName = "AC-20: Bir personel pasifleştirildiğinde açık oturumu derhal geçersizleşir; sonraki ilk istekte yönetim yüzeyine erişemez")]
    public void DamgaDoner_Pasiflestirildiginde()
    {
        Staff personel = new StaffBuilder().Rol(StaffRole.SalesAdvisor).Kur();
        SecurityStamp oncekiDamga = personel.SecurityStamp;

        personel.Deactivate(otherActiveManagerCount: 1);

        Assert.NotEqual(oncekiDamga, personel.SecurityStamp);
    }

    [Fact(DisplayName = "AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public void Reddedilir_IkinciKezPasiflestirme()
    {
        Staff personel = new StaffBuilder().Rol(StaffRole.SalesAdvisor).Kur();
        personel.Deactivate(otherActiveManagerCount: 1);
        personel.ClearDomainEvents();

        Result ikinci = personel.Deactivate(otherActiveManagerCount: 1);

        Assert.True(ikinci.IsFailure);
        Assert.Equal(IdentityErrors.AlreadyDeactivated, ikinci.Error.Code);
        Assert.Empty(personel.DomainEvents);
    }

    [Fact(DisplayName = "AC-31: Yönetici bir personel eklediğinde personel listede görünür ve verilen kimlikle giriş yapabilir")]
    public void Eklenemez_AdiBossa()
    {
        Result<Staff> sonuc = new StaffBuilder().Ad("   ").Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.EmptyDisplayName, sonuc.Error.Code);
    }

    [Fact(DisplayName = "AC-39: Şubesiz personel eklenebilir; şube zorunlu değildir")]
    public void Eklenir_Subesiz()
    {
        Staff personel = new StaffBuilder().Sube(null).Kur();

        Assert.Null(personel.BranchId);
    }
}
