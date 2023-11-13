using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ssz.Dcs.CentralServer.Common.Migrations.SqliteMigrations
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    PersonnelNumber = table.Column<string>(type: "TEXT", nullable: false),
                    WindowsUserName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpCompOperators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpCompUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpCompUserNameToDisplay = table.Column<string>(type: "TEXT", nullable: false),
                    OpCompUserWindowsUserName = table.Column<string>(type: "TEXT", nullable: false),
                    OperatorId = table.Column<long>(type: "INTEGER", nullable: false)
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstructorUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Type = table.Column<byte>(type: "INTEGER", nullable: false),
                    ProcessModelName = table.Column<string>(type: "TEXT", nullable: false),
                    Enterprise = table.Column<string>(type: "TEXT", nullable: false),
                    Plant = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false)
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatorUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessModelingSessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<byte>(type: "INTEGER", nullable: false),
                    Rating = table.Column<byte>(type: "INTEGER", nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: true),
                    Task = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    File = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorSessions_ProcessModelingSessions_ProcessModelingSessionId",
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessModelingSessionId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishDateTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartProcessModelTimeSeconds = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FinishProcessModelTimeSeconds = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ScenarioName = table.Column<string>(type: "TEXT", nullable: false),
                    InitialConditionName = table.Column<string>(type: "TEXT", nullable: false),
                    Penalty = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPenalty = table.Column<int>(type: "INTEGER", nullable: false),
                    ScenarioMaxProcessModelTimeSeconds = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioResults_ProcessModelingSessions_ProcessModelingSessionId",
                        column: x => x.ProcessModelingSessionId,
                        principalTable: "ProcessModelingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSessionScenarioResult",
                columns: table => new
                {
                    OperatorSessionsId = table.Column<long>(type: "INTEGER", nullable: false),
                    ScenarioResultsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessionScenarioResult", x => new { x.OperatorSessionsId, x.ScenarioResultsId });
                    table.ForeignKey(
                        name: "FK_OperatorSessionScenarioResult_OperatorSessions_OperatorSessionsId",
                        column: x => x.OperatorSessionsId,
                        principalTable: "OperatorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OperatorSessionScenarioResult_ScenarioResults_ScenarioResultsId",
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
