using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AlesyaTheTraveller.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sorting",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    type = table.Column<string>(unicode: false, maxLength: 20, nullable: false),
                    typeid = table.Column<int>(nullable: false),
                    typename = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sorting", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "utterances",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    respid = table.Column<int>(nullable: false),
                    content = table.Column<string>(unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utterances", x => x.id);
                    table.UniqueConstraint("AK_utterances_respid", x => x.respid);
                });

            migrationBuilder.CreateTable(
                name: "responses",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    word = table.Column<string>(unicode: false, maxLength: 20, nullable: false),
                    respid = table.Column<int>(nullable: false),
                    active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK__responses__activ__52593CB8",
                        column: x => x.respid,
                        principalTable: "utterances",
                        principalColumn: "respid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_responses_respid",
                table: "responses",
                column: "respid");

            migrationBuilder.CreateIndex(
                name: "UC_OneTypeTypeid",
                table: "sorting",
                columns: new[] { "type", "typeid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__utteranc__2C2806A0EA17A916",
                table: "utterances",
                column: "respid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "responses");

            migrationBuilder.DropTable(
                name: "sorting");

            migrationBuilder.DropTable(
                name: "utterances");
        }
    }
}
