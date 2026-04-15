using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FanzinePress.Web.Migrations
{
    /// <inheritdoc />
    public partial class StoreImagesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Photos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Photos",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleImageContentType",
                table: "Issues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "TitleImageData",
                table: "Issues",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Ads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Ads",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "TitleImageContentType",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "TitleImageData",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Ads");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Ads");
        }
    }
}
