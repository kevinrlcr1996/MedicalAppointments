using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentSoftDeletePhase8c : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ClinicalAttachments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ClinicalAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClinicalAttachments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalAttachments_NoteId_IsDeleted",
                table: "ClinicalAttachments",
                columns: new[] { "NoteId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalAttachments_NoteId_IsDeleted",
                table: "ClinicalAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ClinicalAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ClinicalAttachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClinicalAttachments");
        }
    }
}
