using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.NpgsqlMigrations
{
    /// <inheritdoc />
    public partial class Initialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    PersonnelNumber = table.Column<string>(type: "text", nullable: false),
                    WindowsUserName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpCompOperators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpCompUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpCompUserNameToDisplay = table.Column<string>(type: "text", nullable: false),
                    OpCompUserWindowsUserName = table.Column<string>(type: "text", nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpCompOperators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpCompOperators_Users_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessModelingSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstructorUserId = table.Column<long>(type: "bigint", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    ProcessModelName = table.Column<string>(type: "text", nullable: false),
                    Enterprise = table.Column<string>(type: "text", nullable: false),
                    Plant = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessModelingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessModelingSessions_Users_InstructorUserId",
                        column: x => x.InstructorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperatorUserId = table.Column<long>(type: "bigint", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessModelingSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Rating = table.Column<byte>(type: "smallint", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    Task = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    File = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorSessions_ProcessModelingSessions_ProcessModelingSes~",
                        column: x => x.ProcessModelingSessionId,
                        principalTable: "ProcessModelingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcessModelingSessionId = table.Column<long>(type: "bigint", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartProcessModelTimeSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FinishProcessModelTimeSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ScenarioName = table.Column<string>(type: "text", nullable: false),
                    InitialConditionName = table.Column<string>(type: "text", nullable: false),
                    Penalty = table.Column<int>(type: "integer", nullable: false),
                    MaxPenalty = table.Column<int>(type: "integer", nullable: false),
                    ScenarioMaxProcessModelTimeSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioResults_ProcessModelingSessions_ProcessModelingSess~",
                        column: x => x.ProcessModelingSessionId,
                        principalTable: "ProcessModelingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSessionScenarioResult",
                columns: table => new
                {
                    OperatorSessionsId = table.Column<long>(type: "bigint", nullable: false),
                    ScenarioResultsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessionScenarioResult", x => new { x.OperatorSessionsId, x.ScenarioResultsId });
                    table.ForeignKey(
                        name: "FK_OperatorSessionScenarioResult_OperatorSessions_OperatorSess~",
                        column: x => x.OperatorSessionsId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorSessionScenarioResult_ScenarioResults_ScenarioResul~",
                        column: x => x.ScenarioResultsId,
                        principalTable: "ScenarioResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpCompOperators_OperatorId",
                table: "OpCompOperators",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_OperatorUserId",
                table: "OperatorSessions",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_ProcessModelingSessionId",
                table: "OperatorSessions",
                column: "ProcessModelingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessionScenarioResult_ScenarioResultsId",
                table: "OperatorSessionScenarioResult",
                column: "ScenarioResultsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessModelingSessions_InstructorUserId",
                table: "ProcessModelingSessions",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioResults_ProcessModelingSessionId",
                table: "ScenarioResults",
                column: "ProcessModelingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpCompOperators");

            migrationBuilder.DropTable(
                name: "OperatorSessionScenarioResult");

            migrationBuilder.DropTable(
                name: "OperatorSessions");

            migrationBuilder.DropTable(
                name: "ScenarioResults");

            migrationBuilder.DropTable(
                name: "ProcessModelingSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
