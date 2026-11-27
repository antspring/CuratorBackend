using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class RemoveMenteeOverallSkillLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mentee_profiles_catalog_items_skill_level_id",
                table: "mentee_profiles");

            migrationBuilder.DropIndex(
                name: "ix_mentee_profiles_skill_level_id",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "skill_level_id",
                table: "mentee_profiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "skill_level_id",
                table: "mentee_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE mentee_profiles AS profile
                SET skill_level_id = COALESCE(
                    (SELECT owned.skill_level_id FROM mentee_owned_skills AS owned
                     WHERE owned.mentee_id = profile.user_id ORDER BY owned.skill_id LIMIT 1),
                    (SELECT id FROM catalog_items WHERE kind = 'skill-levels' AND code = 'basic')
                );
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "skill_level_id",
                table: "mentee_profiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_skill_level_id",
                table: "mentee_profiles",
                column: "skill_level_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mentee_profiles_catalog_items_skill_level_id",
                table: "mentee_profiles",
                column: "skill_level_id",
                principalTable: "catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
