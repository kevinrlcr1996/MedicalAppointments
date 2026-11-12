# API

Base route:

- /api/v1

Authentication:

- JWT bearer.

Phase 1 endpoints:

- POST /api/v1/dev/token
- POST /api/v1/foundation/realtime/test
- GET /health/live
- GET /health/ready

Conventions:

- ProblemDetails for errors.
- Pagination contract for collections.
- DTOs separate from domain entities.
- Idempotency-Key header planned for critical operations.

Appointment reports:

- GET /api/v1/reports/appointments?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- GET /api/v1/reports/appointments/export?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD
- Requires the reports.read permission.
- Dates are inclusive UTC calendar dates; periods are limited to 366 days.
- Includes totals by status, UTC day, doctor, specialty, cancellations, no-shows, and patients created in the selected period.
- Includes agenda occupancy as reserved appointment minutes divided by available scheduled minutes (after holidays, schedule exceptions and doctor blockouts; before appointment occupancy). Cancelled/rescheduled appointments do not reserve time; no-shows do.
- Includes observable appointment demand as persisted appointment records created plus waiting-list registrations created in the selected period. Accepted waiting-list bookings are deduplicated from the appointment count. Unpersisted/failed booking attempts are not measurable.
- The export route returns a UTF-8 CSV with the same aggregates and neutralizes formula-like text values for spreadsheet safety.
- Cancellations are attributed to the appointment's scheduled date. An appointment is counted under each specialty assigned to its doctor.
- Revenue is not available until its financial definition is implemented.

Prescription dispensing:

- POST /api/v1/clinical-records/prescriptions/{id}/dispense requires `prescriptions.dispense`.
- POST /api/v1/clinical-records/prescriptions/{id}/expire requires `prescriptions.expire`.
- The seeded `Pharmacist` role receives prescription read, dispense and expire permissions plus patient lookup. Operational reads return only issued or dispensed prescriptions; authoring, issuing, correction, renewal, cancellation and clinical document access remain separately permissioned.

Clinical attachment scanning:

- In Docker Compose, the API sends uploaded attachments to the private ClamAV service on the Compose network; its host port is bound to loopback only for local verification.
- When `Attachments:Scanner:Enabled` is true, an unavailable or non-responsive scanner rejects the upload with HTTP 503 and records a sanitized audit entry. A positive malware verdict remains HTTP 409 and never persists attachment metadata.
- `Skipped` is accepted only when scanning is explicitly disabled. The local Development settings still disable scanning; configure ClamAV separately before treating local uploads as scanned.
