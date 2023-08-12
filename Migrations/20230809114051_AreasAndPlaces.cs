using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class AreasAndPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    ExploreStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AreaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    ExploreStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_RegionId",
                table: "Areas",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_AreaId",
                table: "Places",
                column: "AreaId");
            
            migrationBuilder.InsertData("Areas", new string[] { "RegionId", "Name", "Notes", "Links", "ExploreStatus", "Order" },
                new object[,]
                {
                    { 3, "Rovaniemi", "City, regional center.  Population 65,000 (2022), urban area/keskustaajama 54,000 (2020)", "https://www.visitrovaniemi.fi/fi\nhttps://fi.wikipedia.org/wiki/Rovaniemi", 3, 1 },
                    { 3, "Pyhä-Luosto National Park", "National park in Central Lapland, fell chain up to over 500 m high", "https://www.luontoon.fi/pyha-luosto", 3, 2 }
                });
        
            migrationBuilder.InsertData("Places", new string[] { "AreaId", "Name", "Notes", "Links", "ExploreStatus", "Order" },
                new object[,]
                {
                    { 1, "Rovaniemi (urban area/keskustaajama)", "", "", 3, 1 },
                    { 1, "Kaihuanvaara", "Highest point of Rovaniemi, 60 km east of Rovaniemi center via Kt81, nothern bank of Kemijoki, part of Kivalot range.  Over 350 m high.  Hiking trails", "https://www.luontoon.fi/kaihuanvaarajakivalot", 3, 2 },
                    { 1, "Lapin metsämuseo", "Forestry museum in Rovaniemi", "https://www.lapinmetsamuseo.fi/", 0, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "Areas");
        }
    }
}
