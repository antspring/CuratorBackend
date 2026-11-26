using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMenteeAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentee_availability",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    days = table.Column<int[]>(type: "integer[]", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    formats = table.Column<int[]>(type: "integer[]", nullable: false),
                    meeting_city_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_availability", x => x.mentee_id);
                    table.ForeignKey(
                        name: "fk_mentee_availability_cities_meeting_city_id",
                        column: x => x.meeting_city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_availability_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentee_availability_meeting_city_id",
                table: "mentee_availability",
                column: "meeting_city_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentee_availability");
        }
    }
}
