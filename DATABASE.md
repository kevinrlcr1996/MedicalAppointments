# Database

Engine: SQL Server.

ORM: EF Core 10.

Persistence strategy:

- UTC dates.
- RowVersion for concurrency-sensitive tables.
- Indexes for common query paths.
- Foreign keys enforced.

Phase 1 table:

- OutboxMessages

Future core tables:

- Users
- Roles
- Permissions
- RefreshTokens
- Patients
- Doctors
- Specialties
- Organizations
- Clinics
- Rooms
- Resources
- DoctorSchedules
- Appointments
- WaitingListEntries
- WaitingRoomEntries
- MedicalRecords
- Prescriptions
- Payments
- Notifications
- AuditLogs