using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarGallery.Modules.Dealership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchSpecialHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "special_hours",
                schema: "dealership",
                table: "branches",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "special_hours",
                schema: "dealership",
                table: "branches");
        }
    }
}
