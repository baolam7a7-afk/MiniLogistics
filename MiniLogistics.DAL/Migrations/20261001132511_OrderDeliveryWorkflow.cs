using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniLogistics.DAL.Migrations
{
    /// <inheritdoc />
    public partial class OrderDeliveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConfirmedByUserId",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerConfirmedAt",
                table: "orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_ConfirmedByUserId",
                table: "orders",
                column: "ConfirmedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_ConfirmedByUserId",
                table: "orders",
                column: "ConfirmedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_ConfirmedByUserId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_ConfirmedByUserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmedAt",
                table: "orders");
        }
    }
}
