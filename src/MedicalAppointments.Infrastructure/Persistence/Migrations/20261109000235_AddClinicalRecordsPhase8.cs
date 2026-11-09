using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalRecordsPhase8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectsNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_ClinicalNotes_CorrectsNoteId",
                        column: x => x.CorrectsNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Doctors_AuthorDoctorId",
                        column: x => x.AuthorDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_Appointment_Evolution",
                table: "ClinicalNotes",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AppointmentId",
                table: "ClinicalNotes",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AuthorDoctorId",
                table: "ClinicalNotes",
                column: "AuthorDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_CorrectsNoteId",
                table: "ClinicalNotes",
                column: "CorrectsNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalNotes");
        }
    }
}
