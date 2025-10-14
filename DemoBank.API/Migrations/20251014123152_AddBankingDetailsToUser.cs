using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoBank.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingDetailsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CVV",
                table: "BankingDetails",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "BankingDetails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "BankingDetails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpiryDate",
                table: "BankingDetails",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionHash",
                table: "BankingDetails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "BankingDetails",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "BankingDetails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CVV",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "TransactionHash",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "WalletAddress",
                table: "BankingDetails");
        }
    }
}
