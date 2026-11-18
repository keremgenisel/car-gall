using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CarGallery.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "faq_entries",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faq_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faq_entry_localizations",
                schema: "content",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faq_entry_localizations", x => new { x.entry_id, x.locale });
                    table.ForeignKey(
                        name: "FK_faq_entry_localizations_faq_entries_entry_id",
                        column: x => x.entry_id,
                        principalSchema: "content",
                        principalTable: "faq_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_localizations",
                schema: "content",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_localizations", x => new { x.page_id, x.locale });
                    table.ForeignKey(
                        name: "FK_page_localizations_pages_page_id",
                        column: x => x.page_id,
                        principalSchema: "content",
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_old_slugs",
                schema: "content",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_old_slugs", x => new { x.locale, x.slug });
                    table.ForeignKey(
                        name: "FK_page_old_slugs_pages_page_id",
                        column: x => x.page_id,
                        principalSchema: "content",
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "content",
                table: "pages",
                columns: new[] { "id", "created_at", "system_key", "version" },
                values: new object[,]
                {
                    { new Guid("0192c000-0000-7000-8000-0000c0a70001"), new DateTimeOffset(new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "about", 1 },
                    { new Guid("0192c000-0000-7000-8000-0000c0a70002"), new DateTimeOffset(new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "privacy", 1 },
                    { new Guid("0192c000-0000-7000-8000-0000c0a70003"), new DateTimeOffset(new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "cookies", 1 },
                    { new Guid("0192c000-0000-7000-8000-0000c0a70004"), new DateTimeOffset(new DateTime(2026, 11, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "faq", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_faq_entries_position",
                schema: "content",
                table: "faq_entries",
                column: "position");

            migrationBuilder.CreateIndex(
                name: "ux_page_localizations_locale_slug",
                schema: "content",
                table: "page_localizations",
                columns: new[] { "locale", "slug" },
                unique: true,
                filter: "slug <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_page_old_slugs_page_id",
                schema: "content",
                table: "page_old_slugs",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "ux_pages_system_key",
                schema: "content",
                table: "pages",
                column: "system_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faq_entry_localizations",
                schema: "content");

            migrationBuilder.DropTable(
                name: "page_localizations",
                schema: "content");

            migrationBuilder.DropTable(
                name: "page_old_slugs",
                schema: "content");

            migrationBuilder.DropTable(
                name: "faq_entries",
                schema: "content");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "content");
        }
    }
}
