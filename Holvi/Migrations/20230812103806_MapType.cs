using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class MapType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapType",
                table: "Countries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.UpdateData("Countries", "MapType", "", "MapType", "default");
            migrationBuilder.UpdateData("Countries", "Name", "Finland", "MapType", "finland");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapType",
                table: "Countries");
        }
    }
}
