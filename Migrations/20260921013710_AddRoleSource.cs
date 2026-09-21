using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodClockTowerScriptEditor.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Team = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ability = table.Column<string>(type: "TEXT", nullable: true),
                    Image = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Edition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Flavor = table.Column<string>(type: "TEXT", nullable: true),
                    Setup = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstNight = table.Column<double>(type: "REAL", nullable: false),
                    OtherNight = table.Column<double>(type: "REAL", nullable: false),
                    FirstNightReminder = table.Column<string>(type: "TEXT", nullable: true),
                    OtherNightReminder = table.Column<string>(type: "TEXT", nullable: true),
                    IsOfficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleSource = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    official_id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    special = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OriginalOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleReminders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReminderText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleReminders_RoleTemplates_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RoleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleReminders_RoleId",
                table: "RoleReminders",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_Edition",
                table: "RoleTemplates",
                column: "Edition");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_IsOfficial",
                table: "RoleTemplates",
                column: "IsOfficial");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_Name",
                table: "RoleTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplates_Team",
                table: "RoleTemplates",
                column: "Team");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleReminders");

            migrationBuilder.DropTable(
                name: "RoleTemplates");
        }
    }
}
