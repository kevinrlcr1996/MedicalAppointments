# PROJECT CONTEXT

## Project

Medical Appointments Platform

## Current Phase

Appointments

## Current Version

0.5.0

## Objective

Professional medical appointment management platform.

## Stack

### Backend

- .NET 10
- ASP.NET Core
- EF Core 10
- SQL Server
- JWT
- Refresh Tokens
- FluentValidation
- Serilog
- SignalR
- Outbox

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- React Router
- TanStack Query
- React Hook Form
- Zod

## Restrictions

- DO NOT use MediatR.
- DO NOT use JavaScript.
- DO NOT use CSS files for component styling.
- DO NOT use SCSS/SASS/LESS.
- Tailwind is mandatory.
- REST is the primary API.
- SQL Server is the source of truth.
- SignalR is not the source of truth.
- Hubs contain no business logic.
- External notification providers are optional.
- Prefer Open Source/free solutions.
- No mandatory paid service.

## Current Phase

Phase 5 — Appointments.

## Completed

### Phase 1

- Foundation.

### Phase 2

- Identity.

### Phase 3

- Medical Structure.

### Phase 4

- Scheduling and Availability Engine.

### Phase 5

- Appointment domain aggregate.
- Explicit appointment state machine.
- Appointment statuses:
  - Pending
  - Confirmed
  - CheckedIn
  - InProgress
  - Completed
  - Cancelled
  - NoShow
  - Rescheduled
- Granular appointment permissions:
  - read_all / create_all / update_all / cancel_all / reschedule_all
  - read_assigned / update_assigned / cancel_assigned / reschedule_assigned
  - read_own / create_own / cancel_own / reschedule_own
  - check_in / complete / mark_no_show / override_policy
- Resource-based appointment access service.
- Idempotency infrastructure:
  - IdempotencyKeys table
  - Idempotency-Key header
  - request hash validation
  - replay detection
- Double booking protection:
  - SQL transaction
  - sp_getapplock exclusive booking locks
  - slot revalidation inside lock
  - room overlap validation
  - RowVersion optimistic concurrency
- Appointment booking service:
  - Create
  - Cancel
  - Reschedule
- Appointment workflow service:
  - Confirm
  - Check-in
  - Start
  - Complete
  - No-show
- Audit log foundation:
  - AuditLogs table
  - minimal sanitized audit entries
- Outbox events:
  - Appointments.Created
  - Appointments.Cancelled
  - Appointments.Rescheduled
  - Appointments.StatusChanged
- SignalR events:
  - AppointmentCreated
  - AppointmentCancelled
  - AppointmentRescheduled
  - AppointmentStatusChanged
- Real appointment occupancy integrated into availability engine.
- Frontend:
  - appointments API
  - appointments list page
  - booking page
  - realtime invalidation hook
  - permission-based routes
- Patient profile lookup endpoint:
  - GET /api/v1/patients/me

## Current Task

Local verification of Phase 5.

## Last Completed Task

Phase 5 Appointments implementation.

## Next Task

After Phase 5 verification:

- Phase 6 — Waiting List:
  - WaitingListEntry
  - matching when appointment frees a slot
  - notifications
  - reservation flow from waiting list
- Then Phase 7 — Waiting Room:
  - check-in operational board
  - patient calling
  - in consultation
  - completed

## Architecture Decisions

- Modular monolith.
- Clean Architecture.
- No MediatR.
- Appointment booking uses application services, not handlers.
- Double booking protected by SQL app locks + transaction + revalidation.
- Idempotency stored in SQL Server and validated inside booking lock.
- Reschedule creates new appointment and marks old as Rescheduled.
- Audit logs are sanitized and do not store clinical notes.
- SignalR events are published through Outbox after persistence.
- Availability engine now uses real appointment occupancy.
- Room occupancy is validated on create/reschedule when RoomId is supplied.
- Full room/resource availability engine remains future hardening.

## Security Decisions

- Patient can only access own appointments.
- Doctor can only access assigned appointments unless granted all permissions.
- Reception/Admin can access all appointments according to granted permissions.
- SuperAdmin bypasses permission checks.
- Cancel/reschedule policy can be overridden only with appointments.override_policy.
- Idempotency key reuse with different payload is rejected.
- Booking locks prevent concurrent slot allocation.
- No sensitive clinical data is sent through SignalR payloads.

## Database

Phase 5 tables:

- Appointments
- IdempotencyKeys
- AuditLogs

Existing:

- OutboxMessages
- Identity tables
- Medical structure tables
- Scheduling tables

## API

Base route:

- /api/v1

Appointment endpoints:

- GET /appointments
- GET /appointments/{id}
- POST /appointments
- POST /appointments/{id}/confirm
- POST /appointments/{id}/cancel
- POST /appointments/{id}/reschedule
- POST /appointments/{id}/check-in
- POST /appointments/{id}/start
- POST /appointments/{id}/complete
- POST /appointments/{id}/no-show

Critical headers:

- Idempotency-Key for create/cancel/reschedule

## SignalR

Events:

- AppointmentCreated
- AppointmentCancelled
- AppointmentRescheduled
- AppointmentStatusChanged

Groups:

- doctor:{doctorId}
- clinic:{clinicId}
- patient:{patientId}
- appointment:{appointmentId}
- user:{userId}

## Outbox

Event types:

- Appointments.Created
- Appointments.Cancelled
- Appointments.Rescheduled
- Appointments.StatusChanged

## Known Issues

- Phase 5 has not been compiled/executed in this chat environment.
- Local verification is mandatory before marking Phase 5 complete.
- Frontend cancel/reschedule modals with idempotency keys need to be completed in UI hardening.
- Room/resource full availability engine is partial: room overlap is validated on booking, but availability UI does not yet filter by room.
- Waiting list and waiting room are not implemented yet.
- UserAdminService may still be incomplete from previous phases.

## Technical Debt

- Complete user administration.
- Add full room/resource aware availability engine.
- Add appointment calendar UI.
- Add waiting room board.
- Add notifications consumer for appointment events.
- Add background cleanup for expired idempotency records.
- Add more integration tests for concurrent booking and idempotency.
- Add audit UI.
- Add appointment policies configuration UI.

## Files Changed

Phase 5 introduced Appointments, Idempotency, Audit, booking concurrency, outbox handlers, SignalR events, API contracts/controllers and frontend appointment pages.

## Pending Decisions

- Confirm whether reschedule should preserve original room assignment by default.
- Confirm whether patient can reschedule without staff confirmation.
- Confirm waiting list behavior when a slot freed by cancellation should be offered automatically.

## Notes for Next AI Agent

Before Phase 6:

1. Read PROJECT_CONTEXT.md.
2. Verify Phase 5 builds and tests pass locally.
3. Do not introduce MediatR.
4. Do not bypass booking locks for waiting list reservations.
5. Reuse appointment booking service or extract shared booking kernel.
6. Keep SignalR thin and post-persistence.
7. Keep availability backend authoritative.
8. Protect waiting list claiming with concurrency/idempotency.