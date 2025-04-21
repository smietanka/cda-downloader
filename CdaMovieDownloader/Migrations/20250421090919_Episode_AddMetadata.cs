using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdaMovieDownloader.Migrations
{
    /// <inheritdoc />
    public partial class Episode_AddMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Episodes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Episodes");
        }
    }
}
