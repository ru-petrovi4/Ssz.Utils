using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.SqliteMigrations
{
    /// <inheritdoc />
    public partial class ProcessModel_Scenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessModelName = table.Column<string>(type: "TEXT", nullable: false),
                    Enterprise = table.Column<string>(type: "TEXT", nullable: false),
                    Plant = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessModelId = table.Column<long>(type: "INTEGER", nullable: false),
                    ScenarioName = table.Column<string>(type: "TEXT", nullable: false),
                    InitialConditionName = table.Column<string>(type: "TEXT", nullable: false),
                    MaxPenalty = table.Column<int>(type: "INTEGER", nullable: false),
                    ScenarioMaxProcessModelTimeSeconds = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scenarios_ProcessModels_ProcessModelId",
                        column: x => x.ProcessModelId,
                        principalTable: "ProcessModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scenarios_ProcessModelId",
                table: "Scenarios",
                column: "ProcessModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scenarios");

            migrationBuilder.DropTable(
                name: "ProcessModels");
        }
    }
}
