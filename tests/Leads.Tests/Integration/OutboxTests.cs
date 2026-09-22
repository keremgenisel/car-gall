using System.Text.Json;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// R14 — duyuru talebin kaydıyla BİRLİKTE kalıcı olur. Burada sınanan şey duyurunun
/// üretilmesi değil (onu domain testi yapıyor), AYNI TRANSACTION'da kalıcılaşmasıdır:
/// iki yazma ayrı düşerse ya duyurulmayan bir talep ya da olmayan bir talebin duyurusu
/// kalır.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class OutboxTests(LeadsDatabase database)
{
    [Fact(DisplayName = "0005/AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public async Task Yazilir_LeadCaptured_TalepKaydedildiginde()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Locale("ar").Tip(LeadType.TestDrive).Olustur().Value;
        await KaydetAsync(talep);

        await using LeadsDbContext context = database.CreateContext();
        OutboxMessage satir = await context.OutboxMessages.AsNoTracking().SingleAsync();

        Assert.Equal(typeof(LeadCaptured).FullName, satir.Type);

        LeadCaptured duyuru = JsonSerializer.Deserialize<LeadCaptured>(satir.Payload)!;

        Assert.Equal(talep.Id.Value, duyuru.LeadId);
        Assert.Equal(talep.BranchId, duyuru.BranchId);
        Assert.Equal(talep.Vehicle.ListingId, duyuru.ListingId);
        Assert.Equal("test_drive", duyuru.LeadType);
        Assert.Equal("ar", duyuru.SubmittedLocale);
    }

    [Fact(DisplayName = "0005/AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public async Task TasimazKisiselVeri_OutboxGovdesi()
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

        await using LeadsDbContext context = database.CreateContext();
        OutboxMessage satir = await context.OutboxMessages.AsNoTracking().SingleAsync();

        // Outbox gövdesi KALICIDIR. Anonimleştirme talebin kolonlarını temizler ama bu
        // jsonb'ye dokunmaz — kişisel veri buraya düşerse bir daha çıkarılamaz (L6).
        Assert.DoesNotContain("Ayşe", satir.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("+905321112233", satir.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("ayse@ornek.com", satir.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("Hafta sonu", satir.Payload, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0005/AC-31: Talep kaydedilmediğinde kaydedilmiş bir `LeadCaptured` duyurusu kalmaz")]
    public async Task Kalmaz_Duyuru_TalepKaydedilmediginde()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Olustur().Value;

        // Kaydetme, veritabanı seviyesinde başarısız olur: aynı idempotentlik anahtarını
        // taşıyan bir satır zaten var. Duyuru outbox'a AYNI SaveChanges içinde yazıldığı
        // için o da geri alınır — yarım kalmış bir duyuru kalmaz.
        await KaydetAsync(talep);

        Lead ikizi = new LeadBuilder()
            .IdempotentlikAnahtari(talep.IdempotencyKey)
            .Olustur()
            .Value;

        await using (LeadsDbContext context = database.CreateContext())
        {
            context.Leads.Add(ikizi);

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
                    .SaveChangesAsync(CancellationToken.None));
        }

        await using LeadsDbContext dogrulama = database.CreateContext();

        Assert.Equal(1, await dogrulama.Leads.CountAsync());
        Assert.Equal(1, await dogrulama.OutboxMessages.CountAsync());
    }

    private async Task KaydetAsync(Lead talep)
    {
        await using LeadsDbContext context = database.CreateContext();
        context.Leads.Add(talep);
        await database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
            .SaveChangesAsync(CancellationToken.None);
    }
}
