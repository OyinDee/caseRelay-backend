using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseAndRelatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Cases");
        }
    }
}
