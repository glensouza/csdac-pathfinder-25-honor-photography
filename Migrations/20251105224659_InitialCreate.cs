using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PathfinderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompositionRuleId = table.Column<int>(type: "integer", nullable: false),
                    CompositionRuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoSubmissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_CompositionRuleId",
                table: "PhotoSubmissions",
                column: "CompositionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_PathfinderName",
                table: "PhotoSubmissions",
                column: "PathfinderName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoSubmissions");
        }
    }
}
