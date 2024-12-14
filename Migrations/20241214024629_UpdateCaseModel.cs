using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficerAssigned",
                table: "Cases");

            migrationBuilder.AddColumn<int>(
                name: "OfficerAssignedUserID",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OfficerAssignedUserID",
                table: "Cases",
                column: "OfficerAssignedUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_OfficerAssignedUserID",
                table: "Cases",
                column: "OfficerAssignedUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_OfficerAssignedUserID",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OfficerAssignedUserID",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerAssignedUserID",
                table: "Cases");

            migrationBuilder.AddColumn<string>(
                name: "OfficerAssigned",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
