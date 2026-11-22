using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM user_profiles;");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "user_profiles");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<short>(
                name: "age",
                table: "user_profiles",
                type: "smallint",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "city_id",
                table: "user_profiles",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "language_id",
                table: "user_profiles",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "photo_file_id",
                table: "user_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_city_id",
                table: "user_profiles",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_language_id",
                table: "user_profiles",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_photo_file_id",
                table: "user_profiles",
                column: "photo_file_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_profiles_catalog_items_language_id",
                table: "user_profiles",
                column: "language_id",
                principalTable: "catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_profiles_cities_city_id",
                table: "user_profiles",
                column: "city_id",
                principalTable: "cities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_profiles_files_photo_file_id",
                table: "user_profiles",
                column: "photo_file_id",
                principalTable: "files",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM user_profiles;");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profiles_catalog_items_language_id",
                table: "user_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profiles_cities_city_id",
                table: "user_profiles");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profiles_files_photo_file_id",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "ix_user_profiles_city_id",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "ix_user_profiles_language_id",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "ix_user_profiles_photo_file_id",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "age",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "city_id",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "language_id",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "photo_file_id",
                table: "user_profiles");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "user_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false);
        }
    }
}
