using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentor_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    has_free_trial = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentor_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_catalog_items_availability_id",
                        column: x => x.availability_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_catalog_items_experience_id",
                        column: x => x.experience_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_catalog_items_format_id",
                        column: x => x.format_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_catalog_items_position_id",
                        column: x => x.position_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_catalog_items_pricing_model_id",
                        column: x => x.pricing_model_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentor_directions",
                columns: table => new
                {
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentor_directions", x => new { x.mentor_id, x.direction_id });
                    table.ForeignKey(
                        name: "fk_mentor_directions_catalog_items_direction_id",
                        column: x => x.direction_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_directions_mentor_profiles_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "mentor_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentor_skills",
                columns: table => new
                {
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_level_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentor_skills", x => new { x.mentor_id, x.skill_id });
                    table.ForeignKey(
                        name: "fk_mentor_skills_catalog_items_skill_level_id",
                        column: x => x.skill_level_id,
                        principalTable: "catalog_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mentor_skills_mentor_profiles_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "mentor_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mentor_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentor_directions_direction_id",
                table: "mentor_directions",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_profiles_availability_id",
                table: "mentor_profiles",
                column: "availability_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_profiles_experience_id",
                table: "mentor_profiles",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_profiles_format_id",
                table: "mentor_profiles",
                column: "format_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_profiles_position_id",
                table: "mentor_profiles",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_profiles_pricing_model_id",
                table: "mentor_profiles",
                column: "pricing_model_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_skills_skill_id",
                table: "mentor_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_mentor_skills_skill_level_id",
                table: "mentor_skills",
                column: "skill_level_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentor_directions");

            migrationBuilder.DropTable(
                name: "mentor_skills");

            migrationBuilder.DropTable(
                name: "mentor_profiles");
        }
    }
}
