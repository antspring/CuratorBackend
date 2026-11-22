using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenteeProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentee_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_from = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    budget_to = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    goal = table.Column<string>(type: "text", nullable: false),
                    goal_deadline_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_mentee_profiles_catalog_items_availability_id",
                        column: x => x.availability_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profiles_catalog_items_format_id",
                        column: x => x.format_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profiles_catalog_items_goal_deadline_id",
                        column: x => x.goal_deadline_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profiles_catalog_items_skill_level_id",
                        column: x => x.skill_level_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentee_skills",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_skills", x => new { x.mentee_id, x.skill_id });
                    table.ForeignKey(
                        name: "fk_mentee_skills_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mentee_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mentee_tasks",
                columns: table => new
                {
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentee_tasks", x => new { x.mentee_id, x.task_id });
                    table.ForeignKey(
                        name: "fk_mentee_tasks_catalog_items_task_id",
                        column: x => x.task_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentee_tasks_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_availability_id",
                table: "mentee_profiles",
                column: "availability_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_format_id",
                table: "mentee_profiles",
                column: "format_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_goal_deadline_id",
                table: "mentee_profiles",
                column: "goal_deadline_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_skill_level_id",
                table: "mentee_profiles",
                column: "skill_level_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_skills_skill_id",
                table: "mentee_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentee_tasks_task_id",
                table: "mentee_tasks",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentee_skills");

            migrationBuilder.DropTable(
                name: "mentee_tasks");

            migrationBuilder.DropTable(
                name: "mentee_profiles");
        }
    }
}
