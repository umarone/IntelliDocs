using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIChatAssistant.Migrations
{
    /// <inheritdoc />
    public partial class newfilebased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "VectorRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "VectorRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "VectorRecords");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "VectorRecords");
        }
    }
}
