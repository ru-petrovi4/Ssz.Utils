using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.NpgsqlMigrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcessModelName = table.Column<string>(type: "text", nullable: false),
                    Enterprise = table.Column<string>(type: "text", nullable: false),
                    Plant = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcessModelId = table.Column<long>(type: "bigint", nullable: false),
                    ScenarioName = table.Column<string>(type: "text", nullable: false),
                    InitialConditionName = table.Column<string>(type: "text", nullable: false),
                    MaxPenalty = table.Column<int>(type: "integer", nullable: false),
                    ScenarioMaxProcessModelTimeSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
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
