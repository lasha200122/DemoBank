using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class bugFIxOfUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_Users_UserId1",
                table: "Investments");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId1",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_UserId1",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Investments_UserId1",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Investments");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BankingDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficialName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IBAN = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BIC = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankingDetails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankingDetails_UserId",
                table: "BankingDetails",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Accounts");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Investments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId1",
                table: "Invoices",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Investments_UserId1",
                table: "Investments",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_Users_UserId1",
                table: "Investments",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId1",
                table: "Invoices",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
