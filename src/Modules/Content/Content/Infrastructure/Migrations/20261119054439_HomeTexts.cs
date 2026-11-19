using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Content.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HomeTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "home_texts",
                schema: "content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_texts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "home_text_localizations",
                schema: "content",
                columns: table => new
                {
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    home_texts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    intro_title = table.Column<string>(type: "text", nullable: false),
                    intro_body = table.Column<string>(type: "text", nullable: false),
                    form_title = table.Column<string>(type: "text", nullable: false),
                    featured_title = table.Column<string>(type: "text", nullable: false),
                    empty_title = table.Column<string>(type: "text", nullable: false),
                    empty_body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_text_localizations", x => new { x.home_texts_id, x.locale });
                    table.ForeignKey(
                        name: "FK_home_text_localizations_home_texts_home_texts_id",
                        column: x => x.home_texts_id,
                        principalSchema: "content",
                        principalTable: "home_texts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "content",
                table: "home_texts",
                columns: new[] { "id", "version" },
                values: new object[] { new Guid("0192c000-0000-7000-8000-0000c0a70100"), 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "home_text_localizations",
                schema: "content");

            migrationBuilder.DropTable(
                name: "home_texts",
                schema: "content");
        }
    }
}
