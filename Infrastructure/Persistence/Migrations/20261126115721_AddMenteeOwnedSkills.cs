using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMenteeOwnedSkills : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentee_owned_skills",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_level_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_owned_skills", x => new { x.mentee_id, x.skill_id });
                    table.ForeignKey(
                        name: "fk_mentee_owned_skills_catalog_items_skill_level_id",
                        column: x => x.skill_level_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_owned_skills_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mentee_owned_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentee_owned_skills_skill_id",
                table: "mentee_owned_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_owned_skills_skill_level_id",
                table: "mentee_owned_skills",
                column: "skill_level_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentee_owned_skills");
        }
    }
}
