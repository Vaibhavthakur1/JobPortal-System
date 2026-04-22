using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruiterService.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeAccessExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContactUnlockedAt",
                table: "Pipelines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeAccessExpiresAt",
                table: "Pipelines",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeViewedAt",
                table: "Pipelines",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactUnlockedAt",
                table: "Pipelines");

            migrationBuilder.DropColumn(
                name: "ResumeAccessExpiresAt",
                table: "Pipelines");

            migrationBuilder.DropColumn(
                name: "ResumeViewedAt",
                table: "Pipelines");
        }
    }
}
