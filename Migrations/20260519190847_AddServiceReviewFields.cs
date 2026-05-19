using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleParts.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerID",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentID",
                table: "Reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "Reviews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Parts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentID",
                table: "Reviews",
                column: "AppointmentID");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerID_AppointmentID",
                table: "Reviews",
                columns: new[] { "CustomerID", "AppointmentID" },
                unique: true,
                filter: "\"AppointmentID\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Appointments_AppointmentID",
                table: "Reviews",
                column: "AppointmentID",
                principalTable: "Appointments",
                principalColumn: "AppointmentID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Appointments_AppointmentID",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentID",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerID_AppointmentID",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AppointmentID",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "Reviews");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Parts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerID",
                table: "Reviews",
                column: "CustomerID");
        }
    }
}
