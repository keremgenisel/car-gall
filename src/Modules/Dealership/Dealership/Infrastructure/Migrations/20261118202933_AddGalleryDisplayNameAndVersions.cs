using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Dealership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryDisplayNameAndVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `0020` C4 — yalnız `dealership` şeması. Görünen ad boş bir haritayla başlar; varsayılan
            // dildeki karşılığını kurulum adımı ticari unvandan doldurur (`DealershipSchema`),
            // çünkü varsayılan dil yapılandırmadır ve migration onu bilemez.
            migrationBuilder.AddColumn<string>(
                name: "display_names",
                schema: "dealership",
                table: "galleries",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "dealership",
                table: "galleries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "dealership",
                table: "branches",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_names",
                schema: "dealership",
                table: "galleries");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "dealership",
                table: "galleries");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "dealership",
                table: "branches");
        }
    }
}
