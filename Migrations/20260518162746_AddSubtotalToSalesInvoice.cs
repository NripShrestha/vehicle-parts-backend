using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleParts.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSubtotalToSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "SalesInvoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "SalesInvoices");
        }
    }
}
