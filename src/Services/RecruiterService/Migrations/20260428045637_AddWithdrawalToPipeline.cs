using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruiterService.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalToPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWithdrawn",
                table: "Pipelines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WithdrawnAt",
                table: "Pipelines",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWithdrawn",
                table: "Pipelines");

            migrationBuilder.DropColumn(
                name: "WithdrawnAt",
                table: "Pipelines");
        }
    }
}
