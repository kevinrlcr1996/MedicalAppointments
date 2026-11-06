using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalAppointments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitingListPhase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitingListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreferredFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreferredToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitingListEntries_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListEntries_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WaitingListOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SlotStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SlotEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingListOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitingListOffers_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListOffers_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListOffers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListOffers_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaitingListOffers_WaitingListEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "WaitingListEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_ClinicId",
                table: "WaitingListEntries",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_DoctorId_Status",
                table: "WaitingListEntries",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_Matching",
                table: "WaitingListEntries",
                columns: new[] { "DoctorId", "ClinicId", "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListEntries_PatientId_Status",
                table: "WaitingListEntries",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_ClinicId",
                table: "WaitingListOffers",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_DoctorId",
                table: "WaitingListOffers",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_EntryId_SlotStartUtc",
                table: "WaitingListOffers",
                columns: new[] { "EntryId", "SlotStartUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_PatientId_Status",
                table: "WaitingListOffers",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_RoomId",
                table: "WaitingListOffers",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingListOffers_Status_ExpiresAt",
                table: "WaitingListOffers",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitingListOffers");

            migrationBuilder.DropTable(
                name: "WaitingListEntries");
        }
    }
}
