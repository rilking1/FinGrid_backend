using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrid.Migrations
{
    /// <inheritdoc />
    public partial class AddBankSyncFlagToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBankSyncEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBankSyncEnabled",
                table: "AspNetUsers");
        }
    }
}
