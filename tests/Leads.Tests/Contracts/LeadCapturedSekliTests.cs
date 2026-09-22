using System.Reflection;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Contracts.Events;

namespace CarGallery.Modules.Leads.Tests.Contracts;

/// <summary>
/// Sözleşme testi: entegrasyon olayının ŞEKLİ kırılmasın (docs/testing.md § 1).
/// Burada sınanan şey yalnız uyumluluk değil, bir KVKK kuralıdır: outbox gövdesi kalıcı
/// olduğu için olaya eklenen bir kişisel alan, anonimleştirmeden sonra da orada kalır.
/// Bu test o alanın sessizce eklenmesini engeller.
/// </summary>
public sealed class LeadCapturedSekliTests
{
    private static readonly string[] BeklenenAlanlar =
    [
        nameof(LeadCaptured.EventId),
        nameof(LeadCaptured.OccurredAt),
        nameof(LeadCaptured.LeadId),
        nameof(LeadCaptured.BranchId),
        nameof(LeadCaptured.ListingId),
        nameof(LeadCaptured.LeadType),
        nameof(LeadCaptured.SubmittedLocale),
    ];

    [Fact(DisplayName = "AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public void TasirYalnizBeklenenAlanlari_LeadCaptured()
    {
        string[] alanlar =
        [
            .. typeof(LeadCaptured)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != "EqualityContract")
                .Select(property => property.Name)
                .Order()
        ];

        Assert.Equal([.. BeklenenAlanlar.Order()], alanlar);
    }

    [Fact(DisplayName = "AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public void TasimazKiselAlan_LeadCaptured()
    {
        string[] yasakParcalar = ["name", "phone", "email", "message", "contact", "consent"];

        foreach (PropertyInfo property in typeof(LeadCaptured).GetProperties())
        {
            foreach (string parca in yasakParcalar)
            {
                Assert.False(
                    property.Name.Contains(parca, StringComparison.OrdinalIgnoreCase),
                    $"Duyuru kişisel alan taşıyamaz (L6, R13): '{property.Name}'.");
            }
        }
    }

    [Fact(DisplayName = "AC-30: Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir")]
    public void EntegrasyonOlayidir_LeadCaptured() =>
        Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(typeof(LeadCaptured)));
}
