using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations;

/// <summary>
/// Adds AiMarketingImageData field for storing AI-generated marketing images from Gemini/Imagen
/// </summary>
public partial class AddGeminiMarketingImage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "AiMarketingImageData",
            table: "PhotoSubmissions",
            type: "bytea",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AiMarketingImageData",
            table: "PhotoSubmissions");
    }
}
