using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleParts.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Parts");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Parts",
                newName: "SellingPrice");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Parts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                table: "Parts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PartName",
                table: "Parts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReorderLevel",
                table: "Parts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "CostPrice",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "PartName",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "Parts");

            migrationBuilder.RenameColumn(
                name: "SellingPrice",
                table: "Parts",
                newName: "Price");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Parts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Parts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
