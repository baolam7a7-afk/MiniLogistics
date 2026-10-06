using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MiniLogistics.DAL.Data;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261002170000_OneShopPerSeller")]
    public partial class OneShopPerSeller : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shops_OwnerUserId",
                table: "shops");

            migrationBuilder.CreateIndex(
                name: "IX_shops_OwnerUserId",
                table: "shops",
                column: "OwnerUserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shops_OwnerUserId",
                table: "shops");

            migrationBuilder.CreateIndex(
                name: "IX_shops_OwnerUserId",
                table: "shops",
                column: "OwnerUserId");
        }
    }
}
