using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Content.Infrastructure.Migrations
{
    /// <summary>
    /// `0019` — KVKK aydınlatma metninin yayınlanmış sürümleri (R1, R2, R7, R13).
    ///
    /// <para><b>Başlangıç sürümü (D6, AC-16).</b> Migration anında `privacy`'nin yayında
    /// olduğu her dil için o anki metin bir başlangıç sürümü olur ve dilin işaretçisi ona
    /// bağlanır; kayda geçtiği an migration'ın anıdır ("bu andan itibaren kayıtlı"). Taslak
    /// dil sürüm almaz. Var olan talepler buradan etkilenmez: onlar `leads`'in kendi
    /// migration'ında "kayıt tutulmuyordu" olur (X4).</para>
    ///
    /// <para><b>Kimlik Guid v7 (P6).</b> PostgreSQL 17'de <c>uuidv7()</c> yok; v7 bitleri
    /// rastgele bir v4'ün ilk 48 bitine milisaniye zaman damgası yazılıp sürüm alanı
    /// <c>0111</c>'e çekilerek kurulur (`docs/conventions.md` § 9).</para>
    ///
    /// <para><b>Koruma (C5, D11, D18).</b> Sürüm tablosunda <c>UPDATE</c> ve <c>DELETE</c>
    /// DEYİM düzeyinde reddedilir — hiçbir satıra denk gelmeyen girişim de. Emsal
    /// <c>AddLeadDeleteGuard</c>: <c>TRUNCATE</c> bilerek kapsam dışıdır (testlerin temizlik
    /// yolu; tablo sahipliği uygulama rolünde değil).</para>
    /// </summary>
    public partial class PrivacyNoticeVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "published_version_id",
                schema: "content",
                table: "page_localizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "privacy_notice_versions",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_initial = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privacy_notice_versions", x => x.id);
                });

            migrationBuilder.Sql(
                """
                WITH yeni AS (
                    INSERT INTO content.privacy_notice_versions (id, locale, title, body, recorded_at, is_initial)
                    SELECT
                        encode(
                            set_bit(set_bit(
                                overlay(uuid_send(gen_random_uuid())
                                    placing substring(int8send((extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3)
                                    FROM 1 FOR 6),
                                52, 1), 53, 1),
                            'hex')::uuid,
                        satir.locale, satir.title, satir.body, now(), true
                    FROM content.page_localizations satir
                    JOIN content.pages sayfa ON sayfa.id = satir.page_id
                    WHERE sayfa.system_key = 'privacy' AND satir.status = 'published'
                    RETURNING id, locale)
                UPDATE content.page_localizations satir
                SET published_version_id = yeni.id
                FROM yeni, content.pages sayfa
                WHERE sayfa.id = satir.page_id
                  AND sayfa.system_key = 'privacy'
                  AND satir.locale = yeni.locale;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION content.reject_privacy_notice_version_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'content.privacy_notice_version_immutable'
                        USING HINT = 'spec 0019 C5: yayinlanmis metin surumu duzenlenmez ve silinmez.';
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_privacy_notice_versions_reject_change
                BEFORE UPDATE OR DELETE ON content.privacy_notice_versions
                FOR EACH STATEMENT
                EXECUTE FUNCTION content.reject_privacy_notice_version_change();
                """);
        }

        /// <summary>
        /// `0019` D23 — geri alınamaz. <c>Down</c> sürüm satırlarını ve işaretçiyi düşürürdü:
        /// bu, taleplerin bağlı olduğu kanıtın kaybıdır ve C5'i arka kapıdan deler. Hata ileri
        /// düzeltme migration'ıyla çözülür.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                "0019 D23: PrivacyNoticeVersions geri alınamaz; yayınlanmış sürümler kanıttır.");
    }
}
