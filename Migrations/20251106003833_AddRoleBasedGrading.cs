using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleBasedGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradeStatus",
                table: "PhotoSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GradedBy",
                table: "PhotoSubmissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedDate",
                table: "PhotoSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PathfinderEmail",
                table: "PhotoSubmissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreviousSubmissionId",
                table: "PhotoSubmissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionVersion",
                table: "PhotoSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_GradeStatus",
                table: "PhotoSubmissions",
                column: "GradeStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_PathfinderEmail",
                table: "PhotoSubmissions",
                column: "PathfinderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PhotoSubmissions_GradeStatus",
                table: "PhotoSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_PhotoSubmissions_PathfinderEmail",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "GradeStatus",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "GradedBy",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "GradedDate",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "PathfinderEmail",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "PreviousSubmissionId",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmissionVersion",
                table: "PhotoSubmissions");
        }
    }
}
