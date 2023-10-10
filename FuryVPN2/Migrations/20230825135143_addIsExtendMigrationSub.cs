using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuryVPN2.Migrations
{
    /// <inheritdoc />
    public partial class addIsExtendMigrationSub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExtend",
                table: "AutoSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExtend",
                table: "AutoSubscriptions");
        }
    }
}
