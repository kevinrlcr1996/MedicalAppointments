using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalSignsPhase8d1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SystolicmmHg = table.Column<int>(type: "int", nullable: true),
                    DiastolicmmHg = table.Column<int>(type: "int", nullable: true),
                    HeartRateBpm = table.Column<int>(type: "int", nullable: true),
                    TemperatureC = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TemperatureMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RespiratoryRatePerMin = table.Column<int>(type: "int", nullable: true),
                    SpO2Percent = table.Column<int>(type: "int", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    PainScore0to10 = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectsVitalSignsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Doctors_AuthorDoctorId",
                        column: x => x.AuthorDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalSigns_VitalSigns_CorrectsVitalSignsId",
                        column: x => x.CorrectsVitalSignsId,
                        principalTable: "VitalSigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AppointmentId",
                table: "VitalSigns",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AppointmentId_CreatedAt",
                table: "VitalSigns",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AuthorDoctorId",
                table: "VitalSigns",
                column: "AuthorDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_CorrectsVitalSignsId",
                table: "VitalSigns",
                column: "CorrectsVitalSignsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VitalSigns");
        }
    }
}
