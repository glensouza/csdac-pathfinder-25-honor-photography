using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFieldsToPhotoSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiDescription",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiMarketingCopy",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiMarketingHeadline",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiRating",
                table: "PhotoSubmissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSocialMediaText",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AiSuggestedPrice",
                table: "PhotoSubmissions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiTitle",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiDescription",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiMarketingCopy",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiMarketingHeadline",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiRating",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiSocialMediaText",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiSuggestedPrice",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiTitle",
                table: "PhotoSubmissions");
        }
    }
}
