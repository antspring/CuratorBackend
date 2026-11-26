using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMenteeProfileGoals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentee_profile_goals",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    deadline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_profile_goals", x => new { x.mentee_id, x.id });
                    table.ForeignKey(
                        name: "fk_mentee_profile_goals_catalog_items_deadline_id",
                        column: x => x.deadline_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profile_goals_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentee_profile_goal_directions",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_profile_goal_directions", x => new { x.mentee_id, x.goal_id, x.direction_id });
                    table.ForeignKey(
                        name: "fk_mentee_profile_goal_directions_catalog_items_direction_id",
                        column: x => x.direction_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profile_goal_directions_mentee_profile_goals_mentee_",
                        columns: x => new { x.mentee_id, x.goal_id },
                        principalTable: "mentee_profile_goals",
                        principalColumns: new[] { "mentee_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO mentee_profile_goals (mentee_id, id, text, deadline_id, sort_order)
                SELECT user_id, gen_random_uuid(), goal, goal_deadline_id, 0
                FROM mentee_profiles;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profile_goal_directions_direction_id",
                table: "mentee_profile_goal_directions",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profile_goals_deadline_id",
                table: "mentee_profile_goals",
                column: "deadline_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentee_profile_goal_directions");

            migrationBuilder.DropTable(
                name: "mentee_profile_goals");
        }
    }
}
