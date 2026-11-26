using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMenteePersonalDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "about_me",
                table: "mentee_profiles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialty",
                table: "mentee_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "mentee_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "university",
                table: "mentee_profiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "about_me",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "specialty",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "status",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "university",
                table: "mentee_profiles");
        }
    }
}
