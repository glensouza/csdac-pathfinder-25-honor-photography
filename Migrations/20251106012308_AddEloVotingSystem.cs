using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddEloVotingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EloRating",
                table: "PhotoSubmissions",
                type: "double precision",
                nullable: false,
                defaultValue: 1000.0);

            migrationBuilder.CreateTable(
                name: "PhotoVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VoterEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WinnerPhotoId = table.Column<int>(type: "integer", nullable: false),
                    LoserPhotoId = table.Column<int>(type: "integer", nullable: false),
                    VoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoVotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVotes_VoterEmail",
                table: "PhotoVotes",
                column: "VoterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVotes_VoterEmail_WinnerPhotoId_LoserPhotoId",
                table: "PhotoVotes",
                columns: new[] { "VoterEmail", "WinnerPhotoId", "LoserPhotoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoVotes");

            migrationBuilder.DropColumn(
                name: "EloRating",
                table: "PhotoSubmissions");
        }
    }
}
