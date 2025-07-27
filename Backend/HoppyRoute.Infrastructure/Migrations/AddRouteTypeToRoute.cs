using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoppyRoute.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteTypeToRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Routes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Routes");
        }
    }
}
