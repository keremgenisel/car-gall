using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// R13/AC-28, **L6** — talep kaydı silinmez. Kuralın uygulamada değil VERİTABANINDA
/// dayatıldığını sınar: "silme kodu yazmadık" bir kriteri kanıtlamaz, yarın yazılan bir
/// bakım betiği veya admin sorgusu onu sessizce deler.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class SilmeTests(LeadsDatabase database)
{
    [Fact(DisplayName = "AC-28: Kaydedilmiş bir talep silinmez; silme girişimi reddedilir")]
    public async Task Reddedilir_EfUzerindenSilmeGirisimi()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Olustur().Value;
        await KaydetAsync(talep);

        await using LeadsDbContext context = database.CreateContext();
        Lead izlenen = await context.Leads.SingleAsync(item => item.Id == talep.Id);
        context.Leads.Remove(izlenen);

        DbUpdateException hata = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());

        Assert.Contains("lead.delete_rejected", Metin(hata), StringComparison.Ordinal);

        await using LeadsDbContext dogrulama = database.CreateContext();
        Assert.Equal(1, await dogrulama.Leads.CountAsync());
    }

    [Fact(DisplayName = "AC-28: Kaydedilmiş bir talep silinmez; silme girişimi reddedilir")]
    public async Task Reddedilir_HamSqlIleSilmeGirisimi()
    {
        await database.ResetAsync();

        Lead talep = new LeadBuilder().Olustur().Value;
        await KaydetAsync(talep);

        await using LeadsDbContext context = database.CreateContext();

        PostgresException hata = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync("DELETE FROM leads.leads"));

        Assert.Equal("lead.delete_rejected", hata.MessageText);

        await using LeadsDbContext dogrulama = database.CreateContext();
        Assert.Equal(1, await dogrulama.Leads.CountAsync());
    }

    /// <summary>
    /// Reddedilen şey silme SONUCU değil, silme GİRİŞİMİDİR: hiçbir satıra denk gelmeyen
    /// bir `DELETE` de reddedilir. Tetikleyici bu yüzden satır değil DEYİM seviyesinde.
    /// </summary>
    [Fact(DisplayName = "AC-28: Kaydedilmiş bir talep silinmez; silme girişimi reddedilir")]
    public async Task Reddedilir_HicbirSatiraDenkGelmeyenSilmeGirisimi()
    {
        await database.ResetAsync();

        await using LeadsDbContext context = database.CreateContext();

        PostgresException hata = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                "DELETE FROM leads.leads WHERE id = '00000000-0000-0000-0000-000000000000'"));

        Assert.Equal("lead.delete_rejected", hata.MessageText);
    }

    private static string Metin(Exception hata)
    {
        Exception? gecerli = hata;
        List<string> parcalar = [];

        while (gecerli is not null)
        {
            parcalar.Add(gecerli.Message);
            gecerli = gecerli.InnerException;
        }

        return string.Join(" | ", parcalar);
    }

    private async Task KaydetAsync(Lead talep)
    {
        await using LeadsDbContext context = database.CreateContext();
        context.Leads.Add(talep);
        await database.CreateUnitOfWork(context, new FixedClock(LeadBuilder.VarsayilanAn))
            .SaveChangesAsync(CancellationToken.None);
    }
}
