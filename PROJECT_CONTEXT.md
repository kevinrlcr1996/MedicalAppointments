# PROJECT CONTEXT

## Project

Medical Appointments Platform

## Current Phase

Waiting Room

## Current Version

0.7.0

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

Phase 7 — Waiting Room.

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
- Resource-based appointment access service (authorizes against the DOMAIN entity Appointment, not a DTO).
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
  - appointments list page (with cancel modal + idempotency key)
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
- LOCALLY VERIFIED by developer (booking vertical closed end-to-end for Patient).

### Phase 6

- Waiting list domain aggregates:
  - WaitingListEntry (statuses: Waiting / Fulfilled / Cancelled)
  - WaitingListOffer (statuses: Pending / Accepted / Declined / Expired)
  - WaitingListPriority (Low / Normal / High / Urgent)
  - preferred window (PreferredFromUtc / PreferredToUtc) on entries
  - offer TTL (ExpiresAt) on offers
  - navigation properties (Patient, Doctor, Clinic?) declared INSIDE the entity file from the first draft
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
- LOCALLY VERIFIED by developer (offer -> accept -> conflict -> re-match -> expire loop exercised against real SQL Server).

### Phase 7

- WaitingRoomCall aggregate (statuses: Queued / Called / InConsultation / Skipped / Completed) with SequenceNumber, RoomId, RecallCount, CalledAt/AnsweredAt/SkippedAt/CompletedAt timestamps, Note.
- Navigation properties (Appointment, Doctor, Clinic, Room?) declared INSIDE the entity file from the first draft (lesson from Phase 6 CS1061).
- Unique constraints:
  - (AppointmentId): one active call per appointment
  - (DoctorId, ClinicId, LocalDate, SequenceNumber): no duplicate turns per queue/day
- The waiting room does NOT mutate Appointment.Status directly. All appointment advances go through the Phase 5 IAppointmentWorkflowService:
  - Answer -> workflow.StartAsync (CheckedIn -> InProgress)
  - Complete -> workflow.CompleteAsync (InProgress -> Completed)
  - Verified by contrast: moving a citation from the Citas page (Phase 5 buttons) leaves the call orphaned until lazy reconciliation; moving it from the room advances citation + call together with one click.
- Calling is orthogonal to consultation:
  - CallNext sets the call to Called and publishes PatientCalled (audio/display) WITHOUT changing the appointment (stays CheckedIn)
  - Answer moves the call to InConsultation AND calls workflow.StartAsync
  - Skip returns the call to the queue without touching the appointment
  - Recall re-calls a Called OR Skipped patient (RecallCount++, status -> Called)
- CheckInToRoom requires the appointment to already be CheckedIn (else 409). Physical check-in (Confirmed -> CheckedIn) remains a Phase 5 action invoked from the Citas page.
- Lazy reconciliation call <- appointment ( architectural decision, see below ):
  - CallNext iterates the queue in turn order instead of failing on the first orphan
  - a call whose appointment moved by another means (e.g. started/completed/cancelled from the Citas page) is reconciled to its real state using only legal chained transitions of the call state machine (Queued -> Called -> InConsultation/Completed/Skipped), respecting the entity guards
  - the first candidate whose appointment is still CheckedIn is called
  - the "no patients waiting" 409 is thrown OUTSIDE the transaction lambda so committed reconciliations are not rolled back
- Unified recall ( architectural decision, see below ):
  - MarkRecalled accepts Called or Skipped and always leaves the call in Called with RecallCount++
  - aligns UI (Recordar button on the Skipped column) <-> service <-> domain; previously the service demanded Called strictly and broke the Skipped case the UI exposed
- Concurrency:
  - CallNext and CheckInToRoom run inside IBookingCoordinator.ExecuteInTransactionAsync with sp_getapplock on waitingroom:doctor:{id}:clinic:{id}:date:{yyyy-MM-dd}
  - sequence assigned under lock; verified that two simultaneous CallNext produce two distinct consecutive turns, never a duplicate
- Authorization delegated, not duplicated:
  - IWaitingRoomAccessService wraps IAppointmentAccessService and authorizes against the DOMAIN entity Appointment (not a DTO)
  - operating a call = permission to Start/Complete/MarkNoShow THAT appointment (doctor on assigned, staff via waiting_room.manage)
  - viewing the board = waiting_room.read or being the assigned doctor
  - Patient has no waiting room access
  - Policy WaitingRoomOperate = manage || check_in || complete || mark_no_show
- Outbox events:
  - WaitingRoom.Updated
  - WaitingRoom.PatientCalled
  - WaitingRoom.PatientRecalled
- SignalR events:
  - WaitingRoomUpdated
  - PatientCalled
  - PatientRecalled
  - published to doctor:{id}, clinic:{id}, room:{id}, user:{id} groups
- New realtime group room:{roomId}; NotificationHub JoinRoomGroup / LeaveRoomGroup validated server-side (waiting_room.read/manage or rooms.read).
- Configuration reuses WaitingList/Outbox polling settings; no new appsettings section required for Phase 7.
- Frontend:
  - waitingRoom API service (board, check-in, call-next, answer, skip, recall, complete)
  - useWaitingRoomRealtime hook (invalidates waiting-room + appointments queries on SignalR events)
  - WaitingRoomBoardPage: live columns Check-in pendiente / En espera / Llamado / En consulta / Saltados / Completados; "Entrar en sala" button on checked-in appointments not yet in a call; call-next with room selector; answer/skip/recall/complete per card; realtime invalidation
  - WaitingRoomDisplayPage (TV): big last-call block + chime via WebAudio, queued/in-consultation lists, SignalR + 15s polling fallback, MEDICAL + CLINIC + DATE selectors so the TV can be aligned with the queue being operated, custom white date icon (appearance-none + CalendarDays + showPicker) because the native date widget renders grey on the dark background
  - permission-based menu items and routes
- Tests:
  - WaitingRoomCall domain transitions (Queued -> Called -> InConsultation -> Completed)
  - Recall from Skipped returns to Called with RecallCount++ (protects the unified-recall fix)
  - Recall throws from Queued or Completed
  - MarkInConsultation throws when not Called
  - WaitingRoom architecture layer tests (no outer-layer / EF / MediatR deps)
- Migration: AddWaitingRoomPhase7 (WaitingRoomCalls).
- LOCALLY VERIFIED by developer (calling realtime, delegation to Phase 5 workflow, skip/recall not moving the appointment, concurrent call-next without duplicate turn, role isolation).

## Current Task

Local verification of Phase 7 complete. Preparing Phase 8.

## Last Completed Task

Phase 7 Waiting Room implementation and local verification.

## Next Task

After Phase 7 verification:

- Phase 8 — Clinical Records:
  - encounter notes tied to appointments in InProgress / Completed
  - structured fields + free text
  - attachments foundation (upload + metadata, no clinical content in SignalR)
  - MUST reference Appointment / Encounter, NOT duplicate waiting room state
  - MUST NOT mutate Appointment.Status; notes are a separate aggregate referencing the appointment
- Then Phase 9+ (prescriptions, documents, payments, reports, telemedicine).

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
- Availability engine uses real appointment occupancy.
- Room occupancy is validated on create/reschedule when RoomId is supplied.
- Full room/resource availability engine remains future hardening.
- Waiting list matching is event-driven via Outbox, never inline in the appointment write path.
- Offers are time-boxed; acceptance is the only path that creates an appointment, always through the booking kernel.
- No automatic blind booking: a slot freed is OFFERED, not silently assigned (default mode).
- Expiration and decline both re-enter matching; the unique (EntryId, SlotStartUtc) guarantees termination.
- Accept consistency (appointment + offer + entry) is eventually consistent via idempotent replay, not a single saga transaction.
- Waiting Room is a coordination/board layer over appointments, NOT a parallel state machine.
- Appointment state transitions are owned exclusively by the Phase 5 workflow service; the waiting room orchestrates calls to it.
- Calling (PatientCalled) is an operational event distinct from consultation start; supports no-show/skip/recall workflows.
- Turn sequence is per (doctor, clinic, local date) and assigned under app-lock.
- One active WaitingRoomCall per appointment (unique AppointmentId).
- Waiting room authorization delegates to IAppointmentAccessService to avoid duplicating the permission matrix.
- LAZY RECONCILIATION call <- appointment: the appointment is the source of truth and the waiting room does not know about it; when a call's appointment moved by another means (e.g. started/completed/cancelled from the Citas page), CallNext reconciles the call to its real state using only legal chained transitions, then continues to the next candidate. This keeps the two layers decoupled while the board stays coherent. The "no patients waiting" 409 is raised outside the transaction so committed reconciliations survive.
- UNIFIED RECALL: recalling a Called patient (re-announce) and recalling a Skipped patient (re-call) are the same operational act and both leave the call in Called with RecallCount++. The service guard, the domain transition and the UI must agree on this; they were misaligned in the first Phase 7 draft and were fixed during verification.
- NAVIGATION PROPERTIES LIVE INSIDE THE ENTITY FILE from the first draft, never as a footnote on the EF configuration. Every HasOne(x => x.Nav).WithMany().HasForeignKey(x => x.NavId) in a configuration requires the Nav property to exist on the entity in the same delivery. This was the root cause of repeated CS1061 cycles in Phases 6 and 7.
- DROPDOWNS USE CONFIRMED ENDPOINTS ONLY: doctor list via GET /api/v1/doctors (paginated, exists since Phase 3, always returns id/firstName/lastName); clinic list for a chosen doctor via GET /api/v1/doctors/{id} (GetById, which loads Clinics via GetTrackedWithRelationsAsync since Phase 3). The previously assumed GET /api/v1/doctors/options DOES NOT EXIST in the backend and caused 404s during verification; it was reverted. Do not reintroduce an options endpoint unless it is actually implemented and registered.
- CROSS-LAYER CALLS USE VERIFIED SIGNATURES ONLY: before delivering code that calls a method on another layer, the exact signature must be confirmed against the existing code (or requested from the developer). Assumed symmetry between stores/hubs caused CS1061 (RoomStore has no ExistsAsync; HubCallerContext has no CallAborted) and CS1503 (appointment access authorizes against the entity, not a DTO).
- EVERY OPERATIONAL ACT IS CLOSED WITH A UI / SERVICE / DOMAIN TABLE in the same draft: if the UI exposes a button, the service and the state machine must already have the corresponding transition written in the same delivery. Misalignment between these three layers caused the recall 400 and the orphaned-call 409.

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
- Patient cannot view or operate any waiting room.
- Doctor can view/operate only their own queue (delegated via assigned appointment permissions, authorizing against the entity).
- Reception/Admin operate the waiting room globally via waiting_room.manage.
- Room group join requires waiting_room.read/manage or rooms.read, validated server-side in the hub.
- No clinical data in waiting room SignalR payloads (names + turn + room only).
- The onboarding gate in the booking page applies ONLY to the pure Patient role (isPatientOnly = isPatient && !isStaff); staff with bypass must see the patient picker, not the self-service profile form. This was a gate bug fixed during verification.

## Database

Phase 7 tables:

- WaitingRoomCalls

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

Waiting room endpoints:

- GET /waiting-room/board?doctorId&clinicId&localDate
- POST /waiting-room/check-in?appointmentId
- POST /waiting-room/call-next
- POST /waiting-room/{callId}/answer
- POST /waiting-room/{callId}/skip
- POST /waiting-room/{callId}/recall
- POST /waiting-room/{callId}/complete

Doctor endpoints used by dropdowns:

- GET /api/v1/doctors (paginated; doctor list)
- GET /api/v1/doctors/{id} (GetById; includes Clinics adscription)

NOTE: GET /api/v1/doctors/options is NOT implemented and must not be referenced by the frontend.

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
- WaitingRoomUpdated
- PatientCalled
- PatientRecalled

Groups:

- doctor:{doctorId}
- clinic:{clinicId}
- patient:{patientId}
- appointment:{appointmentId}
- user:{userId}
- room:{roomId}

## Outbox

Event types:

- Appointments.Created
- Appointments.Cancelled
- Appointments.Rescheduled
- Appointments.StatusChanged
- WaitingList.OfferCreated
- WaitingList.Updated
- WaitingList.SlotFreed
- WaitingRoom.Updated
- WaitingRoom.PatientCalled
- WaitingRoom.PatientRecalled

## Notifications

No external notification channels implemented through Phase 7.

Offer and call notifications are SignalR-only (in-app realtime). Email/SMS notifications for offers and calls remain technical debt.

## Known Issues

- Phase 7 was locally verified by the developer; the lazy-reconciliation and unified-recall fixes were applied during verification and are now part of the design.
- The board can still SHOW an orphaned call in "En espera" until "Llamar siguiente" is pressed (which reconciles and persists it). A read-only reconciliation in GetBoardAsync (derive displayed state from the appointment without persisting) is NOT implemented; see Technical Debt.
- During verification, moving an appointment from the Citas page (Phase 5 buttons) creates an orphaned call until the next CallNext reconciles it. Operating consultations from the room keeps the queue clean; this is a discipline note, not a bug.
- The TV display requires an authenticated reception token; an anonymous kiosk token is NOT implemented.
- The TV date selector was added during verification because the original design fixed the date to "today", which broke testing with future appointments. In production the TV should default to today; the selector remains for operability.
- Skip does not auto-mark no-show; explicit no-show remains a Phase 5 action (could be wired as an option after N recalls).
- Frontend cancel/reschedule modals with idempotency keys: cancel modal completed in Phase 7 verification; reschedule modal still pending (carried from Phase 5).
- Room/resource full availability engine is partial: room overlap validated on booking, availability UI does not filter by room.
- Accept flow is eventually consistent (idempotent replay), not atomic; harden with saga/outbox-saga if strict atomicity is required.
- "Apuntarme" window defaults to 14 days; fine-grained window picker is debt.
- Matching runs inside the Outbox processing pipeline; move to a dedicated worker/queue if matching becomes heavy.
- Patient search normalizer uppercases but does NOT fold accents; searching "maria" without accent may miss "María". Minor debt.
- UserAdminService may still be incomplete from previous phases.

## Technical Debt

- Complete user administration.
- Add full room/resource aware availability engine.
- Add appointment calendar UI.
- Add notifications consumer (email/SMS) for waiting list offers, appointment events and room calls.
- Add background cleanup for expired idempotency records.
- Add more integration tests for concurrent booking, idempotency, waiting list offer/accept/conflict/re-match loop, and concurrent call-next / sequence uniqueness.
- Add audit UI.
- Add appointment policies configuration UI.
- Add saga/transactional consistency for accept (appointment + offer + entry) if business demands.
- Add window picker UI in booking flow.
- Add staff UI to manually prioritize/promote waiting list entries.
- Move matching to dedicated worker if Outbox latency grows.
- Validate permission/role stamps against DB per request (or short-lived tokens) to avoid stale-token authorization gaps.
- Add READ-ONLY reconciliation in GetBoardAsync so the board never shows orphaned calls in "En espera" without pressing "Llamar siguiente".
- Add anonymous kiosk token for the TV display.
- Add optional skip-with-no-show policy flag (after N recalls).
- Add waiting room metrics (avg wait, no-show rate) in the reports phase.
- Add room/consultatory selector to the TV display (so the blue "Turno #N · consultorio" shows the room name even when the board calls with a different room).
- Add accent folding to the patient search normalizer.
- Add reschedule modal with idempotency key to the appointments UI (carried from Phase 5).
- PROCESS RULE for future phases: close every operational act with a UI / service / domain table in the same draft, and put navigation properties inside the entity file from the first draft. Do not assume cross-layer signatures; confirm them or request them.

## Files Changed

Phase 7 introduced WaitingRoom domain (call aggregate + status enum + navigation props inside the entity file), application services (board service, call service with lazy reconciliation and unified recall, access service delegating to appointment access against the entity, event notifier, payloads, validators, store interface with GetQueuedCallsTrackedOrderedAsync), infrastructure (EF config, store, DbSets, DI), outbox handlers (updated/called/recalled), realtime additions (room group, events, hub join/leave with CancellationToken.None for synchronous guards), contracts, API controller, permissions/policies/seeder usage, frontend (waitingRoom API, realtime hook, board page with check-in-pending column, display page with medical/clinic/date selectors and custom white date icon, menu/routes), unit + architecture tests, and migration AddWaitingRoomPhase7.

Phase 6 introduced WaitingList domain (entries, offers, enums, priority, navigation props inside entity files), application services (waiting list service, access service, matching service, event notifier, options, validators, store interfaces), Outbox handlers (OfferCreated, Updated, SlotFreed trigger), infrastructure (EF configs, stores, expiration worker, DbSets, DI), contracts, API controller, permissions/policies/seeder additions, slot-freed triggers in appointment booking/workflow services, frontend (waiting-list API, realtime hook, WaitingListPage, "Apuntarme" in booking, menu/route), unit and architecture tests, and migration AddWaitingListPhase6.

Phase 5 introduced Appointments, Idempotency, Audit, booking concurrency, outbox handlers, SignalR events, API contracts/controllers and frontend appointment pages.

## Pending Decisions

- Confirm the operating discipline: consultations should be advanced from the ROOM (answer/complete), not from the Citas page, to avoid orphaned calls. Currently enforced only by convention + lazy reconciliation; could be hardened by hiding Phase 5 start/complete buttons once a call exists, or by read-only board reconciliation.
- Confirm whether skip should optionally mark no-show after N recalls (currently no).
- Confirm whether a doctor can call-next across clinics or strictly their assigned queue (currently delegated to appointment access; doctor limited to their DoctorId).
- Confirm TV display token strategy (reception-authenticated now; kiosk token later).
- Confirm whether waiting room sequence resets daily per (doctor, clinic) (currently yes, keyed by LocalDate).
- Confirm whether waiting list should stay in OFFER mode (default, patient consent on exact slot) or switch to AUTOMATIC booking mode (top candidate reserved directly on slot free). Automatic mode swap is prepared (replace offer creation with direct kernel call + stable key wl-auto:{entryId}:{slotStart}).
- Confirm whether reschedule of the original appointment should keep freeing its original slot for re-matching (currently YES via EnqueueSlotFreedIfFuture including Rescheduled).
- Confirm whether a waiting entry with clinic-null should match any clinic of the doctor (currently YES).
- Confirm whether accept should be atomic with offer/entry state (currently eventual via idempotent replay).
- Confirm whether patient can reschedule without staff confirmation (carried from Phase 5).
- Confirm whether reschedule should preserve original room assignment by default (carried from Phase 5).

## Notes for Next AI Agent

Before Phase 8 (Clinical Records):

1. Read PROJECT_CONTEXT.md fully, including Architecture Decisions and Known Issues.
2. Phase 7 is locally verified; do not redo its verification. The lazy-reconciliation and unified-recall fixes are part of the design, not temporary patches.
3. Do NOT introduce MediatR.
4. Do NOT let the waiting room (or clinical records) mutate Appointment.Status directly; always call the Phase 5 workflow service. Clinical notes are a SEPARATE aggregate referencing the appointment/encounter; they must NOT duplicate waiting room state.
5. Keep authorization delegated to IAppointmentAccessService, authorizing against the DOMAIN entity Appointment (not a DTO). Do not fork the permission matrix.
6. Keep the appointment as the source of truth and the dependent layers (waiting room, clinical records) unaware of each other; synchronize via lazy reconciliation or events, never by having the appointment call back into a dependent layer.
7. Keep SignalR thin and post-persistence via Outbox. No clinical content in SignalR payloads.
8. Keep availability backend authoritative.
9. NAVIGATION PROPERTIES go INSIDE the entity file from the first draft, never as a footnote on the EF configuration. Every HasOne(x => x.Nav) in a config requires Nav on the entity in the same delivery.
10. Do NOT invent endpoints or cross-layer methods. Use ONLY signatures confirmed against existing code or requested from the developer. Dropdowns use GET /api/v1/doctors (paginated) + GET /api/v1/doctors/{id} (GetById, includes Clinics). There is NO /doctors/options endpoint; do not reference it.
11. Close every operational act with a UI / service / domain table in the same draft: if the UI exposes a button, the service and the state machine must already have the matching transition. Misalignment between these three layers caused the recall 400 and the orphaned-call 409 in Phase 7.
12. When a guard is synchronous (e.g. hub group-join permission check), pass CancellationToken.None, not a non-existent Context property like CallAborted. HubCallerContext has ConnectionAborted, not CallAborted.
13. RoomStore has NO ExistsAsync(collection); validate a room with GetTrackedByIdAsync + null-check. DoctorStore and ClinicStore DO have ExistsAsync; do not assume symmetry across stores.
14. The onboarding gate in booking applies only to the pure Patient role (isPatientOnly = isPatient && !isStaff); staff with bypass must see the patient picker.
15. Clinical Records (Phase 8) must reference the Appointment/Encounter and tie notes to appointments in InProgress/Completed; do not reintroduce check-in or consultation logic there.