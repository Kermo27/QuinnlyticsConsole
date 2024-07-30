using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuinnlyticsConsole.Migrations
{
    /// <inheritdoc />
    public partial class AddItemExceptionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemExceptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemExceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchDate = table.Column<long>(type: "INTEGER", nullable: false),
                    MatchId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Win = table.Column<bool>(type: "INTEGER", nullable: false),
                    Opponent = table.Column<string>(type: "TEXT", nullable: false),
                    SummonerSpells = table.Column<string>(type: "TEXT", nullable: false),
                    Champion = table.Column<string>(type: "TEXT", nullable: false),
                    GameVersion = table.Column<string>(type: "TEXT", nullable: false),
                    GameDuration = table.Column<long>(type: "INTEGER", nullable: false),
                    RuneDetails = table.Column<string>(type: "TEXT", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    Deaths = table.Column<int>(type: "INTEGER", nullable: false),
                    Assists = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMinionsKilled = table.Column<int>(type: "INTEGER", nullable: false),
                    MinionsPerMinutes = table.Column<float>(type: "REAL", nullable: false),
                    QSkillUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    WSkillUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    ESkillUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    RSkillUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    AllInPings = table.Column<int>(type: "INTEGER", nullable: true),
                    AssistMePings = table.Column<int>(type: "INTEGER", nullable: true),
                    CommandPings = table.Column<int>(type: "INTEGER", nullable: true),
                    EnemyMissingPings = table.Column<int>(type: "INTEGER", nullable: true),
                    EnemyVisionPings = table.Column<int>(type: "INTEGER", nullable: true),
                    GetBackPings = table.Column<int>(type: "INTEGER", nullable: true),
                    NeedVisionPings = table.Column<int>(type: "INTEGER", nullable: true),
                    OnMyWayPings = table.Column<int>(type: "INTEGER", nullable: true),
                    PushPings = table.Column<int>(type: "INTEGER", nullable: true),
                    GoldEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    GoldSpent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVersions");

            migrationBuilder.DropTable(
                name: "ItemExceptions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Matches");
        }
    }
}
