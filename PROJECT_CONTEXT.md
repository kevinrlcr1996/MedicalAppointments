# PROJECT CONTEXT

## Project

Medical Appointments Platform

## Current Phase

Clinical Records — Structured Clinical Data (Phase 8d complete)

## Current Version

0.8.5

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

Phase 8d — Clinical Records: structured clinical data (8d.1 vitals, 8d.2 ICD-10 diagnoses, 8d.3 patient allergies, 8d.4 patient active medications). ALL FOUR verticals locally verified; the patient-level dialogs are mounted in PatientsPage. Phase 8d is COMPLETE as a foundation; the remaining open items are environment/decision-dependent (C1 AV, storage swap, 8c adjacent acts, pending business/security decisions), not 8d code.

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

### Phase 8

- ClinicalNote aggregate (a SEPARATE aggregate that REFERENCES the appointment; the appointment is the encounter and is NOT duplicated):
  - fields: AppointmentId, AuthorUserId, AuthorDoctorId, AuthorDisplayName (immutable snapshot), Category (ClinicalNoteCategory: Subjective/Objective/Assessment/Plan/Other = SOAP), Title, Content, IsFinal, OccurredAtUtc, FinalizedAt, FinalizedByUserId, CorrectsNoteId
  - navigation properties (Appointment, AuthorDoctor, CorrectsNote?) declared INSIDE the entity file from the first draft (rule 9)
  - DIRECTIONALITY: the note knows the appointment (HasOne -> Appointment.WithMany()); the appointment does NOT navigate to notes (Appointment entity untouched, no collection added) — rule 6
- Inmutability + additive corrections (clinical standard):
  - a note is born as a draft (IsFinal=false)
  - Finalize closes it (IsFinal=true, FinalizedAt/By); a finalized note CANNOT be edited or deleted (UpdateDraft throws DomainException)
  - a correction is a NEW note linked via CorrectsNoteId (CreateCorrection requires the original to be IsFinal); the original stays intact
- Authorship = a professional with a DOCTOR PROFILE:
  - on create, the author's DoctorId is resolved via IDoctorStore.GetIdByUserIdAsync(userId); if null -> 409 "must have a doctor profile"
  - AuthorDisplayName snapshotted from the Doctor entity (FirstName/LastName), NOT via a User navigation (User does not exist in domain; assuming it was the /doctors/options-class error)
- Gate of appointment status: notes only allowed when appointment.Status is InProgress or Completed (else 409). CheckedIn/Pending/Confirmed/Cancelled/NoShow/Rescheduled -> 409. The UI hides the button accordingly AND the backend revalidates (never trust the client).
- Concurrency: optimistic via RowVersion (AggregateRoot) + normal EF transaction. NO sp_getapplock here (notes do not allocate a slot; assuming BookingLockResources.ForInterval would have been another "assumed symmetry").
- Authorization (own permission matrix, NOT wrapping IAppointmentAccessService):
  - granular permissions: clinical_records.read / create / update / finalize / correct / manage_all
  - read scope delegated to appointment read perms (read_all / read_assigned) combined with clinical_records.read; reading a specific appointment's notes requires CanReadAll OR (CanReadAssigned AND subject.DoctorId == appointment.DoctorId)
  - create requires clinical_records.create AND a doctor profile AND (manage_all OR subject.DoctorId == appointment.DoctorId)
  - update/finalize require the respective perm AND (manage_all OR note.AuthorUserId == subject.UserId)
  - correct requires clinical_records.correct AND (manage_all OR original.AuthorUserId == subject.UserId)
  - Patient has NO clinical_records.* in Phase 8 (history to patient = separate phase, consent/portal)
  - Policies: ClinicalRecordsRead (clinical_records.read), ClinicalRecordsManage (create||update||finalize||correct||manage_all)
  - Seeder grants: Doctor = read/create/update/finalize/correct; Admin = read/manage_all; Receptionist = read; Patient = none
- Outbox events:
  - ClinicalRecords.NoteCreated
  - ClinicalRecords.NoteUpdated
  - ClinicalRecords.NoteFinalized
- SignalR events:
  - ClinicalNoteUpdated (single event reused for created/updated/finalized)
  - published to doctor:{id}, clinic:{id}, patient:{id}, appointment:{id}, user:{id} groups
  - PAYLOAD = METADATA ONLY (noteId, appointmentId, doctorId, clinicId, patientId, authorUserId, category, isFinal, correctsNoteId, changeType, occurredAt, correlationId). NO Title/Content. The frontend uses the event to invalidate queries, never to render clinical text.
- Frontend:
  - clinicalRecords API service (listByAppointment, create, update, finalize, correct)
  - useClinicalRecordsRealtime hook (invalidates ['clinical-records'] on ClinicalNoteUpdated)
  - ClinicalNotesDialog mounted from the Citas page (button "Notas clínicas" shown only on InProgress/Completed rows when the user has clinical_records.read): list notes, create draft, edit draft (hidden on finalized), finalize, correct (badge "Corrige: <titulo>"), category select (SOAP), title/content inputs
  - NO global history route/menu: notes are reached FROM the appointment (the encounter), avoiding exposure of a global clinical list; patient history is a separate phase
- Tests:
  - ClinicalNote domain rules (create starts as draft; UpdateDraft throws when finalized; CreateCorrection links to finalized original; CreateCorrection throws when original not finalized)
  - ClinicalRecords architecture layer tests (no outer-layer / EF / MediatR deps)
- Migration: AddClinicalRecordsPhase8 (ClinicalNotes; FKs Restrict to Appointments/Doctors + self-ref CorrectsNoteId; indexes on AppointmentId, (AppointmentId,CreatedAt), AuthorDoctorId, CorrectsNoteId). Appointments table NOT modified.
- User administration fix applied DURING Phase 8 verification (see Files Changed): UserAdminService.CreateAsync now actually assigns roles (was echoing command.Roles without calling AssignRole); AssignRolesAsync implemented; IUserStore.GetRoleIdsByNamesAsync + EfUserStore implementation added; CreateUserCommandValidator added. This is what makes POST /api/v1/users grant the Doctor role via API (no SQL needed).
- Doctor-directory 403 fix applied DURING Phase 8 verification (see Known Issues): GET /api/v1/doctors (GetList) and GET /api/v1/doctors/{id} (GetById) are both guarded by [Authorize(Policy = Permissions.DoctorsRead)]; there is NO DoctorsReadAll constant. The Doctor role lacked doctors.read, so the medical/clinic selectors 403'd for a pure doctor in Reservar, the waiting-room board and Availability. Fixed by adding Permissions.DoctorsRead to the Doctor role grants in IdentitySeeder (one line; the same grant covers list + clinic-detail). Requires API restart (seeder runs on startup) + re-login (permissions are login-time claims).
- LOCALLY VERIFIED by developer. Confirmed on screen with a real doctor user (dra.ana, profile linked to her login): the full immutability chain draft -> edit -> finalize -> correct works (a finalized note cannot be edited and a correction is an additive linked note while the original stays intact); authorship requires a linked doctor profile (a staff user without one gets 409 on authoring even though they can read); the status gate restricts notes to InProgress/Completed; role isolation holds (SuperAdmin reads but cannot author -> 409 profile; Patient has no access -> no dialog button and 403 on GET notes); and the realtime ClinicalNoteUpdated payload is metadata-only (no Title/Content over SignalR). The login<->doctor-profile link that initially blocked the run was fixed during verification (SQL UPDATE of Doctors.UserId); the process rule "verify the link, not just existence" is retained below. The 403 on GET /api/v1/doctors for the Doctor role was ALSO resolved during verification (added Permissions.DoctorsRead to the Doctor role in IdentitySeeder); it never blocked the clinical-notes verification (notes live in Citas) and is now closed.

### Phase 8b

- ClinicalAttachment aggregate (a SEPARATE, IMMUTABLE aggregate that REFERENCES a ClinicalNote; the note is the parent and is NOT modified to hold a collection):
  - fields: NoteId, FileName (original, legible, metadata only), ContentType, SizeBytes, StorageKey (GUID-based, sanitizada, NUNCA el nombre original -> anti path-traversal y anti colisión), Sha256Hex (64 hex), ScanStatus (ClinicalAttachmentScanStatus: Pending/Clean/Skipped/Infected), ScannedAt, UploadedByUserId
  - navigation property (Note) declared INSIDE the entity file from the first draft (rule 9)
  - DIRECTIONALITY: the attachment knows the note (HasOne -> Note.WithMany()); the note does NOT navigate to attachments (ClinicalNote de Fase 8 NO modificado) — rule 6, misma direccionalidad que nota->cita
  - INMUTABLE: no Update/Delete setters; Create-only with guards (FileName no vacío, SizeBytes>0, StorageKey no vacío, Sha256Hex exactamente 64 hex, UploadedByUserId no vacío). Infected NUNCA persiste (se rechaza en el upload con rollback + 409 + auditoría), por lo que el estado persistido es Clean o Skipped.
- Attachments extend clinical immutability (Phase 8 rule applied to files):
  - an attachment can ONLY be added to a DRAFT note (note.IsFinal == false); uploading to a finalized note -> 409 "only be added to a draft"
  - finalizing a note seals its attachments (UI hides "Adjuntar archivo" on finalized cards; backend revalidates)
  - the attachment is a post-save section in the note card (it needs the note's Id to exist before a file can be linked/stored), NOT an input inside the note-writing form
  - (8c superseded the "NO delete endpoint" rule: removal from a draft is now a SOFT-DELETE with audit, see Phase 8c; finalized notes still cannot be un-attached)
- Storage abstraction (open-source/free, no paid service):
  - IFileStorage + StoredFile; LocalDiskFileStorage default (works out-of-the-box, zero external infra); StorageRoot configurable; ResolveSafe validates the resolved path stays under the root (double defense vs traversal, even though keys are GUID-based)
  - swap to MinIO/S3-compatible later WITHOUT refactor (the interface is the seam; provider choice is a pending decision, STILL OPEN as of 8d)
  - DeleteAsync exists ONLY as internal technical cleanup (rollback of failed/infected uploads), NOT exposed as a user endpoint
- Antivirus abstraction (plugable, default non-blocking in dev):
  - IFileScanner + ScanResult; ClamAvFileScanner real implementation (clamd INSTREAM protocol over System.Net.Sockets, no external library) with internal fallback
  - Attachments:Scanner:Enabled (default false in Development -> ScanStatus=Skipped so the vertical runs without clamd; MUST be true in production with clamd active)
  - the scan RESULT affects the flow: Infected -> rollback temp + 409 + audit, never enters the record; scanner UNAVAILABLE (exception) -> Skipped + log (config decision: AV downtime does not block clinical recording); unexpected response -> Skipped + log
  - this is NOT a half-implemented upload: the contract is implemented and wired; the only concession is a config flag, which is an explicit decision, not a TODO
- Limits (two independent layers):
  - business limit Attachments:MaxBytes (default 10 MB) -> 409 "exceeds the maximum size" pre-persistence, no row, no disk file
  - transport limit [RequestSizeLimit(20 MB)] on the controller -> cuts before the service (413-class), proving the two layers are independent
  - content-type whitelist (pdf/jpeg/png/webp/doc/docx) -> 409 "Content type not allowed" for .txt/.exe/.zip
- Authorization REUSES the Phase 8 matrix (NO new permissions, Permissions.cs untouched):
  - upload attachment = same gate as editing the draft -> CanUpdateNote(note, subject) (reuso confirmado Fase 8)
  - list/download attachment = same gate as reading the note's appointment -> CanReadNotesForAppointment(appointment, subject) (reuso confirmado)
  - patient history = read notes with scope -> CanReadNotes(subject) + read_assigned filter by DoctorId (reuso confirmado)
  - Patient has NO clinical_records.* (Phase 8 rule intact); history to patient = separate phase (consent/portal)
- Patient history (read-only aggregated projection, NOT a new aggregate):
  - GET /clinical-records/patients/{patientId}/history groups ClinicalNotes of ALL the patient's appointments
  - scope: read_all sees all; read_assigned sees only notes whose appointment.DoctorId == subject.DoctorId (a doctor sees only THEIR encounters with that patient)
  - ordered by Appointment.StartAt desc, then CreatedAt
  - source of truth remains ClinicalNote -> Appointment; NO Encounter/PatientHistory aggregate created
  - projection resolves DoctorFullName/ClinicName via Include in cascade (Appointment.Doctor/.Clinic) — B5 NOW CLOSED (see Known Issues): Appointment.cs confirms Doctor/Clinic/Patient/Room as PUBLIC navigations; Doctor.FirstName/LastName used successfully in 8d.1/8d.2; Clinic.Name rendered in the Citas CLINICA column. The Include-in-cascade is valid; the cross-layer-navigation root cause of B5 is eliminated. The developer's "todo funciona" in 8d is consistent with the history rendering names, but B5 was closed by the pasted Appointment.cs, not by the report.
- Outbox events:
  - ClinicalRecords.AttachmentUploaded (reused in 8c for the Deleted changeType; no new event type)
- SignalR events:
  - ClinicalAttachmentUpdated (published via IRealtimePublisher.PublishToGroupsAsync to the SAME confirmed groups doctor/clinic/patient/appointment/user; hub untouched)
  - PAYLOAD (8b) = METADATA ONLY (attachmentId, noteId, appointmentId, doctorId, clinicId, patientId, fileName, contentType, sizeBytes, scanStatus, changeType, occurredAt, correlationId). NO binary, NO StorageKey, NO Sha256, NO clinical Title/Content. (8c further sanitized the payload: fileName/contentType REMOVED, see Phase 8c.)
- Frontend:
  - clinicalAttachments API service (listByNote via apiClient.get; upload via apiClient.postMultipart; download via apiClient.downloadBlob -> object-URL)
  - useClinicalAttachmentsRealtime hook (invalidates ['clinical-attachments'] and ['clinical-records'] on ClinicalAttachmentUpdated; mounted ONCE in the layout/App, like useClinicalRecordsRealtime)
  - AttachmentsSection + ScanBadge inside ClinicalNotesDialog note cards: upload label rendered ONLY when canUpdate && !note.isFinal (drafts); list + download always (whoever reads the note reads its attachments); download is a Button onClick -> downloadBlob (NOT an <a href> relative that loses token and hits the frontend port)
  - ClinicalHistoryDialog mounted from PatientsPage; per-row "Historial clínico" button via the EXISTING CrudColumn.render slot of CrudResourcePage (the generic component was NOT patched — the slot already existed; reading it beat inventing a rowActions prop)
  - NO global history route/menu; history is reached FROM the patient row; patient-facing history is a separate phase
- apiClient fix applied DURING 8b (see Files Changed / Known Issues): the original post did JSON.stringify(body) + forced Content-Type application/json, so passing a FormData compiled (unknown accepts it) but sent "{}" -> backend [FromForm] IFormFile null -> 400 "No file received". And request did response.text()+JSON.parse ALWAYS, corrupting binary downloads. Fixed by adding an internal rawRequest (same auth/token/baseURL/refresh/correlationId, but WITHOUT forcing JSON and WITHOUT text()+parse on the success path) plus postMultipart (body=FormData, no manual Content-Type -> browser builds the boundary) and downloadBlob (returns the intact Blob). get/post/put/patch/delete and request are UNCHANGED (zero regression on the happy path). Lesson recorded: "compile-clean != runtime-correct"; the BODY is the evidence, not the TypeScript tooltip.
- Tests:
  - ClinicalAttachment domain rules (Create persists Clean/Skipped; throws on bad hash length; throws on zero size)
  - ClinicalRecords architecture layer tests extended to the attachment assemblies (no outer-layer / EF / MediatR deps)
- Migration: AddClinicalAttachmentsPhase8b (ClinicalAttachments; FK Restrict to ClinicalNotes; indexes on NoteId, ScanStatus, Sha256Hex). ClinicalNotes and Appointments tables NOT modified.
- Configuration: Attachments section (StorageRoot, MaxBytes, AllowedContentTypes, Scanner:Enabled/Host/Port/Timeout).
- LOCALLY VERIFIED by developer (reported "ya funciona la fase 8b" after running the 8b verification script): the attachments vertical works (upload to a draft persists a row with GUID StorageKey + 64-hex Sha256 + Clean/Skipped status and a physical file on disk; download returns the intact binary with token+baseURL via downloadBlob; size/type limits 409 pre-persistence; finalized notes hide the upload label and 409 on API -> immutability extends to attachments; realtime ClinicalAttachmentUpdated refreshes a second window without reload with a metadata-only payload). The patient-history vertical works (per-row button via CrudColumn.render; read_assigned scope shows only the doctor's own encounters; read_all shows all; Patient has no column and 403 on API). C1 (the EICAR AV rejection) remains environment-dependent (see Known Issues): if clamd was enabled it must be exercised; if Scanner:Enabled:false it is honestly marked "not exercised in dev", not faked green. B5 was flagged here and is NOW CLOSED in 8d (see Known Issues).

### Phase 8c

- Attachments soft-delete with audit (extends the 8b immutability rule from "no delete at all" to "remove-from-draft only, as a reversible-by-audit soft delete"):
  - ClinicalAttachment gains IsDeleted (bool), DeletedAt (DateTimeOffset?), DeletedByUserId (Guid?) — flags declared INSIDE the entity file (rule 9), private set
  - SoftDelete(deletedByUserId, now) is the ONLY mutator: guards empty-user -> DomainException; IDEMPOTENT (if already IsDeleted, no-op, so DeletedAt is NOT re-written and no second audit/event fires — protects double-click / two-window races without a RowVersion conflict)
  - the draft-only gate lives in the SERVICE (CanUpdateNote + !note.IsFinal), NOT in the entity (the entity does not know the parent note's state): removing from a finalized note -> 409 "sealed and cannot be removed"
  - NO hard delete: the DB row PERSISTS with IsDeleted=true and the PHYSICAL file on disk is NOT removed (clinical traceability + audit + possible recovery); physical purge of soft-deleted files is 8d background-cleanup debt (analogous to idempotency-record cleanup)
  - audit entry ClinicalRecords.AttachmentDeleted is SANITIZED: logs extension + sizeBytes + sha256, NEVER the original FileName (a descriptive name like "lab-juan-perez-oncologia.pdf" leaks patient context)
- List/count filters EXCLUDE soft-deleted; tracked-by-id does NOT (architectural distinction, see Architecture Decisions):
  - GetByNoteAsync adds && !x.IsDeleted (so the dialog list and the badge count never show removed attachments)
  - GetCountsByNoteIdsAsync (NEW store method) GroupBy(NoteId).Count(!IsDeleted) over the attachments DbSet — the note does NOT navigate to attachments (rule 6 intact), so the count is a SEPARATE query, not a collection load
  - GetTrackedByIdAsync MUST keep returning deleted rows (no filter): the service needs to SEE IsDeleted to deny download of a removed file and to make DeleteAsync idempotent; filtering it by mistake would break both the download guard and the double-click no-op
- Attachment-count badge in the patient-history timeline (closes the 8b debt where ClinicalHistoryItemDto.AttachmentCount was a fixed 0):
  - GetPatientHistoryAsync now calls GetCountsByNoteIdsAsync(noteIds) and maps the real count into ClinicalHistoryItemDto.AttachmentCount
  - the count reflects only NON-deleted attachments (a soft-deleted file does not bump the badge)
  - requires IClinicalAttachmentStore injected into ClinicalRecordsService (additive constructor param; SEE Known Issues 8c-COUNT: if the service constructor was not updated, the badge stays 0 / the call fails — confirm on screen, do not assume)
- Payload/log name sanitization (closes the 8b privacy debt):
  - ClinicalAttachmentEventPayload REMOVES fileName and contentType (keeps attachmentId/noteId/appointmentId/doctorId/clinicId/patientId/sizeBytes/scanStatus/changeType/occurredAt/correlationId) — the legible name NEVER travels over SignalR; the client re-fetches it via the AUTHORIZED listByNote (CanReadNotesForAppointment)
  - the outbox handler is unchanged (publishes the same ClinicalAttachmentUpdated to the same groups); removing the fields from the record removes them from the wire JSON
  - audit logs also omit the original name (see soft-delete audit above)
- Download guard for soft-deleted (ADDITION to 8b, which had no delete):
  - DownloadAsync must throw NotFoundException/ConflictException when attachment.IsDeleted (a removed file must not serve its binary); 8b did NOT have this guard because 8b had no delete — it is an 8c additive, SEE Known Issues 8c-DL-GUARD: confirm on screen, do not assume it was applied
- API:
  - DELETE /clinical-records/attachments/{attachmentId} (ClinicalRecordsManage; reuses CanUpdateNote + draft gate; returns 204 NoContent; idempotent)
  - no new permissions, no new policies (Permissions.cs untouched; reuses clinical_records.* of Phase 8)
- Frontend:
  - clinicalAttachmentsApi.remove(attachmentId) via apiClient.delete (CONFIRMED in the apiClient body pasted in 8b: delete<T>(path, options?))
  - AttachmentsSection gains a "quitar" Button (Trash2) rendered ONLY when canUpdate (i.e. draft, same gate as "Adjuntar archivo"); handleDelete -> deleteMutation -> invalidate ['clinical-attachments','note',id]; localized delete error via ApiError
  - ClinicalHistoryDialog renders a 📎 N badge per timeline item when item.attachmentCount > 0
- Tests:
  - ClinicalAttachment domain: SoftDelete marks flags + Touch; SoftDelete idempotent when already deleted (DeletedAt stable); SoftDelete throws on empty user
  - (store-level !IsDeleted filtering and GetCountsByNoteIdsAsync are integration concerns, folded into the existing integration-test debt, not unit domain tests)
- Migration: AddAttachmentSoftDeletePhase8c (ClinicalAttachments + IsDeleted/DeletedAt/DeletedByUserId + partial index IX_ClinicalAttachments_NoteId_IsDeleted). ClinicalNotes and Appointments tables NOT modified.
- Storage provider swap (the 4th 8c act) is NOT delivered: it requires the developer to pick the provider (MinIO/S3-compatible/Azure — a pending decision) and to paste the .csproj so the NuGet library + version + API are CONFIRMED, not assumed (delivering a MinioFileStorage with an assumed PutObjectAsync signature would repeat the apiClient.postForm / DoctorsReadAll assumed-symmetry pattern). The IFileStorage seam from 8b already allows the swap WITHOUT refactor; the adapter is additive and will be delivered COMPLETE in the next pass once the .csproj + provider decision arrive. LocalDiskFileStorage remains the working default.
- LOCALLY VERIFIED by developer (reported "ya funciona" for the remove symptom). The CORE soft-delete vertical is confirmed on screen: clicking "quitar" on a DRAFT removes the attachment from the list AND persists the row with IsDeleted=true (the disappearance from the list proves BOTH the list filter && !IsDeleted AND that the DELETE persisted the flag — if the DELETE had not persisted, the filter would exclude nothing and the file would still show). The physical file is preserved by design (soft-delete, not hard-delete). The following 8c acts are NOT asserted green from the global report and remain to be confirmed on screen (see Known Issues): the finalized-note remove gate (409 + hidden button), double-remove idempotency, the download-a-deleted guard (404/409 — additive, 8b lacked it), the realtime Deleted event carrying NO fileName/contentType, the 📎 N badge showing the real non-deleted count (depends on the service-constructor injection), and name-sanitization in wire+log. C1 from 8b ALSO remains open. The storage-provider swap remains a pending decision (no .csproj + no provider chosen). NOTE: the developer's later "todo funciona" in 8d did NOT itemize these 8c adjacent acts and does NOT extend to them; they remain flagged until individually confirmed.

### Phase 8d.1 — Vital Signs (per-encounter)

- VitalSigns aggregate (a SEPARATE, IMMUTABLE aggregate that REFERENCES the appointment, exactly like ClinicalNote/ClinicalAttachment; the appointment is NOT modified to hold a collection):
  - fields: AppointmentId, AuthorUserId, AuthorDoctorId, AuthorDisplayName (immutable snapshot), SystolicmmHg, DiastolicmmHg, HeartRateBpm, TemperatureC, TemperatureMethod (VitalSignsTemperatureMethod enum), Position (VitalSignsPosition enum), RespiratoryRatePerMin, SpO2Percent, WeightKg, HeightCm, PainScore0to10, Note, IsFinal, RecordedAtUtc, FinalizedAt, FinalizedByUserId, CorrectsVitalSignsId
  - navigation properties (Appointment, AuthorDoctor, CorrectsVitalSigns?) declared INSIDE the entity file from the first draft (rule 9)
  - DIRECTIONALITY rule 6: the vital knows the appointment (HasOne -> Appointment.WithMany()); the appointment does NOT navigate to vitals (Appointment entity untouched)
  - INMUTABLE content + additive corrections (espejo Fase 8): born draft (IsFinal=false); Finalize closes it; UpdateDraft throws when finalized; CreateCorrection requires the original to be IsFinal and produces a NEW linked draft while the original stays intact
  - physiological guards in the DOMAIN (self-contained, validate only PRESENT values): systolic 50-300, diastolic 30-200, systolic>diastolic, HR 20-250, temp 30-45°C, RR 4-60, SpO2 50-100%, weight 1-400kg, height 30-260cm, pain 0-10, note<=2000 — out-of-range -> DomainException -> 400 pre-persistence, no row
- Gate of appointment status: SAME as notes — only InProgress/Completed (else 409). CheckedIn/Pending/Confirmed/Cancelled/NoShow/Rescheduled -> 409. UI hides the button accordingly AND backend revalidates.
- Authorship = DOCTOR PROFILE resolved at runtime (IDoctorStore.GetIdByUserIdAsync + GetTrackedWithRelationsAsync for the name snapshot); if null -> 409 "requires a doctor profile". Reading vs authoring distinct gates (same as Fase 8).
- Concurrency: optimistic RowVersion + normal EF transaction; NO app-lock (vitals do not allocate a slot).
- Authorization REUSES the Phase 8 matrix (NO new permissions): list = CanReadNotesForAppointment; create/update/finalize/correct = the respective CanCreate/CanUpdate/CanFinalize/CanCorrect flags + (CanManageAll OR ownership/authorship). Gates computed INLINE from subject flags (no assumed CanCreateNote-style method on the access service).
- Outbox events: ClinicalRecords.VitalSignsCreated / VitalSignsUpdated / VitalSignsFinalized
- SignalR events: VitalSignsUpdated (single event reused for created/updated/finalized); published to doctor:{id}, clinic:{id}, patient:{id}, appointment:{id}, user:{id} groups
- PAYLOAD = METADATA ONLY (vitalSignsId, appointmentId, doctorId, clinicId, patientId, authorUserId, isFinal, correctsVitalSignsId, changeType, recordedAtUtc, occurredAt, correlationId). NO numeric values (systolic/diastolic/HR/temp/RR/SpO2/weight/height/pain are PHI and NEVER travel over SignalR). The frontend invalidates queries, never renders clinical numbers from the event.
- Frontend:
  - vitalSigns API service (listByAppointment, create, update, finalize, correct) via apiClient.get/post/put (CONFIRMED bodies)
  - useVitalSignsRealtime hook (invalidates ['vital-signs'] on VitalSignsUpdated)
  - VitalSignsDialog mounted from the Citas page (AppointmentsPage), button "Signos vitales" shown ONLY when canReadNotes && (InProgress||Completed) — the SAME literal condition as "Notas clínicas" (read vs author respected separately: the outer button gates on clinical_records.read; the in-dialog create/edit/finalize/correct gate on the authoring perms)
  - NO global route; vitals reached FROM the appointment
- Tests:
  - VitalSigns domain rules (create starts draft; UpdateDraft throws when finalized; CreateCorrection links to finalized original and throws when original not finalized; SpO2 out-of-range throws; systolic<=diastolic throws)
  - ClinicalRecords architecture layer tests extended to the vital-signs assemblies (no outer-layer / EF / MediatR deps)
- Migration: AddVitalSignsPhase8d1 (VitalSigns; FKs Restrict to Appointments/Doctors + self-ref CorrectsVitalSignsId; indexes on AppointmentId, (AppointmentId,CreatedAt), AuthorDoctorId, CorrectsVitalSignsId). Appointments/ClinicalNotes/ClinicalAttachments tables NOT modified.
- Handler fix applied DURING 8d.1 verification (see Known Issues): the first 8d.1 draft used an ABSTRACT base handler with a PARAMETERLESS constructor ("resolved via DI ctor below" — a lie) plus three sealed derivatives that declared NO constructor of their own. DI built the derivatives via the implicit parameterless ctor -> the base parameterless ctor ran -> _publisher/_jsonOptions stayed NULL -> NullReferenceException at PublishToGroupsAsync (caught on screen while the outbox processor retried the message). The write itself SUCCEEDED (the VitalSigns row committed BEFORE the outbox), which is why the author's own dialog showed the data while cross-window realtime did not. Fixed by replacing the abstract with THREE SEALED handlers each with a REAL (IRealtimePublisher, JsonSerializerOptions) constructor — the exact pattern of ClinicalAttachmentUploadedOutboxHandler (8b) that already publishes successfully. Lesson: do NOT refactor a working infrastructure pattern (handlers/DI/transport) to a "DRY" abstract with a trap constructor; the uniformity of DI constructor resolution across abstract hierarchies is NOT assumed — replicate the pattern that already works.
- LOCALLY VERIFIED by developer (reported "8d.1 y 8d.2 funcionan correctamente, ya los podemos dar por buenos"). Confirmed on screen: the dialog renders a created vital with real values (PA 120/80, FC 75, Temp 36.5 (Oral), FR 16, SpO2, peso 70, talla 175, posición Sitting, dolor, note) — proving create + list + render end-to-end; with the handler fix the realtime VitalSignsUpdated cross-window refresh + metadata-only payload (no numbers) is the remaining on-screen check the developer's global report covers under "funcionan correctamente". The physiological guards, status gate, authorship gate, finalized-immutability, additive correction, and role isolation follow the SAME verified mechanics as Fase 8 notes (the developer confirmed those in Fase 8 and the 8d.1 service reuses them literally).

### Phase 8d.2 — ICD-10 Diagnoses (per-encounter)

- Diagnosis aggregate (SEPARATE, IMMUTABLE, REFERENCES the appointment; appointment NOT modified):
  - fields: AppointmentId, AuthorUserId, AuthorDoctorId, AuthorDisplayName, Code (ICD-10 string), DisplayTerm, Category (DiagnosisCategory: Primary/Secondary/Differential/Other), OnsetDate (DateOnly?), Note, IsFinal, RecordedAtUtc, FinalizedAt, FinalizedByUserId, CorrectsDiagnosisId
  - navigation properties (Appointment, AuthorDoctor, CorrectsDiagnosis?) INSIDE the entity file (rule 9); directionality rule 6 (diagnosis knows appointment; appointment does NOT navigate to diagnoses)
  - INMUTABLE content + additive corrections (espejo Fase 8/8d.1): draft -> finalize -> UpdateDraft throws when finalized -> CreateCorrection requires original IsFinal, produces linked draft, original intact
  - ICD-10 SYNTACTIC validation in the DOMAIN via compiled regex ^[A-TV-Z][0-9][0-9](\.[0-9A-Z]{1,4})?$ (U reserved excluded; rejects 123, U07.1, A00.1.2.3.4, empty, I1); code normalized to UPPERCASE; display term required <=300; note <=2000. NO semantic catalog validation (no ICD-10 table assumed — hardening 8d.2b debt). Out-of-syntax -> DomainException -> 400 pre-persistence, no row.
- Gate of appointment status: SAME as notes/vitals — only InProgress/Completed (else 409).
- Authorship = DOCTOR PROFILE (IDoctorStore.GetIdByUserIdAsync + GetTrackedWithRelationsAsync); null -> 409.
- Concurrency: optimistic RowVersion + normal EF transaction; NO app-lock.
- Authorization REUSES the Phase 8 matrix (NO new permissions); gates computed INLINE from subject flags.
- Outbox events: ClinicalRecords.DiagnosisCreated / DiagnosisUpdated / DiagnosisFinalized
- SignalR events: DiagnosisUpdated (reused for created/updated/finalized); published to doctor/clinic/patient/appointment/user groups
- PAYLOAD = METADATA ONLY (diagnosisId, appointmentId, doctorId, clinicId, patientId, authorUserId, isFinal, correctsDiagnosisId, changeType, recordedAtUtc, occurredAt, correlationId). NO Code, NO DisplayTerm (both PHI — a diagnosis code/term reveals condition and NEVER travels over SignalR). Frontend invalidates, never renders clinical text/code from the event.
- Frontend:
  - diagnoses API service (listByAppointment, create, update, finalize, correct) via apiClient.get/post/put
  - useDiagnosisRealtime hook (invalidates ['diagnoses'] on DiagnosisUpdated)
  - DiagnosisDialog mounted from the Citas page (AppointmentsPage), button "Diagnósticos" shown ONLY when canReadNotes && (InProgress||Completed) — SAME literal condition as notes/vitals; code input uppercased in UI but the HARD validation is the domain regex (backend 400 if invalid; no shared schema assumed)
  - NO global route; diagnoses reached FROM the appointment
- Tests:
  - Diagnosis domain rules (create starts draft; normalizes code to uppercase; UpdateDraft throws when finalized; CreateCorrection links to finalized original and throws when original not finalized; invalid ICD-10 codes throw via [Theory]; empty display term throws)
  - ClinicalRecords architecture layer tests extended to the diagnosis assemblies
- Migration: AddDiagnosesPhase8d2 (Diagnoses; FKs Restrict to Appointments/Doctors + self-ref CorrectsDiagnosisId; indexes on AppointmentId, (AppointmentId,CreatedAt), AuthorDoctorId, CorrectsDiagnosisId, Code). Appointments/ClinicalNotes/VitalSigns/ClinicalAttachments tables NOT modified.
- Handlers: THREE SEALED with real ctors (the corrected 8d.1 pattern; NOT the abstract-with-parameterless that caused the NRE).
- LOCALLY VERIFIED by developer (reported "8d.1 y 8d.2 funcionan correctamente, ya los podemos dar por buenos"). The syntactic ICD-10 guard, status gate, authorship gate, finalized-immutability, additive correction, role isolation, and metadata-only payload (no code/term) follow the SAME verified mechanics as 8d.1/Fase 8; the developer's global report covers them under "funcionan correctamente".

### Phase 8d.3 — Patient Allergies (standing data, per-PATIENT)

- PatientAllergy aggregate (SEPARATE, IMMUTABLE-content aggregate that REFERENCES the PATIENT, NOT the appointment — standing data persists across encounters):
  - fields: PatientId, AuthorUserId, AuthorDoctorId, AuthorDisplayName, Allergen, AllergenClass (AllergenClass enum: Unknown/Drug/Food/Latex/Environmental/InsectVenom/Other), Severity (AllergySeverity enum: Unknown/Mild/Moderate/Severe), Reaction, OnsetDate (DateOnly?), IsResolved, ResolvedAt, ResolvedByUserId, IsFinal, RecordedAtUtc, FinalizedAt, FinalizedByUserId, CorrectsAllergyId
  - navigation properties (Patient, AuthorDoctor, CorrectsAllergy?) INSIDE the entity file (rule 9); directionality rule 6 (allergy knows patient; Patient.cs NOT modified — no collection added)
  - INMUTABLE CONTENT + additive corrections (espejo Fase 8/8d.1/8d.2): draft -> finalize -> UpdateDraft throws when finalized -> CreateCorrection requires original IsFinal, produces linked draft, original intact
  - CYCLE OF LIFE ORTHOGONAL to IsFinal (extension of Fase 8, pending decision): MarkResolved / MarkUnresolved are DEDICATED auditable transitions (NOT content edits), allowed ONLY when IsFinal (a draft cannot be resolved), idempotent (already-resolved -> no-op, ResolvedAt stable). Resolving a finalized allergy does NOT rewrite allergen/severity/reaction — it flips IsResolved + timestamps. Precedents: Appointment workflow transitions; ClinicalAttachment SoftDelete (8c). Difference from 8c: 8c remove was draft-only; allergy resolution MUST work post-finalize (clinical semantics: a validated allergy is later resolved when the patient is no longer allergic).
  - guards: allergen required <=200; reaction <=1000; empty-user on resolve -> DomainException
- NO appointment-status gate (DIFFERENCE from notes/vitals/diagnoses): allergies describe the PATIENT, independent of any encounter, so they can be recorded at any time (consultation, admission, phone). Documented as structural difference.
- Authorization REUSES the Phase 8 matrix (NO new permissions): list = CanReadAll OR CanReadAssigned (NO patient-scope filter — see pending decision); create = CanCreate + doctor profile; update/finalize/resolve/unresolve = CanUpdate/CanFinalize + (CanManageAll OR authorship); correct = CanCorrect + (CanManageAll OR authorship of original). Gates computed INLINE from subject flags.
- NO patient-scope filter implemented (pending decision, NOT invented): Fase 8 scopes read_assigned by appointment.DoctorId, but standing data has NO encounter to filter, and defining "the doctor's patients" (past appointments? active? any interaction?) is a business criterion NOT present in the code — inventing it would be the prohibited assumed-symmetry. Posture: any clinical_records.read user sees any patient's allergies; consequence honestly noted: a Receptionist (clinical_records.read since Fase 8) SEES allergies but CANNOT author (requires doctor profile). If the business wants restriction-by-patient-or-safety-critical-broadening, it is a pending decision to be made with evidence, not assumed.
- Existence check: PatientExistsAsync via _dbContext.Patients.AnyAsync (the ONLY residual assumption was the DbSet NAMING convention, uniform across 11+ observed DbSets — NOT a method signature, which is what broke GetTrackedByIdAsync on IDoctorStore). NOW CONFIRMED by the working vertical (the existence check ran without throwing in the developer's "todo funciona" run).
- Outbox events: ClinicalRecords.AllergyCreated / AllergyUpdated / AllergyFinalized / AllergyResolved (Resolved reused for Unresolved)
- SignalR events: PatientAllergyUpdated (reused for all change types); published to patient:{id} + user:{authorUserId} groups (standing data of patient: no appointment/doctor/clinic to scope; the reader is validated by clinical_records.read)
- PAYLOAD = METADATA ONLY (allergyId, patientId, authorUserId, isFinal, isResolved, correctsAllergyId, changeType, recordedAtUtc, occurredAt, correlationId). NO Allergen, NO Severity, NO Reaction (all PHI — an allergen/severity/reaction reveals condition and NEVER travels over SignalR). Frontend invalidates, never renders clinical text from the event.
- Frontend:
  - patientAllergies API service (listByPatient, create, update, finalize, resolve, unresolve, correct) via apiClient.get/post/put
  - usePatientAllergiesRealtime hook (invalidates ['patient-allergies'] on PatientAllergyUpdated)
  - PatientAllergiesDialog mounted from the Pacientes page (PatientsPage), button "Alergias" shown under the SAME canReadHistory condition as "Historial clínico" (clinical_records.read); in-dialog authoring gated on the authoring perms
  - NO global route; allergies reached FROM the patient row
- Tests:
  - PatientAllergy domain rules (create starts draft+unresolved; UpdateDraft throws when finalized; MarkResolved requires finalized; resolve->unresolve round-trips; MarkResolved idempotent; CreateCorrection links to finalized original and throws when original not finalized; empty allergen throws)
  - ClinicalRecords architecture layer tests extended to the allergy assemblies
- Migration: AddPatientAllergiesPhase8d3 (PatientAllergies; FKs Restrict to Patients/Doctors + self-ref CorrectsAllergyId; indexes on PatientId, (PatientId,IsResolved), AuthorDoctorId, CorrectsAllergyId). Patients/Appointments/ClinicalNotes/VitalSigns/Diagnoses/ClinicalAttachments tables NOT modified.
- Handlers: FOUR SEALED with real ctors (Created/Updated/Finalized/Resolved; the corrected 8d.1 pattern).
- LOCALLY VERIFIED by developer (reported "todo funciona" after the PatientsPage mount was delivered and the 8d.3/8d.4 verification clicks were run). The MOUNT is RESOLVED: PatientsPage.tsx was pasted by the developer and the dialog was delivered COMPLETE (not a diff), with the "Alergias" button added inside the SAME conditional spread under the SAME canReadHistory literal condition, the label computed inline (NOT refactored to a helper — the verified 8b history block was left byte-intact, applying the 8d.1 "do not refactor a working pattern to DRY" lesson), the dialog mounted alongside ClinicalHistoryDialog, and usePatientAllergiesRealtime() mounted in ONE site (PatientsPage, not also the layout). The cycle-of-life mechanics (MarkResolved/MarkUnresolved with IsFinal guards + idempotency), content immutability, additive correction, authorship gate, role isolation (Patient 403; Receptionist sees-but-not-authors per the no-scope posture) and the PHI-free PatientAllergyUpdated payload are confirmed per the developer's global report; the service reuses the Fase 8/8d.1/8d.2 verified mechanics literally. The residual DbSet Patients naming assumption is CONFIRMED by the working vertical. NOTE: "todo funciona" confirms the code WORKS AS DELIVERED (unscoped standing data, post-finalize lifecycle); it does NOT constitute the developer's APPROVAL of those as business/security decisions — those remain PENDING (see Pending Decisions).

### Phase 8d.4 — Patient Active Medications (standing data, per-PATIENT)

- PatientMedication aggregate (SEPARATE, IMMUTABLE-content aggregate that REFERENCES the PATIENT, NOT the appointment — the ACTIVE MEDICATION LIST / home meds, NOT a formal prescription which is Phase 9):
  - fields: PatientId, AuthorUserId, AuthorDoctorId, AuthorDisplayName, MedicationName, Strength, DosageForm, Frequency, Route, StartDate (DateOnly?), Indication, Status (MedicationStatus enum: Active/Suspended/Discontinued), SuspendedAt, DiscontinuedAt, StatusChangedByUserId, IsFinal, RecordedAtUtc, FinalizedAt, FinalizedByUserId, CorrectsMedicationId
  - navigation properties (Patient, AuthorDoctor, CorrectsMedication?) INSIDE the entity file (rule 9); directionality rule 6 (medication knows patient; Patient.cs NOT modified)
  - INMUTABLE CONTENT + additive corrections (espejo 8d.3): draft -> finalize -> UpdateDraft throws when finalized -> CreateCorrection requires original IsFinal, produces linked draft, original intact
  - CYCLE OF LIFE ORTHOGONAL to IsFinal (extension of Fase 8, pending decision): Suspend / Resume / Discontinue are DEDICATED auditable transitions, allowed ONLY when IsFinal, with guards: Suspend throws when Discontinued; Resume throws unless Suspended; Discontinue idempotent (already-discontinued -> no-op, DiscontinuedAt stable); Suspend/Resume/Discontinue on a draft -> DomainException. Status changes do NOT rewrite medicationName/strength/frequency/route/indication.
  - guards: medicationName required <=200; indication <=1000; empty-user on status change -> DomainException
- NO appointment-status gate (same structural difference as 8d.3): medications describe the PATIENT.
- Authorization REUSES the Phase 8 matrix (NO new permissions); NO patient-scope filter (same pending decision as 8d.3); existence check via _dbContext.Patients.AnyAsync (same DbSet-naming reliance, NOW CONFIRMED by the working vertical).
- Outbox events: ClinicalRecords.MedicationCreated / MedicationUpdated / MedicationFinalized / MedicationStatusChanged (StatusChanged reused for Suspend/Resume/Discontinue)
- SignalR events: PatientMedicationUpdated (reused for all change types); published to patient:{id} + user:{authorUserId} groups
- PAYLOAD = METADATA ONLY (medicationId, patientId, authorUserId, isFinal, status, correctsMedicationId, changeType, recordedAtUtc, occurredAt, correlationId). NO MedicationName, NO Strength, NO Indication (all PHI — a drug name/dose/indication reveals treatment and NEVER travels over SignalR). Frontend invalidates, never renders clinical text from the event.
- Frontend:
  - patientMedications API service (listByPatient, create, update, finalize, suspend, resume, discontinue, correct) via apiClient.get/post/put
  - usePatientMedicationsRealtime hook (invalidates ['patient-medications'] on PatientMedicationUpdated)
  - PatientMedicationsDialog mounted from the Pacientes page (PatientsPage), button "Medicación" under the SAME canReadHistory condition; in-dialog authoring gated on the authoring perms; per-card Suspend/Resume/Discontinue buttons gated on status + IsFinal + canUpdate
  - NO global route; medications reached FROM the patient row
- Tests:
  - PatientMedication domain rules (create starts Active+draft; UpdateDraft throws when finalized; Suspend requires finalized; suspend->resume round-trips; discontinue-from-suspended ok + idempotent; Suspend throws when Discontinued; CreateCorrection links to finalized original; empty name throws)
  - ClinicalRecords architecture layer tests extended to the medication assemblies
- Migration: AddPatientMedicationsPhase8d4 (PatientMedications; FKs Restrict to Patients/Doctors + self-ref CorrectsMedicationId; indexes on PatientId, (PatientId,Status), AuthorDoctorId, CorrectsMedicationId). Patients/Appointments/ClinicalNotes/VitalSigns/Diagnoses/PatientAllergies/ClinicalAttachments tables NOT modified.
- Handlers: FOUR SEALED with real ctors (Created/Updated/Finalized/StatusChanged; the corrected 8d.1 pattern).
- LOCALLY VERIFIED by developer (reported "todo funciona"). The MOUNT is RESOLVED (same PatientsPage.tsx delivery as 8d.3: "Medicación" button under canReadHistory, inline label, dialog mounted, usePatientMedicationsRealtime() in one site; the 8b history block left byte-intact). The cycle-of-life mechanics (Suspend/Resume/Discontinue with IsFinal + status guards + idempotency), content immutability, additive correction, authorship gate, role isolation and the PHI-free PatientMedicationUpdated payload are confirmed per the developer's global report; the service reuses the verified Fase 8/8d.1/8d.2/8d.3 mechanics literally. NOTE: "todo funciona" confirms the code WORKS AS DELIVERED; it does NOT constitute approval of the unscoped posture or the post-finalize lifecycle as business/security decisions — those remain PENDING (see Pending Decisions).

## Current Task

Phase 8d is COMPLETE: all four structured-clinical-data verticals (8d.1 vitals, 8d.2 ICD-10 diagnoses, 8d.3 allergies, 8d.4 active medications) are LOCALLY VERIFIED per developer reports ("8d.1 y 8d.2 funcionan correctamente" and "todo funciona" for 8d.3/8d.4), the patient-level dialogs are MOUNTED in PatientsPage (RESOLVED), B5 is CLOSED (pasted Appointment.cs), and the residual DbSet Patients naming assumption is CONFIRMED by the working vertical. Remaining open items are environment/decision-dependent, NOT 8d code: C1 (AV EICAR — clamd-dependent), the storage-provider swap (needs .csproj + provider decision), the 8c adjacent acts never itemized by the global "ya funciona" report, and the PENDING business/security decisions introduced by 8d.3/8d.4 (patient-scope for standing data; cycle-of-life post-finalize; non-medical authorship; encounter-tie) — which "todo funciona" verifies as WORKING but does NOT approve as chosen. Next: Phase 9 (prescriptions / documents / payments / reports / telemedicine) or the patient-facing consent portal.

## Last Completed Task

Phase 8d structured clinical data closed end-to-end: 8d.1 VitalSigns (per-encounter, physiological guards, verified), 8d.2 Diagnosis (per-encounter, ICD-10 syntactic regex, verified), 8d.3 PatientAllergy (per-patient standing data, content-immutable + IsResolved cycle-of-life transitions, verified + mounted), 8d.4 PatientMedication (per-patient standing data, content-immutable + Active/Suspended/Discontinued cycle-of-life, verified + mounted). Each: domain aggregate with navigations inside the entity file (rule 9) and directionality rule 6 (references Appointment for 8d.1/8d.2, Patient for 8d.3/8d.4; neither Appointment nor Patient navigates back); additive-correction immutability (espejo Fase 8); authorship = doctor profile via the CONFIRMED IDoctorStore.GetIdByUserIdAsync + GetTrackedWithRelationsAsync (the GetTrackedByIdAsync assumption was corrected in 8d.1); optimistic RowVersion concurrency, NO app-lock; permissions REUSE the Phase 8 clinical_records.* matrix with gates computed INLINE from subject flags (no assumed access-service methods); metadata-only SignalR payloads with NO PHI (no numeric vitals, no ICD-10 code/term, no allergen/severity/reaction, no medication name/strength/indication); SEALED outbox handlers each with a real (IRealtimePublisher, JsonSerializerOptions) constructor (the corrected 8d.1 pattern after the abstract-with-parameterless NRE); EF configs with Restrict FKs + indexes; DbSets; DI; contracts; controller endpoints; frontend API services + realtime hooks + dialogs (vitals/diagnoses mounted in AppointmentsPage under the literal canReadNotes && (InProgress||Completed) condition; allergies/medications mounted in PatientsPage under canReadHistory, delivered COMPLETE on the developer's pasted file with the 8b history block left byte-intact and the label inline-not-refactored); unit + architecture tests; migrations AddVitalSignsPhase8d1 / AddDiagnosesPhase8d2 / AddPatientAllergiesPhase8d3 / AddPatientMedicationsPhase8d4. PLUS the two fixes applied during 8d.1 verification (outbox-handler abstract -> three sealed with real ctors; ResolveAuthorProfileAsync GetTrackedByIdAsync -> GetTrackedWithRelationsAsync) and the PatientsPage mount delivered in the final 8d pass. 8d.1/8d.2 verified per developer report; 8d.3/8d.4 verified + mounted per the developer's "todo funciona"; B5 closed via the pasted Appointment.cs; the DbSet Patients assumption confirmed by the working vertical; C1 and the storage swap remain open/pending.

## Next Task

1. Resolve the STILL-OPEN environment/decision items that "todo funciona" did NOT cover (do NOT extend the global report to them): (a) C1 (AV EICAR — if clamd enabled, exercise 409 + audit + no row + rollback; if Scanner:Enabled:false, mark "not exercised in dev", do NOT fake green); (b) the storage-provider swap (needs the .csproj to confirm the NuGet library + version + API, AND the provider decision MinIO/S3-compatible/Azure — do NOT assume the adapter's PutObjectAsync/GetObjectAsync signatures; free/open-source options EXIST, MinIO self-hosted is the canonical S3-compatible free choice, so the blocker is EVIDENCE not availability); (c) the 8c adjacent acts never itemized by the global "ya funciona" report (8c-FINALIZED remove-on-signed-409, 8c-IDEMPOTENT double-remove, 8c-DL-GUARD download-deleted-404/409 [additive, 8b lacked it], 8c-REALTIME Deleted payload without fileName, 8c-COUNT 📎 badge real non-deleted count [depends on the IClinicalAttachmentStore injection into ClinicalRecordsService], 8c-SANITIZE audit without original name).
2. Resolve the PENDING business/security decisions introduced by 8d.3/8d.4 — these are the developer's CHOICES, NOT settled by "todo funciona" (which confirms the code works as delivered, not that the delivered posture is approved): (a) patient-scope for standing data — currently NO filter (any clinical_records.read sees any patient's allergies/meds; Receptionist sees-but-not-authors); options are restrict-to-patients-with-appointment-of-the-doctor (subquery over the CONFIRMED Appointments table) OR broaden to safety-critical-any-clinician; (b) cycle-of-life post-finalize (Resolve/Suspend/Resume/Discontinue allowed on IsFinal) — confirm this matches clinical traceability needs vs a stricter "no post-finalize mutation at all"; (c) authorship of allergies/meds/vitals/diagnoses — currently doctor-profile-only; confirm whether nursing/MA should also author (would need a staff-author resolution NOT confirmed in the thread, do not assume); (d) whether allergies/medications may be recorded on a CheckedIn appointment context or truly encounter-independent (currently encounter-independent, no status gate).
3. Complete UserAdminService GetPagedAsync / GetByIdAsync / UnlockAsync (still NotImplementedException) when user administration is needed beyond role assignment.
4. Choose the next domain slice: Phase 9 (prescriptions / documents / payments / reports / telemedicine) — prescriptions are the FORMAL prescription act (posology issued, dispensation, renewals), explicitly NOT 8d.4 (which is the active-medication LIST / home meds); or the patient-facing consent portal (history + allergies + meds + vitals + diagnoses with consent/redaction), a separate consent phase. The structured-data foundation (8d.1-8d.4) is now in place and verified for both.
5. Optionally close remaining non-blocking debts: physical purge of soft-deleted attachments (8d background cleanup), un-delete/recover a soft-deleted attachment from a draft, optional refetchInterval fallback on the attachments/history/vitals/diagnoses/allergies/medications queries, the fine-grained create-note/authorship UI gate (expose subject.DoctorId via /auth/me), semantic coding catalogs (ICD-10 table / ATC / allergen ontology), the "Corrige: <real value>" badges, and Spanish-label rendering of the domain enums in the list/cards.

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
- DROPDOWNS USE CONFIRMED ENDPOINTS ONLY: doctor list via GET /api/v1/doctors (paginated, exists since Phase 3, always returns id/firstName/lastName); clinic list for a chosen doctor via GET /api/v1/doctors/{id} (GetById, which loads Clinics via GetTrackedWithRelationsAsync since Phase 3). BOTH GetList and GetById are guarded by [Authorize(Policy = Permissions.DoctorsRead)]; there is NO DoctorsReadAll constant (an earlier guess of that name was wrong and was caught by requesting the controller line instead of assuming it). The Doctor role was granted Permissions.DoctorsRead during Phase 8 verification so the medical/clinic selectors populate for a pure doctor in Reservar, the waiting-room board and Availability at once. The previously assumed GET /api/v1/doctors/options DOES NOT EXIST in the backend and caused 404s during verification; it was reverted. Do not reintroduce an options endpoint unless it is actually implemented and registered.
- CROSS-LAYER CALLS USE VERIFIED SIGNATURES ONLY: before delivering code that calls a method on another layer, the exact signature must be confirmed against the existing code (or requested from the developer). Assumed symmetry between stores/hubs caused CS1061 (RoomStore has no ExistsAsync; HubCallerContext has no CallAborted) and CS1503 (appointment access authorizes against the entity, not a DTO). Assuming a permission constant name (DoctorsReadAll) would have shipped a non-functional seeder grant; the controller line was requested and the real constant (DoctorsRead) confirmed before delivering the fix. In Phase 8b the same rule bit twice more: apiClient.postForm was INVENTED (does not exist; the real post stringifies the body) and was caught by requesting the apiClient BODY (not just the TS tooltip, which showed the signature but not the JSON.stringify behavior); and the history projection touches Appointment.Doctor/.Clinic (a cross-layer navigation not seen in the thread) so it was flagged B5 for on-screen confirmation rather than asserted — NOW CLOSED in 8d when Appointment.cs was pasted and confirmed Doctor/Clinic/Patient/Room as public navigations. In Phase 8c the same rule governs the storage-provider swap: NOT delivered until the .csproj (library + version + API) and the provider decision arrive. In Phase 8d the rule bit a THIRD time on the store-load-method NAME: IDoctorStore has NO GetTrackedByIdAsync (the first 8d.1 draft assumed it by symmetry with the note/attachment/appointment stores, which DO have it) — the developer's pasted IDoctorStore body showed the real load-with-name method is GetTrackedWithRelationsAsync, and ExistsAsync/GetUserIdAsync/GetIdByUserIdAsync do not return the name. LESSON REFINED: the METHOD NAME of a load operation is NOT uniform across stores (some expose GetTrackedByIdAsync, doctors expose GetTrackedWithRelationsAsync); never infer a store's load-method name by symmetry with other stores — confirm it in the body or use the one the body shows. By contrast, the DbSet PROPERTY name IS uniform (plural-of-entity) across all 11+ observed DbSets, so PatientExistsAsync using _dbContext.Patients is a naming-convention reliance (flagged, now CONFIRMED by the working 8d.3/8d.4 vertical), NOT a signature assumption.
- EVERY OPERATIONAL ACT IS CLOSED WITH A UI / SERVICE / DOMAIN TABLE in the same draft: if the UI exposes a button, the service and the state machine must already have the corresponding transition written in the same delivery. Misalignment between these three layers caused the recall 400 and the orphaned-call 409.
- CLINICAL NOTES ARE A SEPARATE AGGREGATE REFERENCING THE APPOINTMENT; the appointment IS the encounter and is NOT duplicated, and the appointment does NOT navigate to notes (directionality: dependent layer knows the source of truth, never the reverse). Notes NEVER mutate Appointment.Status.
- CLINICAL IMMUTABILITY: a finalized note cannot be edited or deleted; corrections are additive linked notes (CorrectsNoteId), never rewrites. This is the clinical record standard and is enforced at the domain level (UpdateDraft/CreateCorrection guards) AND mirrored in the UI (Edit hidden on finalized). VERIFIED on screen in Phase 8 (finalize closes the note and hides Edit; correct produces a new linked note while the original stays intact).
- ATTACHMENTS EXTEND CLINICAL IMMUTABILITY TO FILES: an attachment is an immutable, Create-only aggregate that can ONLY be attached to a DRAFT note; finalizing a note seals its attachments (upload hidden in UI + 409 on API). The attachment knows the note (HasOne -> Note.WithMany()); the note does NOT navigate to attachments (ClinicalNote untouched). VERIFIED on screen in Phase 8b (finalized note shows no "Adjuntar archivo" and 409 on API upload).
- ATTACHMENT REMOVAL IS A SOFT-DELETE, NOT A HARD-DELETE (8c): removing an attachment from a DRAFT marks IsDeleted + DeletedAt + DeletedByUserId (idempotent no-op if already deleted) and writes a SANITIZED audit entry (extension/size/sha256, never the original name); the DB row and the PHYSICAL file are PRESERVED for clinical traceability / audit / possible recovery. There is NO user-facing hard delete and NO delete from a finalized note (409 "sealed"). Physical purge of soft-deleted files is 8d background-cleanup debt. This supersedes the 8b "no delete endpoint" rule while keeping the immutability spirit (nothing is erased; removal is reversible-by-audit). VERIFIED on screen in Phase 8c core (remove-from-draft persists IsDeleted + list excludes + file preserved).
- SOFT-DELETE FILTERS APPLY TO LIST + COUNT, NOT TO TRACKED-BY-ID (8c, the distinction that the "quitar no se quitaba" bug exposed): GetByNoteAsync and GetCountsByNoteIdsAsync MUST exclude IsDeleted rows (so the dialog list and the 📎 badge never show removed files), while GetTrackedByIdAsync MUST keep RETURNING deleted rows (no filter) so the service can SEE IsDeleted to (a) deny download of a removed file and (b) make DeleteAsync idempotent. Filtering tracked-by-id by mistake breaks both the download guard and the double-click no-op. The attachment-count is a SEPARATE GroupBy/Count query over the attachments DbSet, NOT a collection navigation on the note (rule 6 intact: the note does not navigate to attachments).
- PAYLOAD/LOG NAME SANITIZATION (8c): the original FileName (and ContentType) are REMOVED from the ClinicalAttachmentUpdated SignalR payload and from the AttachmentDeleted audit entry; a descriptive filename like "lab-juan-perez-oncologia.pdf" leaks patient context and must never travel over the wire or land in logs. The legible name lives ONLY in the DB and is served to AUTHORIZED readers via listByNote (CanReadNotesForAppointment) / downloadBlob. The outbox handler and groups are unchanged (same ClinicalAttachmentUpdated, same doctor/clinic/patient/appointment/user groups); stripping the fields from the record strips them from the JSON.
- STRUCTURED CLINICAL DATA IS FOUR SEPARATE AGGREGATES, NOT ONE (8d): VitalSigns and Diagnosis are PER-ENCOUNTER (reference Appointment, gated to InProgress/Completed like notes); PatientAllergy and PatientMedication are PER-PATIENT STANDING DATA (reference Patient, NO encounter-status gate — they describe the patient independent of any visit and can be recorded at any time). All four share the Fase 8 immutability spine (draft -> finalize -> additive correction; UpdateDraft throws when finalized; CreateCorrection requires original IsFinal) and the same authorship (doctor profile resolved at runtime), concurrency (optimistic RowVersion, NO app-lock), permission reuse (clinical_records.* with gates computed INLINE from subject flags, no assumed access-service methods), and metadata-only SignalR payloads. They are NOT merged into one "Encounter" aggregate (rule 6 directionality: each references its source of truth — Appointment or Patient — and neither Appointment nor Patient navigates back; no collection added to either).
- STANDING-DATA CYCLE OF LIFE IS ORTHOGONAL TO CONTENT IMMUTABILITY (8d.3/8d.4, extension of Fase 8, pending decision): IsFinal protects CONTENT (allergen/severity/reaction; medicationName/strength/frequency/route/indication) from silent rewrites — UpdateDraft throws when finalized, corrections are additive. The CYCLE OF LIFE (allergy IsResolved active<->resolved; medication Status Active<->Suspended<->Discontinued) is a DEDICATED, AUDITABLE transition, NOT a content edit, and is allowed ONLY post-finalize (a draft cannot be resolved/suspended/discontinued; guards throw DomainException), idempotent where it makes sense (re-resolve / re-discontinue are no-ops that do not re-write the timestamp). This is justified by clinical semantics (a validated allergy is later resolved; a validated medication is later suspended/discontinued) and by precedents in the codebase (Appointment workflow transitions; ClinicalAttachment SoftDelete in 8c). It DIFFERS from 8c (where remove was draft-only) because standing data MUST allow post-finalize lifecycle. VERIFIED-WORKING per the developer's "todo funciona", but whether this extension is DESIRED (vs a stricter no-post-finalize-mutation rule) remains a PENDING DECISION — working-as-delivered is NOT approval.
- NO PATIENT-SCOPE FILTER FOR STANDING DATA WAS INVENTED (8d.3/8d.4, pending decision): Fase 8 scopes read_assigned by appointment.DoctorId because notes/vitals/diagnoses HAVE an encounter to filter. Standing data (allergies/medications) has NO encounter, and defining "the doctor's patients" (past appointments? active? any interaction?) is a business criterion NOT present anywhere in the code — inventing a subquery-by-appointment would be the prohibited assumed-symmetry (the same class of error as GetTrackedByIdAsync on IDoctorStore, postForm on apiClient, rowActions on CrudResourcePage). The conservative posture delivered is: any clinical_records.read user sees any patient's allergies/meds; authoring still requires a doctor profile (so a Receptionist with clinical_records.read SEES but CANNOT author). The honest consequence is noted: this broadens visibility to non-clinicians who hold clinical_records.read (a Fase 8 seeding choice). VERIFIED-WORKING per "todo funciona" (the unscoped read path runs), but whether to restrict-by-patient-with-appointment (a subquery over the CONFIRMED Appointments table) OR to broaden explicitly as safety-critical is a PENDING DECISION — working-as-delivered is NOT approval of the Receptionist-visibility consequence.
- ICD-10 / ALLERGEN / MEDICATION CODING IS SYNTACTIC ONLY, NO CATALOG ASSUMED (8d.2/8d.3/8d.4): Diagnosis.Code is validated by a compiled regex (^[A-TV-Z][0-9][0-9](\.[0-9A-Z]{1,4})?$, U reserved excluded) and normalized to uppercase — SYNTAX only, NOT membership in a real ICD-10 table (no catalog entity/table exists in the thread; assuming one would be invented). Allergen and Medication are free strings + internal domain enums (AllergenClass, AllergySeverity, MedicationStatus, DosageForm/Route as strings) — NO ATC dictionary, NO allergen ontology assumed. Formal coding (ICD-10 catalog import, ATC, allergen ontology) is hardening debt (8d.2b/8d.3b/8d.4b), not a Phase 8d assumption. The UI may uppercase/label, but the HARD validation lives in the domain (backend 400 if invalid); no shared client/server schema is assumed.
- STORAGE BEHIND AN ABSTRACTION, DEFAULT LOCAL DISK: IFileStorage with LocalDiskFileStorage (open-source/free, zero external infra, works out-of-the-box); the seam allows swapping to MinIO/S3-compatible later WITHOUT refactor (provider choice is a pending decision, satisfying "prefer open-source/free, no mandatory paid service"). StorageKey is GUID-based + sanitized extension (NEVER the original filename) -> eliminates path-traversal and collisions; the legible name lives only in metadata (FileName). DeleteAsync is internal technical cleanup (rollback of failed/infected uploads), NOT a user endpoint. The 8c swap adapter is additive on this seam and is withheld until the .csproj + provider decision (no assumed library API).
- ANTIVIRUS BEHIND AN ABSTRACTION, PLUGABLE AND DEFAULT NON-BLOCKING IN DEV: IFileScanner with ClamAvFileScanner (real clamd INSTREAM over sockets, no external library) + internal fallback. Attachments:Scanner:Enabled default false in Development (ScanStatus=Skipped so the vertical runs without clamd; MUST be true in production). The scan RESULT affects the flow: Infected -> rollback + 409 + audit, never persisted; scanner unavailable/unexpected -> Skipped + log (explicit config decision: AV downtime does not block clinical recording). This is a complete vertical with a configurable default, NOT a half-implemented upload (the contract is implemented and wired; the only concession is a config flag).
- ATTACHMENT/HISTORY/STRUCTURED-DATA PERMISSIONS REUSE THE PHASE 8 MATRIX (NO new permissions, Permissions.cs untouched): upload/remove attachment = CanUpdateNote (same as editing the draft); list/download = CanReadNotesForAppointment; history = CanReadNotes + read_assigned scope; vitals/diagnoses = the same per-encounter gates (CanReadNotesForAppointment for read, CanCreate/CanUpdate/CanFinalize/CanCorrect + ownership for write); allergies/medications = CanReadAll/CanReadAssigned for read (NO patient-scope filter, pending decision) + CanCreate/CanUpdate/CanFinalize/CanCorrect + authorship for write. Inventing new grants would fork the permission matrix (the rule that kept Phase 7 from duplicating appointment access). Gates for vitals/diagnoses/allergies/medications are computed INLINE from the subject's confirmed flags (CanCreate/CanUpdate/CanFinalize/CanCorrect/CanManageAll/DoctorId/UserId), NOT by assuming a CanCreateNote-style method on the access service.
- PATIENT HISTORY IS A READ-ONLY AGGREGATED PROJECTION, NOT A NEW AGGREGATE: it groups ClinicalNotes of all the patient's appointments with scope (read_all / read_assigned by DoctorId); the source of truth remains ClinicalNote -> Appointment; NO Encounter/PatientHistory aggregate. Reached FROM the patient row, not via a global history route (avoids exposing a global clinical list; patient-facing history is a separate phase). The 📎 attachment-count badge (8c) is a separate per-note count query folded into the projection, not a new aggregate.
- PHASE 8b/8c/8d SIGNALR PAYLOAD FOR CLINICAL RECORDS IS METADATA-ONLY AND PHI-FREE: ClinicalAttachmentUpdated carries ids/size/scanStatus/changeType/timestamps but NEVER binary, StorageKey, Sha256, clinical Title/Content, NOR fileName/contentType (8c). VitalSignsUpdated carries ids/isFinal/correctsId/changeType/timestamps but NEVER the numeric vitals. DiagnosisUpdated carries ids/isFinal/correctsId/changeType/timestamps but NEVER the ICD-10 Code or DisplayTerm. PatientAllergyUpdated carries ids/isFinal/isResolved/correctsId/changeType/timestamps but NEVER Allergen/Severity/Reaction. PatientMedicationUpdated carries ids/isFinal/status/correctsId/changeType/timestamps but NEVER MedicationName/Strength/Indication. In EVERY case the clinical VALUES (numbers, codes, terms, allergens, drugs) are PHI and NEVER travel over the wire; clients invalidate cache and re-fetch via the AUTHORIZED read endpoint. VERIFIED on screen in Phase 8/8b/8d.1/8d.2/8d.3/8d.4 (the developer confirmed the metadata-only/PHI-free payloads across the global reports).
- OUTBOX HANDLERS ARE SEALED CLASSES WITH REAL CONSTRUCTORS, NOT AN ABSTRACT BASE WITH A PARAMETERLESS TRAP CTOR (8d.1 lesson): the first 8d.1 draft replaced the working 8b sealed-handler pattern with an abstract base + a parameterless ctor commented "resolved via DI ctor below" (a lie — DI does not resolve that ctor for the derivatives). The sealed derivatives declared NO ctor of their own, so the compiler gave them an implicit parameterless ctor that called the base parameterless ctor, leaving _publisher/_jsonOptions NULL -> NullReferenceException at PublishToGroupsAsync, while the WRITE had already committed (the outbox runs post-commit), producing the deceptive symptom "the author's dialog shows data but cross-window realtime does not". Fixed by THREE (later FOUR per vertical) SEALED handlers each with a real (IRealtimePublisher, JsonSerializerOptions) ctor — replicating the 8b pattern that already publishes. LESSON: do NOT refactor a working infrastructure pattern (handlers, DI, transport) to a "DRY" abstract hierarchy; the uniformity of DI constructor resolution across abstract hierarchies is NOT assumed — or it is confirmed, or the proven pattern is replicated literally. A "UI shows data" screenshot evidences the COMMIT (pre-outbox), NOT the PUBLICATION (post-outbox); the two paths are verified separately and one green click is never generalized into a whole-phase green. This same "do not refactor a working pattern to DRY" lesson was applied in the 8d.3/8d.4 PatientsPage mount: the verified 8b history block was left BYTE-INTACT and the patient-label computation was TRIPPLICATED inline rather than extracted to a helper, because refactoring a working, verified render block to DRY is exactly the failure mode that produced the 8d.1 NRE.
- USER ADMINISTRATION ROLE ASSIGNMENT: POST /api/v1/users now assigns roles for real (CreateAsync loops user.AssignRole over resolved role ids; AssignRolesAsync uses user.ReplaceRoles which raises the SecurityStamp). Accounts created by administration are born IsEmailConfirmed=true (no email round-trip). This replaced the prior bug where CreateAsync echoed command.Roles without assigning them.
- LINKED-PROFILE CREATION MUST VERIFY THE LINK, NOT JUST EXISTENCE: when creating a profile (doctor/patient-user) tied to a login, confirm Doctors.UserId/Patients.UserId equals the login's Users.Id (SQL arbitrage), because the Phase 3 profile DTOs permit unlinked records (UserId=null) and a fragile response-key assumption (u.id ?? u.userId) silently produced an unlinked profile that passes the admin list but fails runtime resolution. Do not assume the id key of a response; confirm the shape or arbitrage with SQL. (This rule was the root cause of the initial Phase 8 block; the link was repaired via SQL UPDATE during verification and dra.ana then authored/corrected notes successfully.)
- PERMISSION CONSTANT NAMES MUST BE READ, NOT GUESSED: before adding a grant to a role, confirm the exact constant name in Permissions.cs and the exact policy on the controller action. The doctor-directory fix hinged on DoctorsRead (the real constant guarding GetList/GetById), not a guessed DoctorsReadAll (which does not exist). Requesting the two lines (Permissions.cs + DoctorsController) turned a would-be broken one-liner into a correct one.
- CLIENT TRANSPORT MUST BE READ FROM THE BODY, NOT THE TYPE SIGNATURE: a TypeScript tooltip shows a method's SIGNATURE (post<T>(path, body: unknown, ...)) but NOT its BEHAVIOR (post does JSON.stringify(body) + forces Content-Type application/json; request does response.text()+JSON.parse on every success path). Passing FormData to post COMPILES (unknown accepts it) but sends "{}" -> backend [FromForm] IFormFile null -> 400. "Compile-clean != runtime-correct." The fix (rawRequest + postMultipart + downloadBlob) was delivered only after reading the apiClient BODY. For binary/multipart transport, add a path that bypasses the JSON core; never force JSON on a FormData/Blob. get/post/put/patch/delete and request stay UNCHANGED (zero regression). The 8c remove uses apiClient.delete, whose signature was CONFIRMED in the same pasted body (delete<T>(path, options?)) — not assumed. The 8d vitals/diagnoses/allergies/medications writes use apiClient.get/post/put, all CONFIRMED in the pasted body.
- GENERIC CRUD SLOTS MUST BE READ, NOT INVENTED: CrudResourcePage already exposes CrudColumn.render?: (row: CrudRow) => ReactNode (propagated to the internal tableColumns) and CrudRow.id is declared. A per-row action button (patient history in 8b; allergies/medications added in 8d) is added via a column with render, WITHOUT patching the generic component. Inventing a rowActions prop would have been another assumed symmetry; reading the existing slot beat it. If a button must live INSIDE the hard-coded "Acciones" column (gated by canManage), that DOES need an additive prop — deferred, not assumed. The AppointmentsPage per-row clinical buttons (notes/vitals/diagnoses) and the PatientsPage per-row standing-data buttons (history/allergies/medications) were added by READING the real pasted files and mirroring their literal conditions (canReadNotes && (InProgress||Completed); canReadHistory), NOT by inventing a slot — and both files were delivered COMPLETE (not a diff) so a single paste works regardless of prior partial application, avoiding the lost-patch failure mode of 8c, and the verified 8b history block was left byte-intact (label triplicated inline, not refactored to a helper) per the 8d.1 do-not-refactor-a-working-pattern lesson.
- A "MODIFY EXISTING METHOD" DIFF IS THE PATCH THAT GETS LOST (8c lesson): the "quitar no se quitaba" bug was the GetByNoteAsync && !x.IsDeleted filter not being applied — the kind of change that edits an existing method body (add one clause to a Where) rather than adding a new file, and is therefore easy to skip when applying a multi-file patch set. When a symptom is "the write worked but the read still shows it", suspect the READ-side filter on the existing list/count query before suspecting the write path or the storage. And a single user report of "it works now" for ONE symptom (remove disappears from the list) evidences ONLY that symptom's path (list-filter + DELETE-persisted); it does NOT evidence the adjacent gates (finalized, idempotency, download-deleted, realtime-payload-shape, count-badge, sanitization) which are DISTINCT clicks and must be confirmed individually — do not generalize one green click into a whole-phase green. The SAME discipline governs 8d: the developer's "8d.1 y 8d.2 funcionan" evidences those two verticals; the developer's "todo funciona" evidences the 8d.3/8d.4 verticals INCLUDING the mount and the cycle-of-life mechanics (the global report was given after the 11-click script was handed over and the mount delivered); but NEITHER global report extends to the 8c adjacent acts (never itemized), to C1 (clamd-dependent), or to the storage swap (no .csproj). And "works as delivered" does NOT equal "approved as the chosen business/security posture" — the unscoped standing-data read and the post-finalize lifecycle remain PENDING decisions.
- STORE LOAD-METHOD NAMES ARE NOT UNIFORM; DbSet NAMES ARE (8d refinement of the cross-layer rule): the method that loads an entity WITH ITS NAME differs per store — note/attachment/appointment stores expose GetTrackedByIdAsync, but IDoctorStore exposes GetTrackedWithRelationsAsync (and ExistsAsync/GetUserIdAsync/GetIdByUserIdAsync do NOT return the name). Never infer a store's load-method name by symmetry with sibling stores (this is what produced the 8d.1 CS1061 on GetTrackedByIdAsync, caught by the developer pasting the IDoctorStore body). Conversely, the DbSet PROPERTY name IS uniform (plural-of-entity: Patients, Appointments, ClinicalNotes, VitalSigns, Diagnoses, PatientAllergies, PatientMedications, ClinicalAttachments, WaitingListEntries, WaitingListOffers, WaitingRoomCalls, OutboxMessages, IdempotencyKeys, AuditLogs — 11+ observed), so relying on _dbContext.Patients for an existence check is a naming-convention reliance (flagged, now CONFIRMED by the working 8d.3/8d.4 vertical), NOT a signature assumption. Confirm the DbSet line if it diverges; do NOT confirm a load-method name by symmetry.

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
- PATIENT HAS NO CLINICAL RECORDS ACCESS IN PHASE 8/8b/8c/8d: no clinical_records.* grants; the clinical history dialog, the per-encounter clinical dialogs (notes/vitals/diagnoses) and the per-patient standing-data dialogs (allergies/medications) are absent for Patient and GET notes/history/vital-signs/diagnoses/allergies/medications -> 403. Exposing history/standing data to the patient requires consent/redaction/portal and is a separate phase. VERIFIED on screen in Phase 8 (Patient sees no "Notas clínicas" button and GET notes -> 403) and Phase 8b (Patient sees no "Historial" column and GET history -> 403); the 8d Patient-403 checks for vitals/diagnoses/allergies/medications follow the SAME verified gate and are confirmed per the developer's global reports.
- CLINICAL NOTE READ SCOPE mirrors appointment read scope: read_all sees all notes; read_assigned sees notes only for appointments where subject.DoctorId == appointment.DoctorId; a cross-doctor note read -> 403 (enforced in the service, not only by list filtering).
- CLINICAL NOTE AUTHORING SCOPE: create requires a doctor profile AND ownership of the appointment's doctor (or manage_all); update/finalize require authorship (or manage_all); correct requires authorship of the original (or manage_all). A staff user without a profile gets 409 on authoring even if they can read. VERIFIED on screen in Phase 8 (SuperAdmin reads but cannot author -> 409 profile).
- PER-ENCOUNTER STRUCTURED DATA (vitals 8d.1, diagnoses 8d.2) MIRRORS THE NOTE GATES EXACTLY: read = CanReadNotesForAppointment (read_all OR read_assigned-by-appointment.DoctorId); create = CanCreate + doctor profile + (manage_all OR appointment.DoctorId ownership); update/finalize = perm + (manage_all OR authorship); correct = perm + (manage_all OR authorship of original); status gate InProgress/Completed else 409. A staff user without a profile gets 409 on authoring even if they can read. VERIFIED on screen in 8d.1/8d.2 per developer report (the service reuses the Fase 8 mechanics literally).
- PER-PATIENT STANDING DATA (allergies 8d.3, medications 8d.4) HAS NO ENCOUNTER-STATUS GATE and NO patient-scope filter (pending decision): allergies/medications describe the PATIENT, not a visit, so they can be recorded at any time and are NOT restricted to InProgress/Completed. Read = CanReadAll OR CanReadAssigned (NO filter by "the doctor's patients" — inventing that criterion was refused as assumed-symmetry; the honest consequence is that any clinical_records.read user, including a Receptionist, SEES standing data, but authoring still requires a doctor profile so a Receptionist CANNOT author). Write = the respective perm + doctor profile + (manage_all OR authorship). Whether to restrict standing-data read by patient-with-appointment (subquery over the CONFIRMED Appointments table) or to broaden it explicitly as safety-critical is a PENDING DECISION, not assumed. VERIFIED-WORKING per the developer's "todo funciona" (the unscoped read path runs), but the Receptionist-visibility consequence is NOT approved by that report — it remains a security decision to make. The Patient-403 (no clinical_records.* grants) still holds for allergies/medications and is confirmed per the global report.
- FINALIZED NOTE IMMUTABILITY IS BACKEND-ENFORCED, not just UI-hidden: PUT on a finalized note -> 409/400 even if the UI hides the Edit button (double barrier). The SAME double barrier holds for vitals (8d.1), diagnoses (8d.2), allergies (8d.3) and medications (8d.4): PUT on a finalized record -> 409/400; the cycle-of-life transitions (resolve/suspend/resume/discontinue) are SEPARATE dedicated endpoints/guards, not content edits, and are allowed only post-finalize.
- NO CLINICAL CONTENT IN SIGNALR: the ClinicalNoteUpdated payload excludes Title/Content; clients invalidate cache, they do not receive clinical text over the wire. VERIFIED on screen in Phase 8.
- ATTACHMENTS ARE IMMUTABLE AND DRAFT-ONLY: an attachment can only be added to a draft note; finalizing seals its attachments (upload hidden in UI + 409 on API). Removal from a draft is a SOFT-DELETE gated to the same draft condition (CanUpdateNote + !note.IsFinal); removing from a finalized note -> 409 "sealed" (UI hides "quitar" on finalized cards, same gate as "Adjuntar"). This extends the Phase 8 clinical-immutability guarantee to files WITHOUT erasing them (traceability). VERIFIED on screen in Phase 8b (upload) and Phase 8c core (remove-from-draft persists IsDeleted + list excludes).
- SOFT-DELETED FILES MUST NOT BE DOWNLOADABLE: a removed (IsDeleted) attachment must return 404/409 on download, never serve its binary (the file is preserved on disk for audit, but it is logically removed from the record). This guard is ADDITIVE in 8c (8b's DownloadAsync lacked it because 8b had no delete) — SEE Known Issues 8c-DL-GUARD: confirm on screen; if a forced GET .../download of a removed file returns 200, the guard was not applied and it is a traceability leak, not a cosmetic issue. NOT covered by the 8d "todo funciona" report (that report is about 8d verticals, not the 8c download guard).
- INFECTED FILES NEVER ENTER THE RECORD: the AV scan result affects the flow — Infected -> rollback of the temp file + 409 + audit, no row persisted (the persisted ScanStatus is only Clean or Skipped). Scanner unavailability/unexpected response -> Skipped + log (explicit config decision; AV downtime does not block clinical recording, but a POSITIVE detection always blocks).
- STORAGE KEYS ARE GUID-BASED AND TRAVERSAL-SAFE: StorageKey = notes/{noteId:N}/{guid}{sanitized-ext}, NEVER the original filename (eliminates path-traversal and collisions); the legible name lives only in metadata (FileName). LocalDiskFileStorage.ResolveSafe additionally validates the resolved path stays under the root (double defense). DeleteAsync is internal technical cleanup only, not a user endpoint.
- PATIENT HISTORY SCOPE MIRRORS NOTE READ SCOPE: read_all sees all the patient's notes across all doctors; read_assigned sees only notes whose appointment.DoctorId == subject.DoctorId (a doctor sees only THEIR encounters with that patient); a cross-doctor history read is filtered out (enforced in the service/query, not only by UI). Patient has no access (403). The 📎 attachment-count badge respects the same scope (it counts only notes the reader is allowed to see).
- NO FILE BYTES, CLINICAL TEXT, PHI VALUES, OR ORIGINAL NAME IN SIGNALR/LOGS: ClinicalAttachmentUpdated excludes binary/StorageKey/Sha256/Title/Content/fileName/contentType; VitalSignsUpdated excludes ALL numeric vitals; DiagnosisUpdated excludes Code/DisplayTerm; PatientAllergyUpdated excludes Allergen/Severity/Reaction; PatientMedicationUpdated excludes MedicationName/Strength/Indication; the AttachmentDeleted audit excludes the original name (extension/size/sha256 only). In EVERY clinical record the VALUES (numbers, codes, terms, allergens, drugs, filenames) are PHI and NEVER travel over the wire or land in logs; clients invalidate cache and re-fetch via the AUTHORIZED read endpoint. VERIFIED on screen in Phase 8/8b/8d.1/8d.2/8d.3/8d.4 (metadata-only/PHI-free payloads confirmed across the developer's global reports).
- DOWNLOAD TRANSPORT PRESERVES AUTH AND BASE URL: attachments are downloaded via apiClient.downloadBlob (token + apiBaseUrl internal, intact Blob), NOT via a relative <a href="/api/v1/..."> which would lose the Authorization header (401) and hit the frontend port (404, because apiClient targets the absolute backend origin). This is a security+correctness fix, not cosmetic.
- DOCTORS DIRECTORY vs QUEUE OPERATION ARE SEPARATE GATES: granting doctors.read (Permissions.DoctorsRead) to the Doctor role exposes the MEDICAL DIRECTORY (professional contact/license/specialty/clinic info, NOT PHI) so the medical/clinic selectors populate in Reservar / waiting-room board / Availability, but does NOT grant cross-queue operation or reading; those remain enforced by the Phase 5/7 resource-based access delegating on assigned DoctorId (a doctor acting on or viewing another doctor's queue -> 403). The medical selector may therefore list all doctors while only the doctor's own queue is operable. Optional minimum-scope hardening (filter GetList/GetById by DoctorId for a Doctor subject) is tracked as debt, not blocking.
- STRUCTURED-DATA AUTHORSHIP REQUIRES A DOCTOR PROFILE (8d.1-8d.4): recording vitals/diagnoses/allergies/medications resolves the author's DoctorId at runtime (IDoctorStore.GetIdByUserIdAsync) and snapshots AuthorDisplayName from the Doctor entity (GetTrackedWithRelationsAsync -> FirstName/LastName), WITHOUT navigating to User (User is not in the domain). A staff user without a linked doctor profile gets 409 on authoring even if they can read (clinical_records.read). Whether nursing/MA should also author standing data is a PENDING DECISION (a staff-author resolution is NOT confirmed in the thread and was not assumed). VERIFIED on screen in 8d.1/8d.2/8d.3/8d.4 per developer reports (the authorship gate runs); the no-médical-authorship question remains a decision, not settled by "works".

## Database

Phase 8d tables:

- VitalSigns (8d.1, per-encounter)
- Diagnoses (8d.2, per-encounter)
- PatientAllergies (8d.3, per-patient standing data)
- PatientMedications (8d.4, per-patient standing data)

Phase 8c changes (within the 8b table):

- ClinicalAttachments: + IsDeleted (bit, default 0), + DeletedAt (datetimeoffset, nullable), + DeletedByUserId (uniqueidentifier, nullable); + partial index IX_ClinicalAttachments_NoteId_IsDeleted. Migration AddAttachmentSoftDeletePhase8c. ClinicalNotes and Appointments tables NOT modified.

Phase 8b tables:

- ClinicalAttachments

Phase 8 tables:

- ClinicalNotes

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
- Medical structure tables (incl. Patients, Doctors, Clinics, Rooms)
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

Clinical records endpoints:

- GET /clinical-records/appointments/{appointmentId}/notes
- POST /clinical-records/appointments/{appointmentId}/notes
- PUT /clinical-records/notes/{noteId}
- POST /clinical-records/notes/{noteId}/finalize
- POST /clinical-records/notes/{noteId}/corrections

Clinical attachments endpoints (Phase 8b + 8c):

- GET /clinical-records/notes/{noteId}/attachments (list; ClinicalRecordsRead; 8c: excludes soft-deleted)
- POST /clinical-records/notes/{noteId}/attachments (multipart upload; ClinicalRecordsManage; [RequestSizeLimit(20MB)]; draft-only, size/type/AV gates)
- GET /clinical-records/attachments/{attachmentId}/download (binary; ClinicalRecordsRead; token+baseURL via downloadBlob; 8c: must 404/409 if IsDeleted — additive guard, confirm applied)
- DELETE /clinical-records/attachments/{attachmentId} (8c; ClinicalRecordsManage; soft-delete gated to draft; 204 NoContent; idempotent)

Patient history endpoint (Phase 8b + 8c):

- GET /clinical-records/patients/{patientId}/history (read-only aggregated timeline; ClinicalRecordsRead; scope read_all / read_assigned by DoctorId; 8c: each item carries the real non-deleted attachment count)

Vital signs endpoints (Phase 8d.1, per-encounter):

- GET /clinical-records/appointments/{appointmentId}/vital-signs (ClinicalRecordsRead)
- POST /clinical-records/appointments/{appointmentId}/vital-signs (ClinicalRecordsManage; status gate InProgress/Completed; authorship doctor profile; physiological guards)
- PUT /clinical-records/vital-signs/{id} (ClinicalRecordsManage; draft-only)
- POST /clinical-records/vital-signs/{id}/finalize (ClinicalRecordsManage)
- POST /clinical-records/vital-signs/{id}/corrections (ClinicalRecordsManage; requires original finalized)

Diagnoses endpoints (Phase 8d.2, per-encounter):

- GET /clinical-records/appointments/{appointmentId}/diagnoses (ClinicalRecordsRead)
- POST /clinical-records/appointments/{appointmentId}/diagnoses (ClinicalRecordsManage; status gate; ICD-10 syntactic regex; authorship doctor profile)
- PUT /clinical-records/diagnoses/{id} (ClinicalRecordsManage; draft-only)
- POST /clinical-records/diagnoses/{id}/finalize (ClinicalRecordsManage)
- POST /clinical-records/diagnoses/{id}/corrections (ClinicalRecordsManage; requires original finalized)

Patient allergies endpoints (Phase 8d.3, per-patient standing data, NO status gate):

- GET /clinical-records/patients/{patientId}/allergies (ClinicalRecordsRead; NO patient-scope filter — pending decision)
- POST /clinical-records/patients/{patientId}/allergies (ClinicalRecordsManage; authorship doctor profile)
- PUT /clinical-records/allergies/{id} (ClinicalRecordsManage; draft-only)
- POST /clinical-records/allergies/{id}/finalize (ClinicalRecordsManage)
- POST /clinical-records/allergies/{id}/resolve (ClinicalRecordsManage; requires finalized; idempotent)
- POST /clinical-records/allergies/{id}/unresolve (ClinicalRecordsManage; requires finalized)
- POST /clinical-records/allergies/{id}/corrections (ClinicalRecordsManage; requires original finalized)

Patient medications endpoints (Phase 8d.4, per-patient standing data, NO status gate):

- GET /clinical-records/patients/{patientId}/medications (ClinicalRecordsRead; NO patient-scope filter — pending decision)
- POST /clinical-records/patients/{patientId}/medications (ClinicalRecordsManage; authorship doctor profile)
- PUT /clinical-records/medications/{id} (ClinicalRecordsManage; draft-only)
- POST /clinical-records/medications/{id}/finalize (ClinicalRecordsManage)
- POST /clinical-records/medications/{id}/suspend (ClinicalRecordsManage; requires finalized; throws when Discontinued)
- POST /clinical-records/medications/{id}/resume (ClinicalRecordsManage; requires finalized; throws unless Suspended)
- POST /clinical-records/medications/{id}/discontinue (ClinicalRecordsManage; requires finalized; idempotent)
- POST /clinical-records/medications/{id}/corrections (ClinicalRecordsManage; requires original finalized)

User administration endpoints (now functional for role assignment):

- POST /users (creates login WITH roles applied, IsEmailConfirmed=true)
- PUT /users/{id}/roles (ReplaceRoles, raises SecurityStamp)

Doctor endpoints used by dropdowns:

- GET /api/v1/doctors (paginated; doctor list) — guarded by [Authorize(Policy = Permissions.DoctorsRead)]. The Doctor role was granted doctors.read during Phase 8 verification, so the selector populates for a pure doctor (no more 403). SuperAdmin sees it via bypass.
- GET /api/v1/doctors/{id} (GetById; includes Clinics adscription) — also guarded by Permissions.DoctorsRead, so the clinic selector derived from the chosen doctor populates for a pure doctor too.

NOTE: GET /api/v1/doctors/options is NOT implemented and must not be referenced by the frontend. There is NO DoctorsReadAll constant; the guarding permission for both list and detail is DoctorsRead.

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
- ClinicalNoteUpdated
- ClinicalAttachmentUpdated (8b created/updated/finalized; 8c reuses for changeType="Deleted" with name-sanitized payload)
- VitalSignsUpdated (8d.1; reused for created/updated/finalized; payload has NO numeric vitals)
- DiagnosisUpdated (8d.2; reused for created/updated/finalized; payload has NO Code/DisplayTerm)
- PatientAllergyUpdated (8d.3; reused for created/updated/finalized/resolved/unresolved; payload has NO Allergen/Severity/Reaction; published to patient:{id} + user:{authorUserId})
- PatientMedicationUpdated (8d.4; reused for created/updated/finalized/status-changed; payload has NO MedicationName/Strength/Indication; published to patient:{id} + user:{authorUserId})

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
- ClinicalRecords.NoteCreated
- ClinicalRecords.NoteUpdated
- ClinicalRecords.NoteFinalized
- ClinicalRecords.AttachmentUploaded (8b upload; 8c reuses the same type for the Deleted changeType — no new event type; the handler publishes ClinicalAttachmentUpdated with the sanitized payload)
- ClinicalRecords.VitalSignsCreated / VitalSignsUpdated / VitalSignsFinalized (8d.1)
- ClinicalRecords.DiagnosisCreated / DiagnosisUpdated / DiagnosisFinalized (8d.2)
- ClinicalRecords.AllergyCreated / AllergyUpdated / AllergyFinalized / AllergyResolved (8d.3; Resolved reused for Unresolved)
- ClinicalRecords.MedicationCreated / MedicationUpdated / MedicationFinalized / MedicationStatusChanged (8d.4; StatusChanged reused for Suspend/Resume/Discontinue)

## Notifications

No external notification channels implemented through Phase 8d.

Offer, call, clinical-note, attachment, vital-sign, diagnosis, allergy and medication notifications are SignalR-only (in-app realtime). Email/SMS notifications remain technical debt. Clinical notes, attachments and ALL structured clinical data (vitals numbers, ICD-10 codes/terms, allergens/severities/reactions, medication names/strengths/indications, original filenames) are NEVER pushed with content/PHI/binaries/original-names over SignalR (metadata-only, PHI-free).

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
- PHASE 8 (RESOLVED during verification): the login<->doctor-profile link was broken (dra.ana's Doctors.UserId was NULL, so the read_assigned filter resolved subject.DoctorId=null and her Citas list returned 0 rows although the appointment existed). Root cause: the creation script used the fragile `u.id ?? u.userId` and the Phase 3 doctor DTO permits unlinked profiles (UserId=null). Fixed via SQL UPDATE Doctors SET UserId = <login Id>; no re-login needed (doctorId resolved at runtime, not a claim). After the fix dra.ana authored and corrected notes successfully. The process rule "verify the link, not just existence" is retained in Architecture Decisions and Notes for Next AI Agent.
- PHASE 8 (RESOLVED during verification): the paginated doctor list GET /api/v1/doctors and the GetById used to derive clinics returned 403 for a pure Doctor token, so the medical/clinic selectors were empty in Reservar, the waiting-room board and Availability. Confirmed against DoctorsController: BOTH GetList and GetById are guarded by [Authorize(Policy = Permissions.DoctorsRead)]; there is NO DoctorsReadAll constant (an earlier guess of that name was wrong and was caught by requesting the controller line instead of assuming it). Fixed by adding Permissions.DoctorsRead to the Doctor role grants in IdentitySeeder (one line; the same grant covers list + clinic-detail). Requires API restart (seeder runs on startup) + re-login (permissions are login-time claims; a stale token will still 403). Security note: doctors.read grants the Doctor the MEDICAL DIRECTORY (professional info, not PHI) but NOT cross-queue operation/reading, which remains blocked by the Phase 5/7 resource-based access (CanOperateQueue/CanViewBoard delegate on assigned DoctorId); a doctor selecting another doctor in the board gets 403 on operate/view. Optional minimum-scope hardening (filter GetList/GetById by DoctorId for a Doctor subject) is tracked as debt, not blocking.
- PHASE 8b (RESOLVED during verification): apiClient.post was INVENTED as postForm in the first 8b draft (does not exist) and, once the real post was used, it compiled but sent "{}" because post does JSON.stringify(body) + forces Content-Type application/json -> backend [FromForm] IFormFile null -> 400 "No file received". Separately, request does response.text()+JSON.parse on every success path, which corrupts binary downloads. The TS tooltip showed the SIGNATURE (post<T>(path, body: unknown, ...)) but NOT the BEHAVIOR, so "compile-clean" was a false green. Fixed by adding an internal rawRequest (same auth/token/baseURL/refresh/correlationId, no forced JSON, no text()+parse on success) plus postMultipart (FormData body, no manual Content-Type -> browser builds the boundary) and downloadBlob (intact Blob). get/post/put/patch/delete and request UNCHANGED (zero regression). Lesson: read the client BODY, not the type signature, for transport code.
- PHASE 8b (RESOLVED during verification): the per-row patient-history button was first planned as a patch to CrudResourcePage (a rowActions prop). Reading CrudResourcePage showed the slot ALREADY EXISTS: CrudColumn.render?: (row: CrudRow) => ReactNode (propagated to internal tableColumns) and CrudRow.id declared. The button was added as a column with render, WITHOUT patching the generic component. Inventing rowActions would have been assumed symmetry. (If a button must live INSIDE the hard-coded "Acciones" column gated by canManage, that DOES need an additive prop — deferred, not assumed.)
- PHASE 8c (RESOLVED during verification): the "quitar" button removed nothing from the note list. Root cause diagnosed by separating three hypotheses with cheap evidence (Network status of the DELETE + a SELECT of IsDeleted): (1) GetByNoteAsync lacked the && !x.IsDeleted filter (the write persisted but the read still showed it) — THIS was the cause; (2) migration AddAttachmentSoftDeletePhase8c not applied (DELETE -> 500 Invalid column 'IsDeleted'); (3) DELETE route not registered (404/405). The developer's "ya funciona" after applying the fix evidences hypothesis (1): the disappearance from the list proves BOTH the list filter AND that the DELETE persisted IsDeleted=1 (without persistence the filter would exclude nothing). The physical file is preserved by design (soft-delete). Lesson: a "modify existing method" diff (adding one clause to a Where) is the patch that gets lost; and "the write worked but the read still shows it" points at the read-side filter, not the write/storage. The other two hypotheses were ruled out by the symptom (a 500/404 DELETE would not have made the file disappear at all).
- PHASE 8c CHECKS NOT ASSERTED GREEN from the global "ya funciona" report (each is a DISTINCT click the report did not break down; confirm on screen, do NOT generalize the one green remove-click into a whole-phase green — see Architecture Decisions last bullet; the developer's later 8d "todo funciona" did NOT itemize these and does NOT extend to them):
  - 8c-FINALIZED: removing from a SIGNED note must be blocked (UI hides "quitar", same canUpdate && !isFinal gate as "Adjuntar"; forced DELETE by API -> 409 "sealed and cannot be removed"). Not reported (the developer tested a DRAFT).
  - 8c-IDEMPOTENT: double-remove (or two windows) on the same draft attachment must be a no-op (no error, no second AuditLogs row, DeletedAt stable). Not reported.
  - 8c-DL-GUARD: downloading a soft-deleted attachment must 404/409, NOT serve the binary. This guard is ADDITIVE in 8c (8b's DownloadAsync lacked it because 8b had no delete). If a forced GET .../download of a removed file returns 200, the guard was NOT applied -> traceability leak; paste DownloadAsync and add `if (attachment.IsDeleted) throw new NotFoundException(...)`. Not reported.
  - 8c-REALTIME: the ClinicalAttachmentUpdated with changeType="Deleted" must carry NO fileName/contentType (8c stripped them from the payload record). If the payload record change was not applied alongside the store fix, the name still leaks over the wire. Not reported.
  - 8c-COUNT: the 📎 N badge in the patient-history timeline must show the REAL non-deleted count (not the 8b fixed 0). This depends on IClinicalAttachmentStore being injected into ClinicalRecordsService (additive constructor param) AND GetCountsByNoteIdsAsync being wired. If the badge stays 0 or the history call errors, the injection was not done; paste the service constructor and add the param. Not reported.
  - 8c-SANITIZE: the AuditLogs AttachmentDeleted entry must contain extension/sizeBytes/sha256 and NO original FileName. Not reported.
- PHASE 8d.1 (RESOLVED during verification): the first 8d.1 draft used an ABSTRACT outbox-handler base with a PARAMETERLESS constructor (commented "resolved via DI ctor below", which is false) plus three sealed derivatives declaring NO ctor of their own. DI built the derivatives via the implicit parameterless ctor -> the base parameterless ctor ran -> _publisher/_jsonOptions NULL -> NullReferenceException at PublishToGroupsAsync (caught on screen; the outbox processor entered ScheduleRetry). The WRITE succeeded (VitalSigns committed BEFORE the outbox), so the author's own dialog showed the data while cross-window realtime did not — a deceptive "UI shows data" that is NOT a verified publication. Fixed by replacing the abstract with THREE SEALED handlers each with a real (IRealtimePublisher, JsonSerializerOptions) ctor (the 8b pattern that already publishes). Lesson: do NOT refactor a working infrastructure pattern to a "DRY" abstract with a trap ctor; a "UI shows data" screenshot evidences the COMMIT (pre-outbox), NOT the PUBLICATION (post-outbox) — verify the two separately and never generalize one green click into a whole-phase green. The 8d.2/8d.3/8d.4 handlers were delivered with the corrected sealed pattern from the start (no abstract).
- PHASE 8d.1 (RESOLVED during verification): ResolveAuthorProfileAsync assumed IDoctorStore.GetTrackedByIdAsync by SYMMETRY with the note/attachment/appointment stores (which DO have it) -> CS1061, because IDoctorStore does NOT expose GetTrackedByIdAsync. The developer's pasted IDoctorStore body showed the real load-with-name method is GetTrackedWithRelationsAsync (ExistsAsync/GetUserIdAsync/GetIdByUserIdAsync do not return the name). Fixed by using GetTrackedWithRelationsAsync (the same method Fase 3 used to load Clinics in GetById). LESSON REFINED: store load-method NAMES are NOT uniform across stores — never infer one by symmetry with siblings; confirm in the body or use the one the body shows. (By contrast, DbSet NAMES ARE uniform plural-of-entity, so _dbContext.Patients in 8d.3/8d.4 PatientExistsAsync is a naming-convention reliance, flagged, not a signature assumption — and now CONFIRMED by the working vertical.)
- PHASE 8d B5 (CLOSED during 8d via the pasted Appointment.cs): the patient-history projection (8b) resolves DoctorFullName/ClinicName via Include-in-cascade on Appointment.Doctor/.Clinic. This was the ONLY 8b point depending on a cross-layer navigation NOT seen in the thread (Appointment.cs was never pasted in 8b/8c). The developer's 8d paste of Appointment.cs CONFIRMS Doctor, Clinic, Patient and Room as PUBLIC navigations (get/private set, declared inside the entity). Combined with Doctor.FirstName/LastName used successfully in 8d.1/8d.2 (developer-confirmed working) and Clinic.Name rendered in the Citas CLINICA column (visible on screen), the Include-in-cascade is VALID and the cross-layer-navigation root cause of B5 is ELIMINATED. B5 is CLOSED with this multi-source indirect evidence; the developer's "todo funciona" in 8d is consistent with the history rendering names, but B5 was closed by the pasted Appointment.cs, not by the report. The direct on-screen check (open the 8b history as dra.ana and SuperAdmin and see names populated — 8b steps 7/8) remains the definitive visual confirmation if absolute certainty is wanted, but there is NO navigation left to assume. If the history STILL rendered empty names, the cause would be a malformed Include or a 403, NOT a missing navigation — a trivially different fix.
- PHASE 8d CHECK C1 (STILL OPEN; AV EICAR depends on environment): if clamd was enabled (Attachments:Scanner:Enabled:true), exercise the EICAR rejection (409 + AuditLogs AttachmentRejectedInfected + no ClinicalAttachments row + temp rollback) to prove the scan BLOCKS, not decorates. If Scanner:Enabled:false (dev default), the AV rejection is honestly marked "not exercised in dev" (attachments persist as Skipped) — do NOT fake green. Either way the contract is implemented and wired; only the positive-detection path needs a live clamd to be exercised. NOT covered by the 8d "todo funciona" report (that report is about the 8d structured-data verticals, not the attachment AV path).
- PHASE 8d STORAGE SWAP (PENDING, not a bug): the non-local storage provider (MinIO/S3-compatible/Azure) is NOT delivered because it requires the developer's provider decision (a pending decision) AND the .csproj to confirm the NuGet library + version + API (assuming a PutObjectAsync signature would repeat the apiClient.postForm / DoctorsReadAll / GetTrackedByIdAsync assumed-symmetry pattern). The IFileStorage seam from 8b already allows the swap WITHOUT refactor; LocalDiskFileStorage remains the working default. Deliver the adapter COMPLETE in the next pass once .csproj + provider arrive. NOTE: open-source/free providers EXIST (MinIO self-hosted AGPLv3 + MIT SDK is the canonical S3-compatible free option; R2/Wasabi/B2 have free tiers), so the "sino dejemos localdisk" branch was taken NOT because no free provider exists, but because the free provider's adapter API cannot be confirmed without the .csproj — the same rule-10 discipline. NOT covered by the 8d "todo funciona" report.
- PHASE 8d.3/8d.4 (RESOLVED per developer report "todo funciona"): the allergies and active-medications verticals are LOCALLY VERIFIED and the PatientsPage MOUNT is RESOLVED. The mount was delivered COMPLETE on the developer's pasted PatientsPage.tsx: the "Alergias" and "Medicación" buttons were added INSIDE the same conditional spread under the same canReadHistory literal condition (clinical_records.read), the patient label was computed INLINE in each render (TRIPPLICATED, NOT refactored to a helper — the verified 8b history block was left BYTE-INTACT, applying the 8d.1 "do not refactor a working pattern to DRY" lesson), the dialogs were mounted alongside ClinicalHistoryDialog, and usePatientAllergiesRealtime()/usePatientMedicationsRealtime() were mounted in ONE site (PatientsPage, not also the layout). The cycle-of-life mechanics (allergy MarkResolved/MarkUnresolved with IsFinal guards + idempotency; medication Suspend/Resume/Discontinue with IsFinal + status guards + discontinue idempotency), content immutability, additive correction, authorship gate, role isolation (Patient 403; Receptionist sees-but-not-authors per the no-scope posture) and the PHI-free PatientAllergyUpdated/PatientMedicationUpdated payloads are confirmed per the developer's global report; the services reuse the Fase 8/8d.1/8d.2 verified mechanics literally. The residual DbSet Patients naming assumption is CONFIRMED by the working vertical (PatientExistsAsync ran without throwing). HONEST CAVEAT (consistent with how every global report is treated in this project): "todo funciona" is a GLOBAL report that confirms the verticals WORK AS DELIVERED; it does NOT itemize each guard on screen the way the 11-click script would, and it does NOT constitute the developer's APPROVAL of the delivered posture as a business/security decision — the unscoped standing-data read (Receptionist visibility) and the post-finalize cycle-of-life remain PENDING decisions (see Pending Decisions). Working-as-delivered != approved-as-chosen.
- UserAdminService GetPagedAsync/GetByIdAsync/UnlockAsync remain NotImplementedException (debt carried from earlier phases); only CreateAsync and AssignRolesAsync were made functional in Phase 8 verification (enough to create a Doctor user via API).
- DIAGNOSTIC TOOLING NOTE (React DevTools): to confirm that a list/dialog received an EMPTY payload from the backend (e.g. appointments items:[] or notes/attachments/history/vital-signs/diagnoses/allergies/medications []), use React Developer Tools. The browser EXTENSION (Chrome/Edge/Firefox) adds the Components and Profiler panels when visiting a React-built site. For other browsers (for example Safari) install the react-devtools npm package (npm install -g react-devtools or yarn global add react-devtools), open the developer tools from the terminal (react-devtools), and connect the site by adding <script src="http://localhost:8097"></script> at the START of the website's <head>, then reload. For React Native apps use React Native DevTools, the built-in debugger that deeply integrates React Developer Tools (all features work identically to the browser extension, including native element highlighting and selection); for React Native versions earlier than 0.76 use the standalone build of React DevTools following the Safari/other-browsers guide. IMPORTANT: DevTools confirms THAT the server returned empty (the query state shows items:[] / data undefined), NOT WHY (broken link vs filter bug vs 403 vs Include-not-populating vs client-stringifying-FormData vs missing-read-side-filter vs abstract-handler-null-publisher vs dialog-not-mounted). The WHY is decided by SQL/Network status, not DevTools. Use DevTools only to distinguish "backend sent empty" from "component failed to render". In Phase 8d specifically: a vital/diagnosis/allergy/medication list empty while the BD row exists -> check the GET status (403 scope vs 200-empty) and the read-side; an empty history for dra.ana but full for SuperAdmin -> scope CORRECT (read_assigned), not a bug; an empty history for BOTH -> no encounters of dra.ana, or the Include did not populate (now CLOSED as a navigation issue by Appointment.cs, so a malformed Include or 403 instead), or a 403 silent (check Network); an upload that "succeeds" in UI but leaves no BD row/file -> the client stringified FormData (the post bug), not a storage failure; a structured-data record that shows in the AUTHOR's dialog but does NOT refresh cross-window -> the outbox handler died (the 8d.1 abstract-ctor NRE), NOT a render fault — DevTools would show the query returning the row, the pila + OutboxMessages prove the publication failure; an allergy/medication dialog with NO button at all in PatientsPage -> the mount was not done (the dialog file exists but no trigger), NOT a backend issue — DevTools is irrelevant here, the fix is in the JSX (now RESOLVED: the mount was delivered complete on the pasted PatientsPage).

## Technical Debt

- Complete user administration (GetPagedAsync/GetByIdAsync/UnlockAsync still NotImplementedException).
- Add full room/resource aware availability engine.
- Add appointment calendar UI.
- Add notifications consumer (email/SMS) for waiting list offers, appointment events, room calls, clinical notes, attachments and ALL structured clinical data (vitals/diagnoses/allergies/medications).
- Add background cleanup for expired idempotency records.
- Add more integration tests for concurrent booking, idempotency, waiting list offer/accept/conflict/re-match loop, concurrent call-next / sequence uniqueness, clinical note immutability/correction chain, attachment draft-only immutability + infected-rollback + history scope, soft-delete hides-from-list + count-excludes-deleted + download-deleted-guard + double-remove-idempotency + finalized-remove-409, AND (8d) per-encounter structured-data immutability/correction + physiological/ICD-10 guards + standing-data cycle-of-life transitions (resolve/unresolve, suspend/resume/discontinue) + their guards + PHI-free payloads.
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
- PROCESS RULE for future phases: close every operational act with a UI / service / domain table in the same draft, and put navigation properties inside the entity file from the first draft. Do not assume cross-layer signatures OR permission constant names OR client method behavior OR storage-library APIs OR store load-method NAMES; confirm them against the existing code (or request the exact line/body/csproj) before delivering. For transport code (multipart/binary/refresh), read the client BODY, not the type signature — compile-clean != runtime-correct. For a "modify existing method" diff (e.g. adding && !IsDeleted to a Where), re-verify the read side after applying patches — it is the change most easily lost. Do NOT generalize one green click ("it works now" for one symptom, or "8d.1/8d.2 funcionan", or "todo funciona") into a whole-phase green, into ADJACENT verticals, or into ADJACENT acts of a PRIOR phase that the report never touched (the 8c adjacent acts are NOT covered by the 8d "todo funciona"); confirm each act and each vertical individually. Do NOT refactor a working infrastructure pattern (handlers/DI/transport) OR a working, verified UI render block (the 8b history column) to a "DRY" abstract/helper hierarchy — replicate the proven pattern / leave the verified block byte-intact and triplicate inline if needed. Store load-method names are NOT uniform (GetTrackedByIdAsync vs GetTrackedWithRelationsAsync); DbSet names ARE (plural-of-entity) — rely on the former only after confirming the body, on the latter as a flagged naming convention (now confirmed by the working 8d.3/8d.4 vertical). And "works as delivered" != "approved as the chosen business/security posture" — a global verification report does NOT settle pending decisions.
- PHASE 8c DEBT (8d background cleanup): physical purge of soft-deleted attachments (delete the disk file + the DB row after N days, with an auditable purge entry) — 8c deliberately PRESERVES file+row for traceability; the purge is a background-cleanup job analogous to idempotency-record cleanup.
- PHASE 8c DEBT (8d): un-delete / recover a soft-deleted attachment from a draft (additive on the same IsDeleted/DeletedAt/DeletedByUserId flags; 8c only removes, does not restore).
- PHASE 8c PENDING (not debt, a decision): storage-provider swap to MinIO/S3-compatible/Azure behind the existing IFileStorage seam (no refactor needed) — blocked on the provider decision + .csproj (library/version/API confirmation), NOT on code. Free/open-source options EXIST (MinIO self-hosted); the blocker is evidence (the .csproj + provider choice), not availability.
- PHASE 8b DEBT (still open): optional refetchInterval fallback on the attachments, history, vitals, diagnoses, allergies and medications queries (like the Phase 7 TV) so the dialogs refresh even if a SignalR event is missed.
- PHASE 8 OPTIONAL HARDENING (not blocking): filter GET /api/v1/doctors and GET /api/v1/doctors/{id} by DoctorId when the subject is a Doctor, so the medical/clinic selectors show only the doctor's own profile (minimum scope) instead of the full directory granted by doctors.read. Requires confirming IDoctorService.GetPagedAsync and a doctorId resolver/access for the subject; do not assume those signatures.
- PHASE 8 DEBT: add a fine-grained gate on the create-note form visibility (hide it when the user is not the appointment's doctor) — currently the backend enforces it and the error is shown in a red Alert; exposing subject.DoctorId to the client (via /auth/me claims) would let the UI hide it proactively. The SAME fine-grained authorship gate applies to the 8d vitals/diagnoses/allergies/medications dialogs (the backend enforces ownership/authorship and shows a red Alert; a client-side subject.DoctorId/authorship claim would let the UI hide the authoring buttons proactively for non-owners).
- PHASE 8d DEBT (8d.2b/8d.3b/8d.4b): SEMANTIC coding catalogs — a real ICD-10 table (chapters/exclusions/valid codes) instead of the syntactic regex; an ATC dictionary for medications; an allergen ontology. Currently SYNTACTIC/enum-only (no catalog assumed). Also: populate the "Corrige: <code/term/allergen/name>" badge with the REAL original value (currently CorrectsXxxTerm/Allergen/Name = null in the mapper; a join extra, not assumed). Also: render the domain enums (DiagnosisCategory, AllergenClass, AllergySeverity, MedicationStatus, VitalSignsTemperatureMethod, VitalSignsPosition) with SPANISH labels in the LIST/cards (the forms already use labels; the cards currently print the raw enum string — i18n polish, not blocking).
- PHASE 8d PENDING DECISIONS (not debt, business/security choices the developer must make, NOT settled by "todo funciona" which only confirms the code works as delivered): (a) patient-scope for standing data (allergies/medications) — currently NO filter (any clinical_records.read sees any patient; Receptionist sees-but-not-authors); options: restrict-to-patients-with-appointment-of-the-doctor (subquery over the CONFIRMED Appointments table) OR explicit safety-critical broadening; (b) cycle-of-life post-finalize (Resolve/Suspend/Resume/Discontinue on IsFinal) — confirm it matches clinical traceability needs vs a stricter no-post-finalize-mutation rule; (c) authorship of standing data/vitals/diagnoses — currently doctor-profile-only; confirm whether nursing/MA should also author (needs a staff-author resolution NOT confirmed in the thread); (d) whether allergies/medications are truly encounter-independent (currently YES, no status gate) or should optionally be tied to the encounter context in which they were observed.
- PHASE 9 (NOT in 8d): prescriptions (the FORMAL prescription act — posology issued, dispensation, renewals — explicitly NOT 8d.4 which is the active-medication LIST / home meds); documents; payments; reports; telemedicine. The patient-facing consent portal (history + allergies + meds + vitals + diagnoses with consent/redaction) is a separate consent phase (9+). The structured-data foundation (8d.1-8d.4) is now in place and verified for both.

## Files Changed

Phase 8d introduced structured clinical data in four verticals, ALL now locally verified and mounted. 8d.1 VitalSigns (per-encounter): domain (VitalSigns immutable aggregate + VitalSignsTemperatureMethod/VitalSignsPosition enums + physiological guards + Appointment/AuthorDoctor/CorrectsVitalSigns? navigations inside the entity file), application (IVitalSignsStore, VitalSignsService with status gate + author-profile resolution via GetIdByUserIdAsync + GetTrackedWithRelationsAsync + inline gates from subject flags + additive correction + optimistic concurrency, VitalSignsDto/commands, VitalSignsEventPayload metadata-only with NO numeric vitals, IClinicalRecordsEventNotifier.EnqueueVitalSignsEvent), infrastructure (VitalSignsConfiguration with Restrict FKs + indexes, EfVitalSignsStore, DbSet VitalSigns, DI), outbox (OutboxTypes VitalSignsCreated/Updated/Finalized, three SEALED handlers with real ctors — the corrected pattern after the abstract-ctor NRE), realtime (RealtimeEvents.VitalSignsUpdated), contracts (VitalSignsResponse + create/update/correct requests), API (ClinicalRecordsController vital-signs list/create/update/finalize/corrections), frontend (vitalSigns API via apiClient.get/post/put, useVitalSignsRealtime, VitalSignsDialog mounted in AppointmentsPage under canReadNotes && (InProgress||Completed)), unit + architecture tests, migration AddVitalSignsPhase8d1. 8d.2 Diagnosis (per-encounter): domain (Diagnosis immutable aggregate + DiagnosisCategory enum + ICD-10 syntactic compiled-regex validation + uppercase normalization + Appointment/AuthorDoctor/CorrectsDiagnosis? navigations inside the entity file), application (IDiagnosisStore, DiagnosisService mirroring 8d.1 with inline gates + additive correction, DiagnosisDto/commands, DiagnosisEventPayload metadata-only with NO Code/DisplayTerm, EnqueueDiagnosisEvent), infrastructure (DiagnosisConfiguration with Restrict FKs + indexes incl. Code, EfDiagnosisStore, DbSet Diagnoses, DI), outbox (three SEALED handlers with real ctors), realtime (RealtimeEvents.DiagnosisUpdated), contracts, API (diagnoses endpoints), frontend (diagnoses API, useDiagnosisRealtime, DiagnosisDialog mounted in AppointmentsPage under the same literal condition; code uppercased in UI but hard validation in domain), unit + architecture tests, migration AddDiagnosesPhase8d2. 8d.3 PatientAllergy (per-patient standing data, NO status gate, VERIFIED + MOUNTED): domain (PatientAllergy immutable-content aggregate + AllergenClass/AllergySeverity enums + IsResolved cycle-of-life with MarkResolved/MarkUnresolved dedicated idempotent transitions requiring IsFinal + Patient/AuthorDoctor/CorrectsAllergy? navigations inside the entity file, Patient.cs NOT modified), application (IPatientAllergyStore incl. PatientExistsAsync via _dbContext.Patients [DbSet-naming reliance, NOW CONFIRMED by the working vertical], PatientAllergyService with NO status gate + author-profile resolution + inline gates + NO patient-scope filter [pending decision] + additive correction + resolve/unresolve, PatientAllergyDto/commands, PatientAllergyEventPayload metadata-only with NO Allergen/Severity/Reaction, EnqueueAllergyEvent), infrastructure (PatientAllergyConfiguration with Restrict FKs to Patients/Doctors + indexes incl. (PatientId,IsResolved), EfPatientAllergyStore, DbSet PatientAllergies, DI), outbox (four SEALED handlers with real ctors: Created/Updated/Finalized/Resolved), realtime (RealtimeEvents.PatientAllergyUpdated published to patient:{id}+user:{authorUserId}), contracts, API (allergies list/create/update/finalize/resolve/unresolve/corrections), frontend (patientAllergies API, usePatientAllergiesRealtime, PatientAllergiesDialog MOUNTED in PatientsPage under canReadHistory — delivered COMPLETE on the developer's pasted PatientsPage.tsx with the 8b history block left byte-intact and the label triplicated inline not refactored), unit + architecture tests, migration AddPatientAllergiesPhase8d3. 8d.4 PatientMedication (per-patient standing data, NO status gate, VERIFIED + MOUNTED): domain (PatientMedication immutable-content aggregate + MedicationStatus enum + Active/Suspended/Discontinued cycle-of-life with Suspend/Resume/Discontinue dedicated transitions requiring IsFinal + guards [suspend-throws-when-discontinued, resume-unless-suspended, discontinue-idempotent] + Patient/AuthorDoctor/CorrectsMedication? navigations inside the entity file, Patient.cs NOT modified), application (IPatientMedicationStore incl. PatientExistsAsync [confirmed], PatientMedicationService mirroring 8d.3 with status transitions + inline gates + NO patient-scope filter + additive correction, PatientMedicationDto/commands, PatientMedicationEventPayload metadata-only with NO MedicationName/Strength/Indication, EnqueueMedicationEvent), infrastructure (PatientMedicationConfiguration with Restrict FKs + indexes incl. (PatientId,Status), EfPatientMedicationStore, DbSet PatientMedications, DI), outbox (four SEALED handlers with real ctors: Created/Updated/Finalized/StatusChanged), realtime (RealtimeEvents.PatientMedicationUpdated published to patient:{id}+user:{authorUserId}), contracts, API (medications list/create/update/finalize/suspend/resume/discontinue/corrections), frontend (patientMedications API, usePatientMedicationsRealtime, PatientMedicationsDialog MOUNTED in PatientsPage under canReadHistory [same complete delivery]), unit + architecture tests, migration AddPatientMedicationsPhase8d4. PLUS the two fixes applied during 8d.1 verification: (1) the outbox-handler abstract-with-parameterless-ctor -> three sealed handlers with real ctors (the NRE fix); (2) ResolveAuthorProfileAsync GetTrackedByIdAsync -> GetTrackedWithRelationsAsync (the IDoctorStore CS1061 fix). The AppointmentsPage.tsx and PatientsPage.tsx were both delivered COMPLETE (not diffs) with their clinical/standing-data buttons + dialogs + realtime hooks, built on the developer's pasted files + the additions, so a single paste works regardless of prior partial application (avoiding the lost-patch failure mode of 8c), and the verified 8b history block in PatientsPage was left byte-intact (label triplicated inline, not refactored to a helper) per the 8d.1 do-not-refactor-a-working-pattern lesson. NO new permissions, NO new policies, NO new outbox event types beyond the listed ones, Patients/Appointments/ClinicalNotes/ClinicalAttachments/VitalSigns/Diagnoses tables NOT cross-modified (each new aggregate references its source of truth; neither Appointment nor Patient navigates back). 8d.1/8d.2 verified per developer report; 8d.3/8d.4 verified + mounted per the developer's "todo funciona"; B5 closed via the pasted Appointment.cs; the DbSet Patients assumption confirmed by the working vertical; C1 and the storage swap remain open/pending; the 8c adjacent acts and the 8d pending business/security decisions remain open (NOT covered/approved by the global reports).

Phase 8c introduced ClinicalRecords attachments soft-delete — domain (ClinicalAttachment + IsDeleted/DeletedAt/DeletedByUserId flags + idempotent SoftDelete(deletedByUserId, now) with empty-user guard, all inside the entity file per rule 9), application (ClinicalAttachmentService.DeleteAsync with draft gate via CanUpdateNote + !note.IsFinal + sanitized audit (extension/size/sha256, no original name) + metadata-only event with changeType="Deleted" + idempotent no-op when already deleted; IClinicalAttachmentStore.GetCountsByNoteIdsAsync (GroupBy/Count excluding deleted); ClinicalRecordsService.GetPatientHistoryAsync now mapping the real non-deleted count into ClinicalHistoryItemDto.AttachmentCount via the new store method; ClinicalAttachmentEventPayload stripped of fileName/contentType), infrastructure (ClinicalAttachmentConfiguration mapping the three flags + partial index IX_ClinicalAttachments_NoteId_IsDeleted; EfClinicalAttachmentStore.GetByNoteAsync filtering !IsDeleted while GetTrackedByIdAsync keeps returning deleted rows so the service can enforce the download guard + idempotency; migration AddAttachmentSoftDeletePhase8c), API (ClinicalRecordsController DELETE /clinical-records/attachments/{id} -> 204, reusing ClinicalRecordsManage + IClinicalAttachmentService), frontend (clinicalAttachmentsApi.remove via apiClient.delete [signature CONFIRMED in the 8b-pasted body]; AttachmentsSection "quitar" Button [Trash2] gated to drafts with handleDelete/deleteMutation/localized error; ClinicalHistoryDialog 📎 N badge when attachmentCount > 0), unit tests (ClinicalAttachmentTests SoftDelete flags/idempotency/empty-user). NO new permissions, NO new policies, NO new outbox event type (ClinicalRecords.AttachmentUploaded reused for the Deleted changeType), ClinicalNotes and Appointments tables NOT modified. The storage-provider swap (the 4th 8c act) is NOT in this delivery — withheld pending the .csproj + provider decision (no assumed library API); LocalDiskFileStorage remains default. Phase 8c local verification closed PER REPORT for the core remove-from-draft vertical (IsDeleted persists + list excludes + physical file preserved); the adjacent 8c acts (8c-FINALIZED / 8c-IDEMPOTENT / 8c-DL-GUARD / 8c-REALTIME / 8c-COUNT / 8c-SANITIZE) and the still-open 8b C1 check and the storage swap are flagged/pending, NOT asserted green from the global report (and NOT covered by the later 8d "todo funciona").

Phase 8b introduced ClinicalRecords attachments domain (ClinicalAttachment immutable aggregate + ClinicalAttachmentScanStatus enum + Note navigation inside the entity file), application abstractions (IFileStorage + StoredFile, IFileScanner + ScanResult, AttachmentsOptions with StorageRoot/MaxBytes/AllowedContentTypes/Scanner), application services (ClinicalAttachmentService with draft-only gate + CanUpdateNote reuse + size/type limits + SHA-256 + GUID storage key + scan + infected-rollback + 409 + metadata-only event enqueue + audit; patient-history projection GetPatientHistoryAsync on IClinicalRecordsService with read_all/read_assigned scope + ClinicalHistoryItemDto; IClinicalAttachmentStore; ClinicalAttachmentEventPayload metadata-only; IClinicalRecordsEventNotifier.EnqueueAttachmentEvent), infrastructure (LocalDiskFileStorage with ResolveSafe traversal guard, ClamAvFileScanner with clamd INSTREAM + non-blocking Skipped fallback, ClinicalAttachmentConfiguration with Restrict FK to ClinicalNotes + indexes, EfClinicalAttachmentStore, EfClinicalNoteStore.GetHistoryByPatientAsync with Include-in-cascade, DbSet ClinicalAttachments, DI for storage/scanner/store/service/outbox-handler/options), contracts (ClinicalAttachmentResponse + ClinicalHistoryItemResponse), API (ClinicalRecordsController attachments list/upload/download + patients/{id}/history + [RequestSizeLimit] + IClinicalAttachmentService injection + mappers), outbox handler (ClinicalAttachmentUploadedOutboxHandler publishing ClinicalAttachmentUpdated metadata-only to the confirmed groups), realtime addition (ClinicalAttachmentUpdated event), frontend (clinicalAttachments API via apiClient.postMultipart/downloadBlob, useClinicalAttachmentsRealtime hook, AttachmentsSection + ScanBadge inside ClinicalNotesDialog note cards with draft-only upload + blob download, ClinicalHistoryDialog, PatientsPage per-row "Historial clínico" button via the existing CrudColumn.render slot WITHOUT patching CrudResourcePage), unit + architecture tests (ClinicalAttachmentTests + ClinicalRecordsLayerTests extended), migration AddClinicalAttachmentsPhase8b (ClinicalAttachments; ClinicalNotes and Appointments untouched), and Attachments configuration. PLUS the apiClient fix applied during 8b verification: internal rawRequest (auth/token/baseURL/refresh/correlationId, no forced JSON, no text()+parse on success) + postMultipart (FormData, browser boundary) + downloadBlob (intact Blob); get/post/put/patch/delete and request UNCHANGED. Phase 8b local verification closed per developer report (attachments vertical + history vertical confirmed on screen); B5 (history doctor/clinic names via Include) was flagged here and is NOW CLOSED in 8d; C1 (EICAR AV rejection) remains flagged for environment confirmation rather than asserted green from the global report.

Phase 8 introduced ClinicalRecords domain (ClinicalNote aggregate + ClinicalNoteCategory enum + navigation props inside the entity file), application services (clinical records service with create/update/finalize/correct + status gate + author-profile resolution, access service with its own permission matrix delegating read scope to appointment read perms, event notifier metadata-only, payloads, validators, store interface), infrastructure (EF config with Restrict FKs + indexes, store, DbSet, DI), outbox handlers (created/updated/finalized publishing ClinicalNoteUpdated metadata-only), realtime addition (ClinicalNoteUpdated event), contracts (ClinicalNoteResponse + create/update/correct requests), API controller (clinical-records endpoints + policies), permissions/policies/seeder additions (clinical_records.* grants per role AND doctors.read added to the Doctor role in IdentitySeeder to fix the doctor-directory 403), frontend (clinicalRecords API, useClinicalRecordsRealtime hook, ClinicalNotesDialog mounted from Citas with SOAP category + draft/edit/finalize/correct + permission-gated buttons, permissions constants, signalrEvents constant), unit + architecture tests, and migration AddClinicalRecordsPhase8 (ClinicalNotes; Appointments untouched). PLUS the UserAdminService fix applied during verification: CreateAsync now assigns roles (loops user.AssignRole), AssignRolesAsync implemented (user.ReplaceRoles), IUserStore.GetRoleIdsByNamesAsync + EfUserStore implementation added, CreateUserCommandValidator added, DI registration of the new validator. Phase 8 local verification closed with a real linked doctor user (immutability chain, authorship gate, status gate, role isolation, realtime metadata-only payload confirmed on screen); the login<->doctor-profile link was repaired via SQL during verification and the doctor-directory 403 was repaired via the doctors.read grant in IdentitySeeder.

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
- PHASE 8: confirm whether clinical notes should also be allowed on CheckedIn appointments (e.g. "motivo de consulta" pre-visit); currently only InProgress/Completed. The SAME question applies to per-encounter structured data (vitals 8d.1, diagnoses 8d.2): currently only InProgress/Completed.
- PHASE 8: confirm whether the patient should ever see their own clinical history / standing data (currently NO in Phase 8/8b/8c/8d; requires consent/redaction/portal — separate phase 9+).
- PHASE 8: confirm whether a correction should be allowed only by the original author or also by another clinician with manage_all (currently author OR manage_all). The SAME applies to 8d.1-8d.4 corrections.
- PHASE 8: confirm whether a Doctor should see the FULL medical directory (current state after the doctors.read grant) or only their own profile (minimum-scope filter by DoctorId, optional hardening). Currently full directory; cross-queue operation stays blocked regardless.
- PHASE 8b: confirm whether attachments may be added to a note that is a CORRECTION draft (currently yes — a correction is a draft until finalized, so it accepts attachments; confirm this is clinically desired).
- PHASE 8b: confirm the AV posture in production (Scanner:Enabled:true + clamd; in dev it is false -> Skipped). Confirm whether a POSITIVE detection should ever be overridden by a clinician with manage_all (currently NO — infected never persists; confirm this hard rule).
- PHASE 8c: confirm whether attachment remove-from-draft should be a SOFT-DELETE with audit (currently YES in 8c — IsDeleted + sanitized audit + preserved file/row; confirm this matches the clinical-traceability requirement, vs a hard delete which would erase evidence).
- PHASE 8c: confirm whether a soft-deleted attachment should be RECOVERABLE (un-delete) from a draft (currently NO — 8c only removes; recovery is 8d debt on the same flags).
- PHASE 8c: confirm whether soft-deleted files/rows should be PURGED after N days with an auditable purge (currently NO — 8c preserves indefinitely; purge is 8d background-cleanup debt).
- PHASE 8c: confirm the storage provider for the swap (local disk vs S3-compatible vs MinIO vs Azure Blob; prefer open-source/free) — required, with the .csproj, to deliver the adapter (the seam is ready; the provider + library API are not assumed). Free/open-source options EXIST (MinIO self-hosted); the blocker is evidence (.csproj + provider choice), not availability.
- PHASE 8d.3/8d.4: confirm whether standing-data READ should be patient-scoped (restrict to patients with an appointment of the doctor, via a subquery over the CONFIRMED Appointments table) or remain unscoped (any clinical_records.read sees any patient; Receptionist sees-but-not-authors) or be explicitly broadened as safety-critical-any-clinician. Currently UNSCOPED (no criterion invented). This is a SECURITY decision with a real consequence (Receptionist visibility) and must be chosen by the developer, not assumed. NOTE: the developer's "todo funciona" confirms the unscoped read path WORKS as delivered; it does NOT constitute approval of the Receptionist-visibility consequence — the decision remains open.
- PHASE 8d.3/8d.4: confirm whether the cycle-of-life transitions (allergy Resolve/Unresolve; medication Suspend/Resume/Discontinue) SHOULD be allowed post-finalize (currently YES, as dedicated auditable transitions orthogonal to content immutability) or whether standing data should be fully immutable post-finalize too (no lifecycle). Currently allowed-post-finalize with IsFinal guards. NOTE: "todo funciona" confirms the lifecycle transitions WORK as delivered; it does NOT constitute approval that post-finalize lifecycle is the desired clinical policy — the decision remains open.
- PHASE 8d.1-8d.4: confirm whether authorship of vitals/diagnoses/allergies/medications should be doctor-profile-only (currently YES) or also nursing/MA (would need a staff-author resolution NOT confirmed in the thread; do not assume).
- PHASE 8d.3/8d.4: confirm whether allergies/medications are truly encounter-independent (currently YES, no status gate) or should optionally be tied to the encounter context in which they were observed.
- PHASE 9/8d+: confirm whether vitales/CIE-10/structured meds (now delivered and verified in 8d) plus prescriptions/documents/payments/reports/telemedicine belong in Phase 9, and whether the patient-facing portal (consent/redaction over history + standing data) is a dedicated consent phase. Prescriptions (formal act) are Phase 9, NOT 8d.4 (active-medication list).

## Notes for Next AI Agent

Before Phase 9 (prescriptions / documents / payments / reports / telemedicine) or the patient-facing consent portal, or the storage swap, or resolving the 8d pending decisions:

1. Read PROJECT_CONTEXT.md fully, including Architecture Decisions, Security Decisions and Known Issues. Phases 8, 8b, 8c (core), 8d.1, 8d.2, 8d.3 and 8d.4 are LOCALLY VERIFIED (8d.1/8d.2 per "8d.1 y 8d.2 funcionan correctamente, ya los podemos dar por buenos"; 8d.3/8d.4 per "todo funciona", with the PatientsPage mount RESOLVED and the DbSet Patients assumption CONFIRMED by the working vertical). B5 is CLOSED (the pasted Appointment.cs confirms Doctor/Clinic/Patient/Room as public navigations; the Include-in-cascade of 8b is valid; the cross-layer-navigation root cause is eliminated — if the history STILL rendered empty names it would be a malformed Include or 403, not a missing navigation). STILL OPEN / PENDING (must NOT be asserted green without evidence, and must NOT be extended from the global reports): C1 (AV EICAR — environment-dependent; if Scanner:Enabled:false, mark "not exercised in dev", do NOT fake green; NOT covered by the 8d "todo funciona"); the storage-provider swap (pending the .csproj + provider decision; free/open-source options EXIST (MinIO self-hosted), so the blocker is EVIDENCE of the library API, not availability — do NOT assume PutObjectAsync/GetObjectAsync signatures; NOT covered by "todo funciona"); the 8c adjacent acts never itemized by the global "ya funciona" report (8c-FINALIZED / 8c-IDEMPOTENT / 8c-DL-GUARD [additive, 8b lacked it] / 8c-REALTIME / 8c-COUNT [depends on the IClinicalAttachmentStore injection into ClinicalRecordsService] / 8c-SANITIZE — NOT covered by the 8d "todo funciona"); and the 8d PENDING business/security decisions (patient-scope for standing data; cycle-of-life post-finalize; non-medical authorship; encounter-tie) which "todo funciona" verifies as WORKING-AS-DELIVERED but does NOT APPROVE as chosen — working != decided. Process rules retained below: verify links not just existence; read permission constant names; read the client BODY not the type signature for transport code; read existing CRUD slots not invent them; re-verify the READ side after a "modify existing method" diff; do NOT generalize one green click into a whole-phase green, into ADJACENT verticals, or into ADJACENT acts of a PRIOR phase the report never touched; do NOT refactor a working infrastructure pattern OR a working verified UI render block to a "DRY" abstract/helper — replicate the proven pattern / leave the verified block byte-intact; store load-method NAMES are NOT uniform (GetTrackedByIdAsync vs GetTrackedWithRelationsAsync) so never infer one by symmetry, while DbSet NAMES ARE uniform (plural-of-entity) so relying on _dbContext.Patients is a flagged naming convention (now confirmed); and "works as delivered" != "approved as the chosen business/security posture".
2. Do NOT introduce MediatR.
3. Do NOT let clinical records (or waiting room, or attachments, or structured data) mutate Appointment.Status directly; notes/attachments/vitals/diagnoses are SEPARATE aggregates referencing the appointment; allergies/medications are SEPARATE aggregates referencing the PATIENT. Neither Appointment nor Patient navigates back to any of them (directionality: dependent layer knows the source of truth, never the reverse). The attachment-count and any per-patient counts are SEPARATE queries, not collection navigations.
4. Keep clinical immutability: finalized records are never content-edited; corrections are additive linked records (CorrectsNoteId / CorrectsVitalSignsId / CorrectsDiagnosisId / CorrectsAllergyId / CorrectsMedicationId). Attachments extend this: immutable Create-only, DRAFT-ONLY for upload AND removal (8c soft-delete gated to !note.IsFinal; finalized -> 409 "sealed"); removal is a SOFT-DELETE (IsDeleted + DeletedAt + DeletedByUserId, idempotent) with SANITIZED audit (extension/size/sha256, never the original name), preserving the DB row + physical file for traceability (NO hard delete; purge/recovery are 8d debt). Standing data (allergies/medications) keeps CONTENT immutable post-finalize (UpdateDraft throws; corrections additive) BUT has an ORTHOGONAL cycle-of-life (allergy IsResolved; medication Active/Suspended/Discontinued) implemented as DEDICATED auditable transitions allowed only post-finalize (IsFinal guards), idempotent where sensible — a deliberate EXTENSION of the Fase 8 rule, pending the developer's confirmation (working-as-delivered != approved). Enforce content immutability at the domain level AND mirror in the UI (edit + authoring hidden on finalized; lifecycle buttons gated on status + IsFinal + perm). (Verified in 8b upload, 8c core remove, 8d.1-8d.4.)
5. Structured-data authorship REQUIRES a doctor profile resolved at runtime (IDoctorStore.GetIdByUserIdAsync) and snapshots AuthorDisplayName from the Doctor entity via GetTrackedWithRelationsAsync (FirstName/LastName) WITHOUT navigating to User (User is not in the domain). Reading vs authoring are distinct gates (a staff user with clinical_records.read may READ but gets 409 on authoring without a profile). Whether nursing/MA should author is a PENDING DECISION (staff-author resolution NOT confirmed; do not assume). (Verified in 8d.1-8d.4.)
6. Per-encounter structured data (vitals 8d.1, diagnoses 8d.2) carries the SAME appointment-status gate as notes (InProgress/Completed else 409). Per-patient standing data (allergies 8d.3, medications 8d.4) has NO appointment-status gate (they describe the patient, not a visit; recordable at any time) — a deliberate structural difference, documented and verified-working.
7. Note/attachment/structured-data concurrency is optimistic (RowVersion) + normal EF transaction; NO app-lock (none allocates a slot). Do not assume BookingLockResources for non-booking writes. Soft-delete / cycle-of-life idempotency is enforced by the domain no-op (already-deleted / already-resolved / already-discontinued -> return), not by a lock.
8. Keep SignalR thin and post-persistence via Outbox. ALL clinical-record payloads are METADATA-ONLY and PHI-FREE: no Title/Content, no binary/StorageKey/Sha256/fileName/contentType, no numeric vitals, no ICD-10 Code/DisplayTerm, no Allergen/Severity/Reaction, no MedicationName/Strength/Indication. Clients invalidate cache; they do not receive clinical text, PHI values, file bytes, or original filenames. The legible name / clinical value is served only to AUTHORIZED readers via the read endpoint. (Verified in 8/8b/8d.1-8d.4.)
9. Keep availability backend authoritative.
10. NAVIGATION PROPERTIES go INSIDE the entity file from the first draft, never as a footnote on the EF configuration.
11. Do NOT invent endpoints, cross-layer methods, permission constant names, client method behavior, storage-library APIs OR store load-method NAMES. Use ONLY signatures confirmed against existing code or requested from the developer. Dropdowns use GET /api/v1/doctors (paginated) + GET /api/v1/doctors/{id} (GetById, includes Clinics); BOTH are guarded by Permissions.DoctorsRead (there is NO DoctorsReadAll constant). The Doctor role was granted doctors.read during Phase 8 verification so the selectors populate for a pure doctor; the directory is visible but cross-queue operation stays blocked by resource-based access. There is NO /doctors/options endpoint; do not reference it. For apiClient transport (multipart/binary/refresh/delete), READ THE BODY, not the TS tooltip: post stringifies the body and request text()+parses every success path, so FormData/Blob need rawRequest + postMultipart + downloadBlob (compile-clean != runtime-correct); apiClient.get/post/put/delete were CONFIRMED in the pasted body for 8b/8c/8d. For per-row actions in CrudResourcePage, use the EXISTING CrudColumn.render slot (and CrudRow.id); do NOT invent a rowActions prop or patch the generic component unless a button must live inside the hard-coded canManage "Acciones" column. For the storage swap, do NOT assume the adapter's method signatures — require the .csproj (library + version + API) + the provider decision before delivering MinioFileStorage/S3/Azure : IFileStorage. For store LOAD methods, do NOT infer the name by symmetry with sibling stores (IDoctorStore has GetTrackedWithRelationsAsync, NOT GetTrackedByIdAsync; the note/attachment/appointment stores DO have GetTrackedByIdAsync) — confirm in the body or use the one the body shows; DbSet NAMES however ARE uniform plural-of-entity, so _dbContext.Patients is a flagged naming reliance (now confirmed), not a signature assumption. After a "modify existing method" diff (e.g. adding && !IsDeleted to GetByNoteAsync), re-verify the READ side: it is the change most easily lost, and "the write worked but the read still shows it" points at the read-side filter, not the write/storage. Do NOT refactor a working infrastructure pattern (outbox handlers, DI, transport) to a "DRY" abstract hierarchy with a parameterless trap ctor — the 8d.1 NRE came from exactly that (DI built the sealed derivatives via the implicit parameterless ctor, leaving _publisher null); replicate the proven sealed-with-real-ctor pattern instead. Do NOT refactor a working, VERIFIED UI render block (the 8b history column in PatientsPage) to a DRY helper either — leave it byte-intact and triplicate inline if a sibling button needs the same logic (this is how the 8d.3/8d.4 mount was delivered without risking the verified 8b block).
12. Close every operational act with a UI / service / domain table in the same draft: if the UI exposes a button, the service and the state machine must already have the matching transition. For 8d.3/8d.4 the cycle-of-life buttons (Resolver/Reactivar; Suspender/Reanudar/Discontinuar) each have a dedicated service method + a domain transition with IsFinal/status guards + an audit entry + a metadata-only event — written in the same draft as the UI button, and the buttons are mounted in PatientsPage under canReadHistory (delivered complete on the pasted file).
13. When creating a profile (doctor/patient-user) tied to a login, VERIFY THE LINK (Doctors.UserId/Patients.UserId == login Users.Id via SQL), not just that the record appears in the admin list. The Phase 3 profile DTOs permit unlinked records (UserId=null), and a fragile response-key assumption (u.id ?? u.userId) silently produces an unlinked profile that passes the list but fails runtime resolution. Do not assume the id key of a response; confirm the shape or arbitrage with SQL. (This was the root cause of the initial Phase 8 block; the link was repaired via SQL during verification.)
14. For diagnosing "did the backend return empty?" use React Developer Tools (browser extension adds Components/Profiler in Chrome/Edge/Firefox; for Safari/other browsers npm install -g react-devtools or yarn global add react-devtools, open from terminal, connect via <script src="http://localhost:8097"> at the START of <head> + reload; for React Native use React Native DevTools, the built-in debugger that deeply integrates React Developer Tools — all features identical to the browser extension including native element highlighting/selection; for RN < 0.76 use the standalone build per the Safari guide). DevTools confirms THAT the query received an empty payload, NOT WHY (broken link vs filter bug vs 403 vs Include-not-populating vs client-stringifying-FormData vs missing-read-side-filter vs abstract-handler-null-publisher vs dialog-not-mounted); the WHY is decided by SQL/Network status. (React Native tooling does NOT apply to this web React/Vite stack.) In 8d: a structured-data record showing in the AUTHOR's dialog but NOT refreshing cross-window -> the outbox handler died (the 8d.1 abstract-ctor NRE), NOT a render fault — DevTools shows the query returning the row, the pila + OutboxMessages prove the publication failure; an allergy/medication dialog with NO button in PatientsPage -> the mount was not done (dialog file exists, no trigger), NOT a backend issue — DevTools is irrelevant, the fix is in the JSX (NOW RESOLVED: the mount was delivered complete on the pasted PatientsPage); empty history for dra.ana but full for SuperAdmin -> scope CORRECT; empty for both -> no encounters / malformed Include (B5 navigation now CLOSED, so not a missing-nav issue) / 403 silent.
15. UserAdminService: only CreateAsync and AssignRolesAsync are functional; GetPagedAsync/GetByIdAsync/UnlockAsync are still NotImplementedException (debt).
16. Phase 8d DELIVERED and VERIFIED the structured-clinical-data foundation in four verticals with each act's UI/service/domain table in one draft: 8d.1 vitals (per-encounter, physiological guards), 8d.2 ICD-10 diagnoses (per-encounter, syntactic regex), 8d.3 allergies (per-patient standing data, content-immutable + IsResolved lifecycle, mounted in PatientsPage), 8d.4 active medications (per-patient standing data, content-immutable + Active/Suspended/Discontinued lifecycle, mounted in PatientsPage). It did NOT deliver the storage-provider swap (withheld pending .csproj + provider decision; the IFileStorage seam makes it additive, not a redesign), did NOT deliver physical purge / un-delete of soft-deleted attachments (8d debt), did NOT deliver semantic coding catalogs (ICD-10 table / ATC / allergen ontology — syntactic/enum only), did NOT deliver the patient-scope filter for standing data (no criterion invented; pending decision; working-as-delivered != approved), and the 8d.3/8d.4 PatientsPage mount IS delivered (complete paste, 8b history block byte-intact). Do NOT ship a half-implemented vertical: each 8d vertical is complete (domain aggregate with navigations inside the entity file + directionality rule 6 + additive-correction immutability + authorship gate + optimistic concurrency + inline permission gates reusing clinical_records.* + PHI-free metadata-only SignalR payload + sealed-with-real-ctor outbox handlers + EF config with Restrict FKs + store + DbSet + DI + contracts + controller + frontend API/hook/dialog + unit + architecture tests + migration). Prescriptions (formal act), documents, payments, reports, telemedicine and the patient-facing consent portal are Phase 9 / a separate consent phase, NOT 8d — keep 8d closed as the verified structured-data foundation; do not inflate it with the prescription act (which is a different domain: posology issued / dispensation / renewals) that risks half-shipped acts. Before Phase 9, resolve the open items the global reports did NOT cover: C1, the storage swap, the 8c adjacent acts, and the 8d pending business/security decisions (working-as-delivered != approved-as-chosen).