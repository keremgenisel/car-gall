using System.Reflection;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Contracts.Events;

namespace CarGallery.Modules.Identity.Tests.Contracts;

/// <summary>
/// Sözleşme testi: entegrasyon olayının ŞEKLİ kırılmasın (docs/testing.md § 1).
/// Burada sınanan şey yalnız uyumluluk değil: outbox gövdesi kalıcı olduğu için olaya
/// eklenen bir kişisel alan, personel silinse bile orada kalır (C1).
/// </summary>
public sealed class StaffDeactivatedSekliTests
{
    private static readonly string[] BeklenenAlanlar =
    [
        nameof(StaffDeactivated.EventId),
        nameof(StaffDeactivated.OccurredAt),
        nameof(StaffDeactivated.StaffId),
    ];

    private static readonly string[] YasakParcalar =
        ["name", "email", "login", "password", "hash", "phone", "locale", "role"];

    [Fact(DisplayName = "0006/AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public void TasirYalnizBeklenenAlanlari_StaffDeactivated()
    {
        string[] alanlar =
        [
            .. typeof(StaffDeactivated)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != "EqualityContract")
                .Select(property => property.Name)
                .Order()
        ];

        Assert.Equal([.. BeklenenAlanlar.Order()], alanlar);
    }

    [Fact(DisplayName = "0006/AC-47: Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve personelin kimliğini taşır")]
    public void TasimazKisiselAlan_StaffDeactivated()
    {
        foreach (PropertyInfo property in typeof(StaffDeactivated).GetProperties())
        {
            foreach (string parca in YasakParcalar)
            {
                Assert.False(
                    property.Name.Contains(parca, StringComparison.OrdinalIgnoreCase),
                    $"Personelin kim olduğu sınırı geçmez (C1): '{property.Name}'.");
            }
        }
    }

    [Fact(DisplayName = "0006/AC-49: Üretilen her duyuru benzersiz bir duyuru kimliği taşır")]
    public void EntegrasyonOlayidir_StaffDeactivated() =>
        Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(typeof(StaffDeactivated)));
}
