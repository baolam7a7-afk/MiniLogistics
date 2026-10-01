using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChatUserVoucherQrPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "payment_transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentRef",
                table: "payment_transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrPayload",
                table: "payment_transactions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerUserId = table.Column<long>(type: "bigint", nullable: false),
                    SellerUserId = table.Column<long>(type: "bigint", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversations_shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversations_users_CustomerUserId",
                        column: x => x.CustomerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversations_users_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_vouchers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "available"),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedOrderId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_vouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_vouchers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_vouchers_vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderUserId = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_messages_users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId",
                table: "chat_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SenderUserId",
                table: "chat_messages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_CustomerUserId_SellerUserId_ShopId",
                table: "conversations",
                columns: new[] { "CustomerUserId", "SellerUserId", "ShopId" },
                unique: true,
                filter: "[ShopId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_ProductId",
                table: "conversations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_SellerUserId",
                table: "conversations",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_ShopId",
                table: "conversations",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_UserId_VoucherId",
                table: "user_vouchers",
                columns: new[] { "UserId", "VoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_vouchers_VoucherId",
                table: "user_vouchers",
                column: "VoucherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "user_vouchers");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "PaymentRef",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "QrPayload",
                table: "payment_transactions");
        }
    }
}
