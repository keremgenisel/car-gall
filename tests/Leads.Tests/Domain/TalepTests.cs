using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Domain.Events;

namespace CarGallery.Modules.Leads.Tests.Domain;

public sealed class TalepTests
{
    [Fact(DisplayName = "0005/AC-2: Ne telefon ne e-posta girilmiş bir talep kaydedilmez")]
    public void Kaydedilmez_HicIletisimKanaliYoksa()
    {
        Result<Lead> sonuc = new LeadBuilder().Telefon(null).Eposta(null).Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.MissingContactChannel, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-3: Adı girilmemiş bir talep kaydedilmez")]
    public void Kaydedilmez_AdBossa()
    {
        Result<Lead> sonuc = new LeadBuilder().Ad("   ").Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.MissingName, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-6: Ad, e-posta veya mesaj alanı üst uzunluk sınırını aşan bir talep kaydedilmez")]
    public void Kaydedilmez_AdUstSiniriAsildiginda()
    {
        Result<Lead> sonuc = new LeadBuilder()
            .Ad(new string('a', ContactDetails.MaximumNameLength + 1))
            .Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.NameTooLong, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-6: Ad, e-posta veya mesaj alanı üst uzunluk sınırını aşan bir talep kaydedilmez")]
    public void Kaydedilmez_EpostaUstSiniriAsildiginda()
    {
        string cokUzun = new string('a', 250) + "@ornek.com";

        Result<Lead> sonuc = new LeadBuilder().Telefon(null).Eposta(cokUzun).Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.EmailTooLong, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-6: Ad, e-posta veya mesaj alanı üst uzunluk sınırını aşan bir talep kaydedilmez")]
    public void Kaydedilmez_MesajUstSiniriAsildiginda()
    {
        Result<Lead> sonuc = new LeadBuilder()
            .Mesaj(new string('m', ContactDetails.MaximumMessageLength + 1))
            .Olustur();

        Assert.True(sonuc.IsFailure);
        Assert.Equal(LeadErrors.MessageTooLong, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0005/AC-7: Mesaj alanı boş bırakılarak bir talep kaydedilebilir")]
    public void Kaydedilir_MesajBosBirakildiginda()
    {
        Result<Lead> sonuc = new LeadBuilder().Mesaj(null).Olustur();

        Assert.True(sonuc.IsSuccess);
        Assert.Null(sonuc.Value.Contact.Message);
    }

    [Fact(DisplayName = "0005/AC-8: Oluşturulan talep `Yeni` durumundadır")]
    public void YeniDurumundadir_Olusturuldugunda()
    {
        Lead talep = new LeadBuilder().Olustur().Value;

        Assert.Equal(LeadStatus.New, talep.Status);
    }

    [Fact(DisplayName = "0005/AC-24: İletişim izni verilerek bırakılan talepte izin verildi olarak kaydedilir")]
    public void Kaydedilir_IzinVerildi()
    {
        Lead talep = new LeadBuilder().Izin(true).Olustur().Value;

        Assert.True(talep.ConsentGiven);
    }

    [Fact(DisplayName = "0005/AC-25: İletişim izni verilmeden bırakılan talepte izin verilmedi olarak kaydedilir; talep yine de kaydedilir")]
    public void Kaydedilir_IzinVerilmedi_TalepYineDeOlusur()
    {
        Result<Lead> sonuc = new LeadBuilder().Izin(false).Olustur();

        Assert.True(sonuc.IsSuccess);
        Assert.False(sonuc.Value.ConsentGiven);
    }

    [Fact(DisplayName = "0005/AC-29: Talebin kişisel alanları (ad, telefon, e-posta, mesaj) ile istatistiksel alanları (tip, şube, locale, tarih, anlık görüntü) ayrı ayrı okunabilir; kişisel alanlar boşaltıldığında kayıt ve istatistiksel alanları bozulmadan kalır")]
    public void Korunur_IstatistikselAlanlar_KisiselAlanlarBosaltildiginda()
    {
        Lead talep = new LeadBuilder()
            .Ad("Ayşe Yılmaz")
            .Telefon("+905321112233")
            .Eposta("ayse@ornek.com")
            .Mesaj("Aracı hafta sonu görebilir miyim?")
            .Olustur()
            .Value;

        talep.Anonymize();

        Assert.True(talep.IsAnonymized());
        Assert.Null(talep.Contact.FullName);
        Assert.Null(talep.Contact.Phone);
        Assert.Null(talep.Contact.Email);
        Assert.Null(talep.Contact.Message);

        Assert.Equal(LeadType.InformationRequest, talep.Type);
        Assert.Equal(LeadBuilder.VarsayilanSubeId, talep.BranchId);
        Assert.Equal("tr", talep.SubmittedLocale.Code);
        Assert.Equal(LeadBuilder.VarsayilanAn, talep.CapturedAt);
        Assert.Equal("Toyota Corolla", talep.Vehicle.CanonicalModelName);
        Assert.Equal(1_250_000m, talep.Vehicle.ReadPrice().Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0005/AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public void Duyurulur_TalepOlustugunda()
    {
        Lead talep = new LeadBuilder().Olustur().Value;

        IDomainEvent tek = Assert.Single(talep.DomainEvents);
        LeadWasCaptured duyuru = Assert.IsType<LeadWasCaptured>(tek);
        Assert.Equal(talep.Id, duyuru.LeadId);
    }
}
