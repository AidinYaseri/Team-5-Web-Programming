using Animal.Data.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Animal.Data.Migrations
{
    [DbContext(typeof(AnimalDbContext))]
    [Migration("20261104110000_AddUserExternalId")]
    public partial class AddUserExternalId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql("UPDATE [Users] SET [ExternalId] = CONCAT(N'legacy:', [Id]) WHERE [ExternalId] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Users_ExternalId", table: "Users");
            migrationBuilder.DropColumn(name: "ExternalId", table: "Users");
        }
    }
}
