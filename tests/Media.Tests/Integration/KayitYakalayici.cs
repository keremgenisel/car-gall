using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// AC-23'ün gerçek ölçümü. "Sonunda ikisi de doğru" demek atomikliği KANITLAMAZ: iki ayrı
/// `SaveChanges` de aynı son durumu üretir ve arada süreç düşerse biri olmadan diğeri
/// kalır.
///
/// <para>Bu dinleyici her `SaveChanges` çağrısında DEĞİŞEN TİPLERİ kaydediyor. Varlık
/// güncellemesi ve outbox eklemesi AYNI çağrıda görünüyorsa ikisi aynı transaction'dadır;
/// ayrı çağrılarda görünüyorsa testin iddiası yanlıştır.</para>
/// </summary>
internal sealed class KayitYakalayici : ISaveChangesInterceptor
{
    private readonly List<IReadOnlyList<string>> turlar = [];

    /// <summary>Her eleman bir `SaveChanges` çağrısı; içindekiler o çağrıda yazılan tipler.</summary>
    internal IReadOnlyList<IReadOnlyList<string>> Turlar => turlar;

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Kaydet(eventData);

        return ValueTask.FromResult(result);
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Kaydet(eventData);

        return result;
    }

    private void Kaydet(DbContextEventData eventData)
    {
        if (eventData.Context is null)
        {
            return;
        }

        List<string> yazilan = [];

        foreach (EntityEntry giris in eventData.Context.ChangeTracker.Entries())
        {
            if (giris.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                yazilan.Add($"{giris.Entity.GetType().Name}:{giris.State}");
            }
        }

        if (yazilan.Count > 0)
        {
            turlar.Add(yazilan);
        }
    }
}
