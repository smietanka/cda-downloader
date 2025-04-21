using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CdaMovieDownloader.Migrations
{
    /// <inheritdoc />
    public partial class Episode_AddIsDownloadColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDownloaded",
                table: "Episodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDownloaded",
                table: "Episodes");
        }
    }
}
