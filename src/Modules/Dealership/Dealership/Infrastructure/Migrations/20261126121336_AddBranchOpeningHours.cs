using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Dealership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchOpeningHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `0025` C6 · D13 · P3 — yalnız `dealership` şeması. NULL = program girilmemiş (D4);
            // var olan şubeler programsız başlar.
            migrationBuilder.AddColumn<string>(
                name: "opening_hours",
                schema: "dealership",
                table: "branches",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "opening_hours",
                schema: "dealership",
                table: "branches");
        }
    }
}
