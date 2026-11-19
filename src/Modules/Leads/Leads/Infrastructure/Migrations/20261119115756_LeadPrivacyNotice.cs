using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Leads.Infrastructure.Migrations
{
    /// <summary>
    /// `0019` — talebin KVKK aydınlatma metni sürümüne bağı (R3, R5, R6). Var olan talepler
    /// kolonun veritabanı varsayılanıyla "kayıt tutulmuyordu" olur ve hiçbir sürüme — başlangıç
    /// sürümüne de — bağlanmaz: o an hangi metnin yayında olduğu kanıtlanamaz (D1, X4, AC-34).
    /// Kimlik `Content`'teki sürümü SOFT gösterir; şemalar arası foreign key yoktur (C2).
    /// </summary>
    public partial class LeadPrivacyNotice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "privacy_notice_status",
                schema: "leads",
                table: "leads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "not_recorded");

            migrationBuilder.AddColumn<Guid>(
                name: "privacy_notice_version_id",
                schema: "leads",
                table: "leads",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_leads_privacy_notice",
                schema: "leads",
                table: "leads",
                sql: "(privacy_notice_status = 'published' AND privacy_notice_version_id IS NOT NULL) OR (privacy_notice_status IN ('not_published', 'not_recorded') AND privacy_notice_version_id IS NULL)");
        }

        /// <summary>
        /// `0019` D23 — geri alınamaz. <c>Down</c> taleplerin sürüm bağlarını silerdi: bu kanıt
        /// kaybıdır ve geri getirilemez. Hata ileri düzeltme migration'ıyla çözülür.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                "0019 D23: LeadPrivacyNotice geri alınamaz; taleplerin sürüm bağları kanıttır.");
    }
}
