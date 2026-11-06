# PROJECT CONTEXT

## Project

Medical Appointments Platform

## Current Phase

Waiting List

## Current Version

0.6.0

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

Phase 6 — Waiting List.

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
- Patient self-service profile (onboarding):
  - POST /api/v1/patients/me
  - PUT /api/v1/patients/me
  - patients.manage_own permission for Patient role
  - frontend PatientOnboardingForm gate in booking and appointments pages
- Locally verified by developer (booking vertical closed end-to-end for Patient).

### Phase 6

- Waiting list domain aggregates:
  - WaitingListEntry (statuses: Waiting / Fulfilled / Cancelled)
  - WaitingListOffer (statuses: Pending / Accepted / Declined / Expired)
  - WaitingListPriority (Low / Normal / High / Urgent)
  - preferred window (PreferredFromUtc / PreferredToUtc) on entries
  - offer TTL (ExpiresAt) on offers
- Anti-loop guarantee:
  - unique constraint (EntryId, SlotStartUtc) on WaitingListOffers
  - a candidate receives at most one offer per freed slot
  - expire/decline re-trigger matching to the NEXT candidate, never the same slot to the same entry twice
- Slot-freed trigger (event-driven, not inline in write path):
  - cancelling, marking no-show, or rescheduling a FUTURE appointment enqueues WaitingList.SlotFreed via Outbox
  - past slots are ignored (EnqueueSlotFreedIfFuture guard)
  - rescheduling the original appointment frees its original slot for re-matching
- Matching service (application service, invoked by Outbox handler after commit):
  - revalidates availability before offering (schedule + exceptions + blockouts + active appointments)
  - selects highest-priority compatible waiting candidate (priority desc, then created asc)
  - clinic-null entry matches any clinic of the doctor
  - runs inside booking app-lock for the slot to avoid races with manual booking
- Reservation from waiting list REUSES the Phase 5 booking kernel:
  - accepting an offer calls IAppointmentBookingService.CreateAsync
  - stable idempotency key wl-offer:{offerId}
  - inherits app-locks, slot revalidation, double-booking protection, audit
  - conflict on accept expires the offer and re-triggers matching (no duplicate appointment)
  - on success: offer Accepted, entry Fulfilled, appointment created
- Decline re-triggers matching for the same slot.
- Expiration worker (BackgroundService):
  - polls pending offers past ExpiresAt
  - marks them Expired and re-enqueues SlotFreed for re-matching
- Outbox events:
  - WaitingList.OfferCreated
  - WaitingList.Updated
  - WaitingList.SlotFreed (internal trigger, not a SignalR publish)
- SignalR events:
  - WaitingListOfferCreated
  - WaitingListUpdated
  - published to patient:{id}, doctor:{id}, clinic:{id}, user:{id} groups
- Granular waiting list permissions:
  - waiting_list.read_own / manage_own (Patient)
  - waiting_list.read_assigned (Doctor)
  - waiting_list.read_all / manage_all (Receptionist / Admin)
- Resource-based waiting list access service (entry read filter + offer scoping).
- Configuration:
  - WaitingList:OfferTtl (default 00:15:00)
  - WaitingList:ExpirationPollingSeconds (default 30)
  - WaitingList:ExpirationBatchSize (default 50)
- Frontend:
  - waiting-list API service
  - useWaitingListRealtime hook (invalidates waiting-list queries on SignalR events)
  - WaitingListPage: pending offers with live countdown + accept/reject, waiting entries with cancel
  - "Apuntarme a la lista de espera" button in BookAppointmentPage when no slots (default 14-day window)
  - permission-based menu item and route
- Tests:
  - WaitingListEntry domain rules (MatchesSlot window/doctor, cancel-from-fulfilled)
  - WaitingListOffer domain rules (accept-after-expiry)
  - WaitingList architecture layer tests (no outer-layer / EF / MediatR deps)
- Migration: AddWaitingListPhase6 (WaitingListEntries, WaitingListOffers).

## Current Task

Local verification of Phase 6.

## Last Completed Task

Phase 6 Waiting List implementation.

## Next Task

After Phase 6 verification:

- Phase 7 — Waiting Room:
  - operational check-in board
  - patient calling (next / previous)
  - in-consultation / completed tracking
  - realtime board updates
  - MUST reuse appointment state machine CheckedIn / InProgress / Completed transitions; do not fork status logic
- Then Phase 8+ (clinical records, prescriptions, documents, payments, reports).

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
- Waiting list matching is event-driven via Outbox, never inline in the appointment write path.
- Offers are time-boxed; acceptance is the only path that creates an appointment, always through the booking kernel.
- No automatic blind booking: a slot freed is OFFERED, not silently assigned (default mode).
- Expiration and decline both re-enter matching; the unique (EntryId, SlotStartUtc) guarantees termination.
- Accept consistency (appointment + offer + entry) is eventually consistent via idempotent replay, not a single saga transaction.
- Dropdowns for doctors/clinics are served by lightweight options endpoints, not paginated lists, to avoid collection-loading pitfalls and over-exposure.

## Security Decisions

- Patient can only access own appointments.
- Doctor can only access assigned appointments unless granted all permissions.
- Reception/Admin can access all appointments according to granted permissions.
- SuperAdmin bypasses permission checks.
- Cancel/reschedule policy can be overridden only with appointments.override_policy.
- Idempotency key reuse with different payload is rejected.
- Booking locks prevent concurrent slot allocation.
- No sensitive clinical data is sent through SignalR payloads.
- Patient sees/manages only own waiting list entries and offers (read_own / manage_own).
- Doctor sees only assigned waiting list (read_assigned).
- Staff manages waiting list globally per read_all / manage_all.
- Accepting an offer is authorized by the booking kernel's own permission checks (create_own / create_all); waiting list does not bypass appointment authorization.
- Clinics are derived from the selected doctor's DoctorClinic adscriptions, never exposed via global clinics.read to patients.

## Database

Phase 6 tables:

- WaitingListEntries
- WaitingListOffers

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

Waiting list endpoints:

- GET /waiting-list/entries
- GET /waiting-list/entries/{id}
- POST /waiting-list/entries
- PUT /waiting-list/entries/{id}
- DELETE /waiting-list/entries/{id}
- GET /waiting-list/offers
- POST /waiting-list/offers/{id}/respond

Doctor options endpoint:

- GET /doctors/options (lightweight, includes clinics adscription)

Critical headers:

- Idempotency-Key for appointment create/cancel/reschedule

## SignalR

Events:

- AppointmentCreated
- AppointmentCancelled
- AppointmentRescheduled
- AppointmentStatusChanged
- WaitingListOfferCreated
- WaitingListUpdated

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
- WaitingList.OfferCreated
- WaitingList.Updated
- WaitingList.SlotFreed

## Notifications

No external notification channels implemented in Phase 6.

Offer notifications are SignalR-only (in-app realtime). Email/SMS offer notifications remain technical debt.

## Known Issues

- Phase 6 has not been compiled/executed in this chat environment.
- Local verification is mandatory before marking Phase 6 complete.
- Phase 5 was locally verified by the developer (booking vertical closed for Patient).
- Frontend cancel/reschedule modals with idempotency keys still need completion (carried from Phase 5).
- Room/resource full availability engine is partial: room overlap validated on booking, availability UI does not filter by room.
- Accept flow is eventually consistent (idempotent replay), not atomic; harden with saga/outbox-saga if strict atomicity is required.
- "Apuntarme" window defaults to 14 days; fine-grained window picker is debt.
- Matching runs inside the Outbox processing pipeline; move to a dedicated worker/queue if matching becomes heavy.
- UserAdminService may still be incomplete from previous phases.

## Technical Debt

- Complete user administration.
- Add full room/resource aware availability engine.
- Add appointment calendar UI.
- Add waiting room board (Phase 7).
- Add notifications consumer (email/SMS) for waiting list offers and appointment events.
- Add background cleanup for expired idempotency records.
- Add more integration tests for concurrent booking, idempotency, and waiting list offer/accept/conflict/re-match loop.
- Add audit UI.
- Add appointment policies configuration UI.
- Add saga/transactional consistency for accept (appointment + offer + entry) if business demands.
- Add window picker UI in booking flow.
- Add staff UI to manually prioritize/promote waiting list entries.
- Move matching to dedicated worker if Outbox latency grows.
- Validate permission/role stamps against DB per request (or short-lived tokens) to avoid stale-token authorization gaps.

## Files Changed

Phase 6 introduced WaitingList domain (entries, offers, enums, priority), application services (waiting list service, access service, matching service, event notifier, options, validators, store interfaces), Outbox handlers (OfferCreated, Updated, SlotFreed trigger), infrastructure (EF configs, stores, expiration worker, DbSets, DI), contracts, API controller, permissions/policies/seeder additions, slot-freed triggers in appointment booking/workflow services, frontend (waiting-list API, realtime hook, WaitingListPage, "Apuntarme" in booking, menu/route), unit and architecture tests, and migration AddWaitingListPhase6.

## Pending Decisions

- Confirm whether waiting list should stay in OFFER mode (default, patient consent on exact slot) or switch to AUTOMATIC booking mode (top candidate reserved directly on slot free). Automatic mode swap is prepared (replace offer creation with direct kernel call + stable key wl-auto:{entryId}:{slotStart}).
- Confirm whether reschedule of the original appointment should keep freeing its original slot for re-matching (currently YES via EnqueueSlotFreedIfFuture including Rescheduled).
- Confirm whether a waiting entry with clinic-null should match any clinic of the doctor (currently YES).
- Confirm whether accept should be atomic with offer/entry state (currently eventual via idempotent replay).
- Confirm whether patient can reschedule without staff confirmation (carried from Phase 5).
- Confirm whether reschedule should preserve original room assignment by default (carried from Phase 5).

## Notes for Next AI Agent

Before Phase 7:

1. Read PROJECT_CONTEXT.md.
2. Verify Phase 6 builds and tests pass locally, and that the offer -> accept -> conflict -> re-match -> expire loop works against real SQL Server.
3. Do not introduce MediatR.
4. Do not create appointments outside the booking kernel (waiting list accept reuses it; Phase 7 check-in must reuse appointment state machine transitions, not fork status logic).
5. Keep matching event-driven (Outbox), not inline in the write path.
6. Keep SignalR thin and post-persistence.
7. Keep availability backend authoritative.
8. Protect waiting list claiming with the same concurrency/idempotency as manual booking.
9. Waiting Room (Phase 7) operates on already-checked-in appointments; do not reintroduce check-in logic there, call the workflow service.
10. Do not expose global clinics/patients lists to Patient; derive options from adscription/ownership as in Phase 6.