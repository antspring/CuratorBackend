using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class RemoveMenteeQuestionnaireGoalColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mentee_profiles_catalog_items_goal_deadline_id",
                table: "mentee_profiles");

            migrationBuilder.DropIndex(
                name: "ix_mentee_profiles_goal_deadline_id",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "goal",
                table: "mentee_profiles");

            migrationBuilder.DropColumn(
                name: "goal_deadline_id",
                table: "mentee_profiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "goal",
                table: "mentee_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "goal_deadline_id",
                table: "mentee_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE mentee_profiles AS profile
                SET goal = COALESCE(
                        (SELECT goal.text FROM mentee_profile_goals AS goal
                         WHERE goal.mentee_id = profile.user_id ORDER BY goal.sort_order, goal.id LIMIT 1), ''),
                    goal_deadline_id = COALESCE(
                        (SELECT goal.deadline_id FROM mentee_profile_goals AS goal
                         WHERE goal.mentee_id = profile.user_id ORDER BY goal.sort_order, goal.id LIMIT 1),
                        (SELECT id FROM catalog_items WHERE kind = 'goal-deadlines'
                         ORDER BY (code = 'no-deadline') DESC, sort_order, id LIMIT 1)
                    );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "goal",
                table: "mentee_profiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "goal_deadline_id",
                table: "mentee_profiles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_mentee_profiles_goal_deadline_id",
                table: "mentee_profiles",
                column: "goal_deadline_id");

            migrationBuilder.AddForeignKey(
                name: "fk_mentee_profiles_catalog_items_goal_deadline_id",
                table: "mentee_profiles",
                column: "goal_deadline_id",
                principalTable: "catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
