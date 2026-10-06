using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SellerReferralProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "referral_policies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RewardMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SharePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PlatformFeePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinProductCount = table.Column<int>(type: "int", nullable: false),
                    RequireApprovedShop = table.Column<bool>(type: "bit", nullable: false),
                    RequireFirstOrder = table.Column<bool>(type: "bit", nullable: false),
                    HoldingDays = table.Column<int>(type: "int", nullable: false),
                    PayoutMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seller_referrals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReferredUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReferredShopId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SignupIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceHint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FraudFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RewardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QualifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayableAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seller_referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seller_referrals_shops_ReferredShopId",
                        column: x => x.ReferredShopId,
                        principalTable: "shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_seller_referrals_users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_seller_referrals_users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_seller_referrals_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "referral_reward_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerReferralId = table.Column<long>(type: "bigint", nullable: false),
                    ShopWalletTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_reward_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_referral_reward_transactions_seller_referrals_SellerReferralId",
                        column: x => x.SellerReferralId,
                        principalTable: "seller_referrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referral_reward_transactions_shop_wallet_transactions_ShopWalletTransactionId",
                        column: x => x.ShopWalletTransactionId,
                        principalTable: "shop_wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferralCode",
                table: "users",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_referral_reward_transactions_SellerReferralId",
                table: "referral_reward_transactions",
                column: "SellerReferralId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referral_reward_transactions_ShopWalletTransactionId",
                table: "referral_reward_transactions",
                column: "ShopWalletTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_referrals_ReferredShopId",
                table: "seller_referrals",
                column: "ReferredShopId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seller_referrals_ReferredUserId",
                table: "seller_referrals",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_referrals_ReferrerUserId",
                table: "seller_referrals",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_seller_referrals_ReviewedByUserId",
                table: "seller_referrals",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referral_policies");

            migrationBuilder.DropTable(
                name: "referral_reward_transactions");

            migrationBuilder.DropTable(
                name: "seller_referrals");

            migrationBuilder.DropIndex(
                name: "IX_users_ReferralCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "users");
        }
    }
}
