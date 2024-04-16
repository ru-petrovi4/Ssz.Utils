using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class ScenarioResultDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpCompOperators_Users_OperatorId",
                table: "OpCompOperators");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ScenarioResults",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "OperatorId",
                table: "OpCompOperators",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_OpCompOperators_Users_OperatorId",
                table: "OpCompOperators",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpCompOperators_Users_OperatorId",
                table: "OpCompOperators");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "ScenarioResults");

            migrationBuilder.AlterColumn<long>(
                name: "OperatorId",
                table: "OpCompOperators",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OpCompOperators_Users_OperatorId",
                table: "OpCompOperators",
                column: "OperatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
