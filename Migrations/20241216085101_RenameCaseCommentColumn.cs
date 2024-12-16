using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseRelayAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameCaseCommentColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "CaseComments",
                newName: "CommentText");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CommentText",
                table: "CaseComments",
                newName: "Comment");
        }
    }
}
