using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinGrid.Migrations
{
    /// <inheritdoc />
    public partial class UpdateManualTransactionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_CategoryId",
                table: "ManualTransactions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualTransactions_BudgetCategories_CategoryId",
                table: "ManualTransactions",
                column: "CategoryId",
                principalTable: "BudgetCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManualTransactions_BudgetCategories_CategoryId",
                table: "ManualTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ManualTransactions_CategoryId",
                table: "ManualTransactions");
        }
    }
}
