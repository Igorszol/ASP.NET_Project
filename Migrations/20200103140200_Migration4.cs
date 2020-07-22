using Microsoft.EntityFrameworkCore.Migrations;

namespace ASP.NETProject.Migrations
{
    public partial class Migration4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "JobOffers",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowVersion",
                table: "JobOffers",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "JobOffers");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "JobOffers",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
