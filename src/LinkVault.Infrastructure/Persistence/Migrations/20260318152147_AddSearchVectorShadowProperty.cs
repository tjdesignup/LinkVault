using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace LinkVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchVectorShadowProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsUsed",
                table: "email_confirmation_tokens",
                newName: "is_used");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "links",
                type: "tsvector",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_used",
                table: "email_confirmation_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "links");

            migrationBuilder.RenameColumn(
                name: "is_used",
                table: "email_confirmation_tokens",
                newName: "IsUsed");

            migrationBuilder.AlterColumn<bool>(
                name: "IsUsed",
                table: "email_confirmation_tokens",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
