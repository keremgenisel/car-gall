using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// **Bilerek BOŞ.** `price_changes.id` istemcide üretiliyor (`Guid.CreateVersion7`) ve
    /// bu yalnız model meta verisidir — DDL karşılığı yok.
    ///
    /// <para>Kayda geçmesinin nedeni ölçülen bir hata: söylenmediğinde EF `Guid` bir
    /// birincil anahtarı mağaza üretimli sayıyor, istemcide dolu gelen anahtarı "zaten var"
    /// diye okuyor ve yeni fiyat geçmişi kaydını `Added` yerine `Modified` yapıyordu —
    /// olmayan bir satıra UPDATE gidiyor ve `DbUpdateConcurrencyException` fırlıyordu.</para>
    ///
    /// <para>Dosya, anlık görüntünün modelle hizalı kalması için duruyor; silmek canlı bir
    /// veritabanı bağlantısı istiyor ve anlık görüntüyü elle düzeltmek onu modelden
    /// ayırma riski taşıyor.</para>
    /// </summary>
    public partial class PriceChangeClientGeneratedKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
