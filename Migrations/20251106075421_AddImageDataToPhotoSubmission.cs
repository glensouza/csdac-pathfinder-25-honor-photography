using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderPhotography.Migrations
{
    /// <inheritdoc />
    public partial class AddImageDataToPhotoSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "PhotoSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "PhotoSubmissions",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "PhotoSubmissions");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "PhotoSubmissions");
        }
    }
}
