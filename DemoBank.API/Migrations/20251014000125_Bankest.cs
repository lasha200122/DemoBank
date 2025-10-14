using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class Bankest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "YearlyReturn",
                table: "ClientInvestment",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyReturn",
                table: "ClientInvestment",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                table: "ClientInvestment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ClientInvestment",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "ClientInvestment");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ClientInvestment");

            migrationBuilder.AlterColumn<decimal>(
                name: "YearlyReturn",
                table: "ClientInvestment",
                type: "numeric",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyReturn",
                table: "ClientInvestment",
                type: "numeric",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
