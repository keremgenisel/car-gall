using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Media.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssetKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `0026` D1 · D14 · AC-60, AC-71 — yalnız `media` şeması. Var olan bütün varlıklar
            // fotoğraftır: varsayılan 1 (`MediaAssetKind.Photo`), 0 tanımsız bir tür olurdu.
            migrationBuilder.AddColumn<short>(
                name: "kind",
                schema: "media",
                table: "assets",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kind",
                schema: "media",
                table: "assets");
        }
    }
}
