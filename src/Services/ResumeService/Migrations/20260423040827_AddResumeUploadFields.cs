using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeService.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeType",
                table: "Resumes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UploadedFileName",
                table: "Resumes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedFilePath",
                table: "Resumes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumeType",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "UploadedFileName",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "UploadedFilePath",
                table: "Resumes");
        }
    }
}
