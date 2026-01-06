using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EntityTestApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "ContactEmail", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "contact@acme.com", "Leading supplier of office products.", "Acme Supplies" },
                    { 2, "info@globaltech.com", "Electronics and IT supplier.", "Global Tech" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
