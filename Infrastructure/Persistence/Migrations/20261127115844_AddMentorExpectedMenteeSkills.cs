using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMentorExpectedMenteeSkills : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentor_expected_mentee_skills",
                columns: table => new
                {
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_level_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentor_expected_mentee_skills", x => new { x.mentor_id, x.skill_id });
                    table.ForeignKey(
                        name: "fk_mentor_expected_mentee_skills_catalog_items_skill_level_id",
                        column: x => x.skill_level_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_expected_mentee_skills_mentor_profiles_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "mentor_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mentor_expected_mentee_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentor_expected_mentee_skills_skill_id",
                table: "mentor_expected_mentee_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_expected_mentee_skills_skill_level_id",
                table: "mentor_expected_mentee_skills",
                column: "skill_level_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentor_expected_mentee_skills");
        }
    }
}
