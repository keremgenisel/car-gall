using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewsletterSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "lead_id",
                schema: "notifications",
                table: "messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "period_id",
                schema: "notifications",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subscription_id",
                schema: "notifications",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "consent_events",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_candidates",
                schema: "notifications",
                columns: table => new
                {
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_candidates", x => x.listing_id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_period_listings",
                schema: "notifications",
                columns: table => new
                {
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_period_listings", x => new { x.period_id, x.listing_id });
                });

            migrationBuilder.CreateTable(
                name: "newsletter_periods",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    cutoff_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_subscriptions",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    locale = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    confirmation_token_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    confirmation_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unsubscribed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_messages_subscription_queued_at",
                schema: "notifications",
                table: "messages",
                columns: new[] { "subscription_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ux_messages_subscription_period",
                schema: "notifications",
                table: "messages",
                columns: new[] { "subscription_id", "period_id" },
                unique: true,
                filter: "period_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_messages_single_target",
                schema: "notifications",
                table: "messages",
                sql: "(lead_id IS NOT NULL AND subscription_id IS NULL AND period_id IS NULL) OR (lead_id IS NULL AND subscription_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_consent_events_subscription_occurred_at",
                schema: "notifications",
                table: "consent_events",
                columns: new[] { "subscription_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_candidates_status",
                schema: "notifications",
                table: "newsletter_candidates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_newsletter_periods_baseline",
                schema: "notifications",
                table: "newsletter_periods",
                column: "kind",
                unique: true,
                filter: "kind = 1");

            migrationBuilder.CreateIndex(
                name: "ux_newsletter_periods_regular_cutoff",
                schema: "notifications",
                table: "newsletter_periods",
                column: "cutoff_at",
                unique: true,
                filter: "kind = 2");

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_subscriptions_status",
                schema: "notifications",
                table: "newsletter_subscriptions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_newsletter_subscriptions_confirmation_token_hash",
                schema: "notifications",
                table: "newsletter_subscriptions",
                column: "confirmation_token_hash",
                unique: true,
                filter: "confirmation_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_newsletter_subscriptions_normalized_email",
                schema: "notifications",
                table: "newsletter_subscriptions",
                column: "normalized_email",
                unique: true);

            // `0018` D27 · D34 — izin olay kaydı YALNIZ EKLENİR: güncelleme ve silme
            // veritabanında reddedilir. Kural kod yoluna bırakılsaydı tek bir yanlış
            // `ExecuteDelete` İYS'nin (X8) dayanacağı geçmişi sessizce silerdi.
            migrationBuilder.Sql("""
                CREATE FUNCTION notifications.consent_events_append_only() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'consent_events is append-only';
                END;
                $$;

                CREATE TRIGGER consent_events_append_only
                BEFORE UPDATE OR DELETE ON notifications.consent_events
                FOR EACH ROW EXECUTE FUNCTION notifications.consent_events_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // AP-11 — geri alma talebe bağlı olmayan gönderim kayıtlarını SİLER: `lead_id`
            // yeniden zorunlu olur ve teyit/bülten kayıtlarının bağlanacağı bir talep yok.
            migrationBuilder.Sql("""
                DROP TRIGGER consent_events_append_only ON notifications.consent_events;
                DROP FUNCTION notifications.consent_events_append_only();
                DELETE FROM notifications.messages WHERE lead_id IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "consent_events",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "newsletter_candidates",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "newsletter_period_listings",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "newsletter_periods",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "newsletter_subscriptions",
                schema: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_messages_subscription_queued_at",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ux_messages_subscription_period",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_messages_single_target",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "period_id",
                schema: "notifications",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "subscription_id",
                schema: "notifications",
                table: "messages");

            migrationBuilder.AlterColumn<Guid>(
                name: "lead_id",
                schema: "notifications",
                table: "messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
