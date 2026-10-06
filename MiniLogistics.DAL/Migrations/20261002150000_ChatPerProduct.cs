using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ChatPerProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_CustomerUserId_SellerUserId_ShopId",
                table: "conversations");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_CustomerUserId_ShopId_ProductId",
                table: "conversations",
                columns: new[] { "CustomerUserId", "ShopId", "ProductId" },
                unique: true,
                filter: "[ShopId] IS NOT NULL AND [ProductId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversations_CustomerUserId_ShopId_ProductId",
                table: "conversations");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_CustomerUserId_SellerUserId_ShopId",
                table: "conversations",
                columns: new[] { "CustomerUserId", "SellerUserId", "ShopId" },
                unique: true,
                filter: "[ShopId] IS NOT NULL");
        }
    }
}
