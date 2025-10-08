using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class CustomRateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_InvestmentRates_CustomRateId",
                table: "Investments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomRateId",
                table: "Investments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_InvestmentRates_CustomRateId",
                table: "Investments",
                column: "CustomRateId",
                principalTable: "InvestmentRates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_InvestmentRates_CustomRateId",
                table: "Investments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomRateId",
                table: "Investments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_InvestmentRates_CustomRateId",
                table: "Investments",
                column: "CustomRateId",
                principalTable: "InvestmentRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
