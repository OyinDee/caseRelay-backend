using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    /// <inheritdoc />
    public partial class latest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_OfficerAssignedUserID",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "CaseComment");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OfficerAssignedUserID",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerAssignedId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerAssignedUserID",
                table: "Cases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfficerAssignedId",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfficerAssignedUserID",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CaseComment",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseComment", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_CaseComment_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OfficerAssignedUserID",
                table: "Cases",
                column: "OfficerAssignedUserID");

            migrationBuilder.CreateIndex(
                name: "IX_CaseComment_CaseId",
                table: "CaseComment",
                column: "CaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_OfficerAssignedUserID",
                table: "Cases",
                column: "OfficerAssignedUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
