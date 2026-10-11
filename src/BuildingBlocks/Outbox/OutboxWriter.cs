using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Duyuruyu iş verisiyle AYNI `DbContext` üzerinden yazar. Ayrı bir bağlantı veya ayrı bir
/// `SaveChanges` kullanılmaz: ikisi ayrı düşerse ya duyurulmayan bir değişiklik ya da
/// olmayan bir değişikliğin duyurusu kalır (R11, AC-25).
///
/// <para><b>Bu tipin non-generic bir arayüzü YOKTUR ve olmayacaktır.</b> Vardı ve bir kaçan
/// hata üretti: `IOutboxWriter` üç modül tarafından kendi `DbContext`'iyle kaydediliyordu,
/// DI'da son kayıt kazanır ve `Program.cs`'te `AddLeads` en sonda çağrılıyordu. `Identity`
/// ve `Dealership`, duyurularını `OutboxWriter&lt;LeadsDbContext&gt;` ile yazıyordu: satır
/// `Leads`'in değişiklik izleyicisine düşüyor, modül kendi `DbContext`'ini kaydediyor ve
/// duyuru hiç kalıcı olmuyordu. Ölçüldü (2026-10-11, birleştirilmiş Host): personel
/// pasifleştirme `204` dönüyor, `identity.staff.status` pasife geçiyor ve `StaffDeactivated`
/// HİÇBİR outbox tablosunda bulunmuyordu.</para>
///
/// <para>Tüketici bu tipi <b>konkret</b> olarak enjekte eder:
/// `OutboxWriter&lt;IdentityDbContext&gt;`. Konkret tip, hangi şemaya yazdığını imzasında
/// taşır ve iki modülün kaydı birbirinin yerine geçemez. Bir mimari kapı
/// (`OutboxYazicisiTests`) paylaşılan bir arayüz kaydının geri gelmesini de yasaklar —
/// ama birincil koruma kapı değil, arayüzün YOKLUĞUDUR: olmayan bir tuzak kurulamaz.</para>
/// </summary>
public sealed class OutboxWriter<TDbContext>(TDbContext context, IClock clock)
    where TDbContext : DbContext, IOutboxDbContext
{
    public void Write(IIntegrationEvent integrationEvent)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = integrationEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            OccurredAt = integrationEvent.OccurredAt,
            NextAttemptAt = clock.UtcNow,
        });
    }
}
