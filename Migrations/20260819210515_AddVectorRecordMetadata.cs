using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIChatAssistant.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorRecordMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "VectorRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "VectorRecords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "VectorRecords");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "VectorRecords");
        }
    }
}
