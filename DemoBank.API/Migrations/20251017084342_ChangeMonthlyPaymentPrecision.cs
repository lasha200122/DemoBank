using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMonthlyPaymentPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyPayment",
                table: "Loans",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyPayment",
                table: "Loans",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");
        }
    }
}
