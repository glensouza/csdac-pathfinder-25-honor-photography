using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddAiProcessingStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiProcessingCompletedTime",
                table: "PhotoSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiProcessingError",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiProcessingStartTime",
                table: "PhotoSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiProcessingStatus",
                table: "PhotoSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiProcessingCompletedTime",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiProcessingError",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiProcessingStartTime",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiProcessingStatus",
                table: "PhotoSubmissions");
        }
    }
}
