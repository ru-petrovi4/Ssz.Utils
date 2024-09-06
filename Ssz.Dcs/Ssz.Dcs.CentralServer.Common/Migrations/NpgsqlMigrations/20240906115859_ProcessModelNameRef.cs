using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.NpgsqlMigrations
{
    /// <inheritdoc />
    public partial class ProcessModelNameRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enterprise",
                table: "ProcessModelingSessions");

            migrationBuilder.DropColumn(
                name: "Plant",
                table: "ProcessModelingSessions");

            migrationBuilder.DropColumn(
                name: "ProcessModelName",
                table: "ProcessModelingSessions");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ProcessModelingSessions");

            migrationBuilder.RenameColumn(
                name: "WindowsUserName",
                table: "Users",
                newName: "ProcessModelNames");

            migrationBuilder.AddColumn<string>(
                name: "DomainUserName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ProcessModelId",
                table: "ProcessModelingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessModelingSessions_ProcessModelId",
                table: "ProcessModelingSessions",
                column: "ProcessModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessModelingSessions_ProcessModels_ProcessModelId",
                table: "ProcessModelingSessions",
                column: "ProcessModelId",
                principalTable: "ProcessModels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessModelingSessions_ProcessModels_ProcessModelId",
                table: "ProcessModelingSessions");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ProcessModelingSessions_ProcessModelId",
                table: "ProcessModelingSessions");

            migrationBuilder.DropColumn(
                name: "DomainUserName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProcessModelId",
                table: "ProcessModelingSessions");

            migrationBuilder.RenameColumn(
                name: "ProcessModelNames",
                table: "Users",
                newName: "WindowsUserName");

            migrationBuilder.AddColumn<string>(
                name: "Enterprise",
                table: "ProcessModelingSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Plant",
                table: "ProcessModelingSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessModelName",
                table: "ProcessModelingSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ProcessModelingSessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
