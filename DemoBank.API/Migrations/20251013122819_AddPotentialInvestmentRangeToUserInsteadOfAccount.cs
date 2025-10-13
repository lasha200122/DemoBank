using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPotentialInvestmentRangeToUserInsteadOfAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotentialInvestmentRange",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "PotentialInvestmentRange",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotentialInvestmentRange",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "PotentialInvestmentRange",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
