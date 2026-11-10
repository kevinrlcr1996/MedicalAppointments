using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosesPhase8d2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DisplayTerm = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OnsetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectsDiagnosisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Diagnoses_CorrectsDiagnosisId",
                        column: x => x.CorrectsDiagnosisId,
                        principalTable: "Diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Doctors_AuthorDoctorId",
                        column: x => x.AuthorDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_AppointmentId",
                table: "Diagnoses",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_AppointmentId_CreatedAt",
                table: "Diagnoses",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_AuthorDoctorId",
                table: "Diagnoses",
                column: "AuthorDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_Code",
                table: "Diagnoses",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_CorrectsDiagnosisId",
                table: "Diagnoses",
                column: "CorrectsDiagnosisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Diagnoses");
        }
    }
}
