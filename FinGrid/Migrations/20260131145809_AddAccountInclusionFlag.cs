using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrid.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountInclusionFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncludedInTotal",
                table: "BankAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncludedInTotal",
                table: "BankAccounts");
        }
    }
}
