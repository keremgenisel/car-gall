using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Leads.Infrastructure.Migrations
{
    /// <summary>
    /// R13/AC-28, docs/domain.md **L6** — talep kaydı SİLİNMEZ; silme girişimi reddedilir.
    ///
    /// Kural veritabanında dayatılır, uygulamada değil. "Silme kodu yazmadık" bir kriteri
    /// kanıtlamaz: yarın yazılan bir bakım betiği, bir admin sorgusu veya bir EF
    /// <c>Remove</c> çağrısı kuralı sessizce deler. Bu, docs/architecture.md § 5'in
    /// "sınır veritabanı seviyesinde de dayatılır" çizgisiyle aynı yaklaşımdır.
    ///
    /// Tetikleyici DEYİM seviyesindedir: hiçbir satıra denk gelmeyen bir <c>DELETE</c> de
    /// reddedilir — reddedilen şey silme SONUCU değil, silme GİRİŞİMİDİR.
    ///
    /// Kapsamadığı yer, bilerek yazılıyor: <c>TRUNCATE</c> bu tetikleyiciyi çalıştırmaz.
    /// Kapsansaydı entegrasyon testlerinin kendi verisini temizleme yolu kapanırdı
    /// (docs/testing.md § 4). <c>TRUNCATE</c> tablo sahipliği ister ve şema başına ayrı
    /// PostgreSQL rolü (architecture.md § 5) onu uygulamanın erişemeyeceği yerde tutar.
    /// </summary>
    public partial class AddLeadDeleteGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION leads.reject_lead_delete() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'lead.delete_rejected'
                        USING HINT = 'docs/domain.md L6: talep silinmez; kisisel alanlar anonimlestirilir.';
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_leads_reject_delete
                BEFORE DELETE ON leads.leads
                FOR EACH STATEMENT
                EXECUTE FUNCTION leads.reject_lead_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_leads_reject_delete ON leads.leads;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS leads.reject_lead_delete();");
        }
    }
}
