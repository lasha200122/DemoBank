using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPotentialInvestmentRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PotentialInvestmentRange",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PotentialInvestmentRange",
                table: "Accounts");
        }
    }
}
