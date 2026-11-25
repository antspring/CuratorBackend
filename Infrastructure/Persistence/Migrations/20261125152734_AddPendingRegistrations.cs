using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddPendingRegistrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_registrations",
                columns: table => new
                {
                    normalized_email = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_nonce = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resend_available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivery_due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts_left = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_registrations", x => x.normalized_email);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_registrations_delivery_due_at",
                table: "pending_registrations",
                column: "delivery_due_at",
                filter: "delivery_due_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pending_registrations_expires_at",
                table: "pending_registrations",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_pending_registrations_verification_id",
                table: "pending_registrations",
                column: "verification_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_registrations");
        }
    }
}
