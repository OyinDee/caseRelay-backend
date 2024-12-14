using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    /// <inheritdoc />
    public partial class latestNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Users_OfficerUserID",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OfficerUserID",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerUserID",
                table: "Cases");

            migrationBuilder.RenameColumn(
                name: "CaseID",
                table: "Cases",
                newName: "CaseId");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Cases",
                newName: "ReportedAt");

            migrationBuilder.RenameColumn(
                name: "CaseTitle",
                table: "Cases",
                newName: "Title");

            migrationBuilder.AlterColumn<string>(
                name: "OfficerAssigned",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AssignedOfficerId",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFiles",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OfficerAssignedId",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousOfficerId",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseComment",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "IX_CaseComment_CaseId",
                table: "CaseComment",
                column: "CaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseComment");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EvidenceFiles",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfficerAssignedId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PreviousOfficerId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Cases");

            migrationBuilder.RenameColumn(
                name: "CaseId",
                table: "Cases",
                newName: "CaseID");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Cases",
                newName: "CaseTitle");

            migrationBuilder.RenameColumn(
                name: "ReportedAt",
                table: "Cases",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<int>(
                name: "OfficerAssigned",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Cases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "OfficerUserID",
                table: "Cases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OfficerUserID",
                table: "Cases",
                column: "OfficerUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Users_OfficerUserID",
                table: "Cases",
                column: "OfficerUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
