using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddMentorshipRequestsAndPairs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentorship_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_months = table.Column<short>(type: "smallint", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentorship_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_mentorship_requests_mentee_profiles_mentee_id",
                        column: x => x.mentee_id,
                        principalTable: "mentee_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mentorship_requests_mentor_profiles_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "mentor_profiles",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mentorships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_months = table.Column<short>(type: "smallint", nullable: false),
                    planned_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_reason_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    end_reason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mentorships", x => x.id);
                    table.ForeignKey(
                        name: "fk_mentorships_mentorship_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "mentorship_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mentorship_requests_mentee_id_created_at_id",
                table: "mentorship_requests",
                columns: new[] { "mentee_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorship_requests_mentee_id_mentor_id",
                table: "mentorship_requests",
                columns: new[] { "mentee_id", "mentor_id" },
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ix_mentorship_requests_mentor_id_created_at_id",
                table: "mentorship_requests",
                columns: new[] { "mentor_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_mentorships_mentee_id_mentor_id",
                table: "mentorships",
                columns: new[] { "mentee_id", "mentor_id" },
                unique: true,
                filter: "status = 'active'");

            migrationBuilder.CreateIndex(
                name: "ix_mentorships_request_id",
                table: "mentorships",
                column: "request_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentorships");

            migrationBuilder.DropTable(
                name: "mentorship_requests");
        }
    }
}
