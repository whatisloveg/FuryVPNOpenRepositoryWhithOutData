using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuryVPN2.Migrations
{
    /// <inheritdoc />
    public partial class AutoSubUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelAutoSubscriptionLink",
                table: "AutoSubscriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelAutoSubscriptionLink",
                table: "AutoSubscriptions");
        }
    }
}
