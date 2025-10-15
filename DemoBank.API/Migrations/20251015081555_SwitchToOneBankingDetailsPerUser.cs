using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToOneBankingDetailsPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankingDetails_UserId",
                table: "BankingDetails");

            migrationBuilder.Sql(@"
                DELETE FROM ""BankingDetails""
                WHERE ""Id"" IN (
                    SELECT ""Id""
                    FROM (
                        SELECT ""Id"",
                               ROW_NUMBER() OVER (PARTITION BY ""UserId"" ORDER BY ""Id"") AS rn
                        FROM ""BankingDetails""
                    ) t
                    WHERE t.rn > 1
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_BankingDetails_UserId",
                table: "BankingDetails",
                column: "UserId",
                unique: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankingDetails_UserId",
                table: "BankingDetails");

            migrationBuilder.CreateIndex(
                name: "IX_BankingDetails_UserId",
                table: "BankingDetails",
                column: "UserId");
        }
    }
}
