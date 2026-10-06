using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MiniLogistics.DAL.Data;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261007013000_AdminSellerChat")]
    public partial class AdminSellerChat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "conversations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "shop");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_platform_shop",
                table: "conversations",
                column: "ShopId",
                unique: true,
                filter: "[Channel] = 'platform' AND [ShopId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_platform_shop",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "conversations");
        }
    }
}
