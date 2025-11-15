using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "CompletionCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PathfinderEmail = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    PathfinderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CertificatePdfData = table.Column<byte[]>(type: "bytea", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotoSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PathfinderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PathfinderEmail = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    CompositionRuleId = table.Column<int>(type: "integer", nullable: false),
                    CompositionRuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ImageData = table.Column<byte[]>(type: "bytea", nullable: true),
                    ImageContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GradeStatus = table.Column<int>(type: "integer", nullable: false),
                    GradedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GradedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmissionVersion = table.Column<int>(type: "integer", nullable: false),
                    PreviousSubmissionId = table.Column<int>(type: "integer", nullable: true),
                    EloRating = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1000.0),
                    AiTitle = table.Column<string>(type: "text", nullable: true),
                    AiDescription = table.Column<string>(type: "text", nullable: true),
                    AiRating = table.Column<int>(type: "integer", nullable: true),
                    AiMarketingHeadline = table.Column<string>(type: "text", nullable: true),
                    AiMarketingCopy = table.Column<string>(type: "text", nullable: true),
                    AiSuggestedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    AiSocialMediaText = table.Column<string>(type: "text", nullable: true),
                    AiMarketingImageData = table.Column<byte[]>(type: "bytea", nullable: true),
                    AiProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    AiProcessingError = table.Column<string>(type: "text", nullable: true),
                    AiProcessingStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AiProcessingCompletedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotoVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VoterEmail = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    WinnerPhotoId = table.Column<int>(type: "integer", nullable: false),
                    LoserPhotoId = table.Column<int>(type: "integer", nullable: false),
                    VoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoVotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletionCertificates_CompletionDate",
                table: "CompletionCertificates",
                column: "CompletionDate");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionCertificates_PathfinderEmail",
                table: "CompletionCertificates",
                column: "PathfinderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_CompositionRuleId",
                table: "PhotoSubmissions",
                column: "CompositionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_GradeStatus",
                table: "PhotoSubmissions",
                column: "GradeStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_PathfinderEmail",
                table: "PhotoSubmissions",
                column: "PathfinderEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoSubmissions_PathfinderName",
                table: "PhotoSubmissions",
                column: "PathfinderName");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVotes_VoterEmail",
                table: "PhotoVotes",
                column: "VoterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoVotes_VoterEmail_WinnerPhotoId_LoserPhotoId",
                table: "PhotoVotes",
                columns: new[] { "VoterEmail", "WinnerPhotoId", "LoserPhotoId" });

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
                name: "CompletionCertificates");

            migrationBuilder.DropTable(
                name: "PhotoSubmissions");

            migrationBuilder.DropTable(
                name: "PhotoVotes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
