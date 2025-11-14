using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddAiMarketingImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AiMarketingImageData",
                table: "PhotoSubmissions",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiMarketingImagePrompt",
                table: "PhotoSubmissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiMarketingImageData",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "AiMarketingImagePrompt",
                table: "PhotoSubmissions");
        }
    }
}
