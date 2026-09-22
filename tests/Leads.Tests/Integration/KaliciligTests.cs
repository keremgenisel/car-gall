using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

[Collection(LeadsDatabaseCollection.Name)]
public sealed class KaliciligTests(LeadsDatabase database)
{
    [Fact(DisplayName = "AC-1: İlan detay sayfasından, adı ve telefonu girilmiş bir bilgi talebi bırakıldığında talep kaydedilir")]
    public async Task Kaydedilir_AdVeTelefonVerildiginde()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder()
            .Tip(LeadType.InformationRequest)
            .Ad("Ayşe Yılmaz")
            .Telefon("+905321112233")
            .Olustur()
            .Value;

        await KaydetAsync(talep);

        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.Equal("Ayşe Yılmaz", okunan.Contact.FullName);
        Assert.Equal("+905321112233", okunan.Contact.Phone);
        Assert.Equal(LeadType.InformationRequest, okunan.Type);
        Assert.Equal(LeadStatus.New, okunan.Status);
    }

    [Fact(DisplayName = "AC-16: Talep, bırakıldığı locale ile kaydedilir; Arapça sayfadan bırakılan talebin locale'i `ar` olur")]
    public async Task Kaydedilir_BirakildigiLocaleIle()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Locale("ar").Olustur().Value;
        await KaydetAsync(talep);

        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.Equal("ar", okunan.SubmittedLocale.Code);
    }

    [Fact(DisplayName = "AC-23: Araca bağlı talep, o aracın şubesine bağlanır")]
    public async Task Baglanir_AracinSubesine()
    {
        Guid subeId = Guid.CreateVersion7();

        await database.ResetAsync();

        Lead talep = new LeadBuilder().Sube(subeId).Olustur().Value;
        await KaydetAsync(talep);

        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.Equal(subeId, okunan.BranchId);
    }

    [Fact(DisplayName = "AC-24: İletişim izni verilerek bırakılan talepte izin verildi olarak kaydedilir")]
    public async Task Kaydedilir_IzinVerildi()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Izin(true).Olustur().Value;
        await KaydetAsync(talep);

        Assert.True((await OkuAsync(talep.Id))!.ConsentGiven);
    }

    [Fact(DisplayName = "AC-25: İletişim izni verilmeden bırakılan talepte izin verilmedi olarak kaydedilir; talep yine de kaydedilir")]
    public async Task Kaydedilir_IzinVerilmedi()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Izin(false).Olustur().Value;
        await KaydetAsync(talep);

        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.False(okunan.ConsentGiven);
    }

    [Fact(DisplayName = "AC-29: Talebin kişisel alanları (ad, telefon, e-posta, mesaj) ile istatistiksel alanları (tip, şube, locale, tarih, anlık görüntü) ayrı ayrı okunabilir; kişisel alanlar boşaltıldığında kayıt ve istatistiksel alanları bozulmadan kalır")]
    public async Task Korunur_Kayit_KisiselAlanlarBosaltildiginda()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder()
            .Ad("Ayşe Yılmaz")
            .Telefon("+905321112233")
            .Eposta("ayse@ornek.com")
            .Mesaj("Hafta sonu görebilir miyim?")
            .Olustur()
            .Value;

        await KaydetAsync(talep);

        await using (LeadsDbContext context = database.CreateContext())
        {
            Lead izlenen = await context.Leads.SingleAsync(item => item.Id == talep.Id);
            izlenen.Anonymize();
            await context.SaveChangesAsync();
        }

        Lead? okunan = await OkuAsync(talep.Id);

        Assert.NotNull(okunan);
        Assert.True(okunan.IsAnonymized());
        Assert.Equal(LeadType.InformationRequest, okunan.Type);
        Assert.Equal(LeadBuilder.VarsayilanSubeId, okunan.BranchId);
        Assert.Equal("tr", okunan.SubmittedLocale.Code);
        Assert.Equal("Toyota Corolla", okunan.Vehicle.CanonicalModelName);
        Assert.Equal(1_250_000m, okunan.Vehicle.PriceAmount);
    }

    private async Task KaydetAsync(Lead talep)
    {
        await using LeadsDbContext context = database.CreateContext();
        context.Leads.Add(talep);
        await database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
            .SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Lead?> OkuAsync(LeadId id)
    {
        await using LeadsDbContext context = database.CreateContext();

        return await context.Leads.AsNoTracking().SingleOrDefaultAsync(lead => lead.Id == id);
    }
}
