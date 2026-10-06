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
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_shops_OwnerUserId' AND object_id = OBJECT_ID('shops'))
                    DROP INDEX [IX_shops_OwnerUserId] ON [shops];

                IF EXISTS (
                    SELECT 1 FROM [shops]
                    GROUP BY [OwnerUserId]
                    HAVING COUNT(*) > 1)
                    CREATE INDEX [IX_shops_OwnerUserId] ON [shops] ([OwnerUserId]);
                ELSE
                    CREATE UNIQUE INDEX [IX_shops_OwnerUserId] ON [shops] ([OwnerUserId]);
                """);
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
