# PROJECT CONTEXT

## Project

Medical Appointments Platform

## Current Phase

Clinical Records — Attachments Hardening

## Current Version

0.8.2

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

Phase 8c — Clinical Records: Attachments hardening (soft-delete + audit, attachment-count in history timeline, payload/log name sanitization).

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
  - swap to MinIO/S3-compatible later WITHOUT refactor (the interface is the seam; provider choice is a pending decision, STILL OPEN as of 8c)
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
  - projection resolves DoctorFullName/ClinicName via Include in cascade (Appointment.Doctor/.Clinic) — SEE KNOWN ISSUES B5: this is the ONLY point of 8b depending on a cross-layer navigation not confirmed in the thread; if names render empty, switch to IDoctorStore/IClinicStore (confirmed Phase 6/7) instead of Include. STILL OPEN as of 8c.
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
- LOCALLY VERIFIED by developer (reported "ya funciona la fase 8b" after running the 8b verification script): the attachments vertical works (upload to a draft persists a row with GUID StorageKey + 64-hex Sha256 + Clean/Skipped status and a physical file on disk; download returns the intact binary with token+baseURL via downloadBlob; size/type limits 409 pre-persistence; finalized notes hide the upload label and 409 on API -> immutability extends to attachments; realtime ClinicalAttachmentUpdated refreshes a second window without reload with a metadata-only payload). The patient-history vertical works (per-row button via CrudColumn.render; read_assigned scope shows only the doctor's own encounters; read_all shows all; Patient has no column and 403 on API). TWO checks are NOT asserted green from the global report and remain to be confirmed on screen (see Known Issues): B5 (Doctor/Clinic names populated in the history via the Include-in-cascade navigation, the only 8b point depending on a cross-layer navigation not seen in the thread) and C1 (the EICAR AV rejection, which depends on whether clamd was enabled; if Scanner:Enabled:false it is honestly marked "not exercised in dev", not faked green).

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
- LOCALLY VERIFIED by developer (reported "ya funciona" for the remove symptom). The CORE soft-delete vertical is confirmed on screen: clicking "quitar" on a DRAFT removes the attachment from the list AND persists the row with IsDeleted=true (the disappearance from the list proves BOTH the list filter && !IsDeleted AND that the DELETE persisted the flag — if the DELETE had not persisted, the filter would exclude nothing and the file would still show). The physical file is preserved by design (soft-delete, not hard-delete). The following 8c acts are NOT asserted green from the global report and remain to be confirmed on screen (see Known Issues): the finalized-note remove gate (409 + hidden button), double-remove idempotency, the download-a-deleted guard (404/409 — additive, 8b lacked it), the realtime Deleted event carrying NO fileName/contentType, the 📎 N badge showing the real non-deleted count (depends on the service-constructor injection), and name-sanitization in wire+log. B5 and C1 from 8b ALSO remain open. The storage-provider swap remains a pending decision (no .csproj + no provider chosen).

## Current Task

Phase 8c core soft-delete verified per developer report (remove-from-draft persists IsDeleted + list excludes). Preparing confirmation of the remaining 8c acts (finalized gate, idempotency, download-deleted guard, realtime-no-name, count badge, sanitization) + the still-open 8b checks (B5, C1) + the storage-provider swap (needs .csproj + provider decision), then Phase 8d (structured clinical data: vitales/CIE-10/allergies/medications) or Phase 9 (prescriptions / documents / payments / reports / telemedicine).

## Last Completed Task

Phase 8c Clinical Records attachments hardening — domain (ClinicalAttachment soft-delete flags IsDeleted/DeletedAt/DeletedByUserId + idempotent SoftDelete inside the entity file), application (ClinicalAttachmentService.DeleteAsync with draft gate + CanUpdateNote reuse + sanitized audit + metadata-only event without fileName + idempotent no-op; GetCountsByNoteIdsAsync store method; GetPatientHistoryAsync mapping the real attachment count; ClinicalAttachmentEventPayload stripped of fileName/contentType), infrastructure (EF config mapping the three flags + partial index; GetByNoteAsync filtering !IsDeleted while GetTrackedByIdAsync keeps returning deleted; migration AddAttachmentSoftDeletePhase8c), API (DELETE /clinical-records/attachments/{id}), frontend (clinicalAttachmentsApi.remove via apiClient.delete; "quitar" button in AttachmentsSection gated to drafts; 📎 N badge in ClinicalHistoryDialog), unit tests (SoftDelete flags/idempotency/empty-user). Local verification closed PER REPORT for the core remove-from-draft vertical (IsDeleted persists + list excludes + physical file preserved); the adjacent 8c acts and the 8b B5/C1 checks and the storage swap are flagged/pending, not asserted green.

## Next Task

1. Confirm the 8c acts NOT asserted green from the global "ya funciona" report (each is a DISTINCT click the report did not break down; see Known Issues 8c-*): (a) finalized-note remove gate — open a SIGNED note with an attachment, verify the "quitar" button is ABSENT and a forced DELETE by API -> 409 "sealed"; (b) double-remove idempotency — click "quitar" twice (or two windows) on the same draft attachment, verify no error, no second AuditLogs row, DeletedAt stable; (c) download-a-deleted guard — after removing, force GET .../download/{id} by DevTools, verify 404/409 (NOT a 200 serving the binary of a removed file); if it returns 200, the additive IsDeleted guard in DownloadAsync was NOT applied — paste DownloadAsync and add `if (attachment.IsDeleted) throw new NotFoundException(...)`; (d) realtime Deleted payload — second window, remove in A, verify B refreshes AND the WS ClinicalAttachmentUpdated with changeType="Deleted" carries NO fileName/contentType/binary/StorageKey/Sha256; (e) 📎 N badge — open patient history, verify the count is REAL (not 0) and EXCLUDES soft-deleted; if it stays 0, the IClinicalAttachmentStore injection into ClinicalRecordsService was not done — paste the service constructor and add the param; (f) name sanitization in log — verify the AuditLogs AttachmentDeleted entry has extension/size/sha256 and NO original FileName.
2. Confirm the two STILL-OPEN 8b checks (see Known Issues): B5 — open patient history as dra.ana and SuperAdmin, verify Doctor/Clinic NAMES are populated (not empty); if empty, paste Appointment.cs and switch the projection to IDoctorStore/IClinicStore (confirmed Phase 6/7) instead of the Include-in-cascade, WITHOUT assuming the navigation. C1 — if clamd was enabled, exercise the EICAR rejection (409 + audit + no row + rollback); if Scanner:Enabled:false, record it as "AV rejection not exercised in dev" (do not fake green).
3. Deliver the storage-provider swap (the 4th 8c act) ONLY after the developer pastes the .csproj (to confirm the NuGet library + version + API) and chooses the provider (MinIO / S3-compatible / Azure Blob; prefer open-source/free). Then deliver MinioFileStorage : IFileStorage (or the chosen adapter) COMPLETE with its config + DI swap (conditional by config: LocalDisk in dev, provider in prod), ZERO change to service/store/domain. Do NOT assume the adapter's method signatures (the apiClient.postForm / DoctorsReadAll lesson).
4. Optionally close remaining non-blocking debts: physical purge of soft-deleted files + rows after N days with audit (8d background cleanup), un-delete/recover a soft-deleted attachment from a draft (additive on the same flags), optional refetchInterval fallback on the attachments/history queries, and the fine-grained create-note UI gate (expose subject.DoctorId via /auth/me).
5. Complete UserAdminService GetPagedAsync / GetByIdAsync / UnlockAsync (still NotImplementedException) when user administration is needed beyond role assignment.
6. Choose the next domain slice AFTER the above confirmations: Phase 8d (structured clinical data: vitales / CIE-10 coded diagnoses / allergies / structured medications — a NEW clinical domain, NOT attachment hardening, deliberately excluded from 8c to keep it cohesive and avoid half-shipped acts) or Phase 9 (prescriptions / documents / payments / reports / telemedicine). The patient-facing history portal with consent/redaction is a separate consent phase (9+), not 8d.

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
- CROSS-LAYER CALLS USE VERIFIED SIGNATURES ONLY: before delivering code that calls a method on another layer, the exact signature must be confirmed against the existing code (or requested from the developer). Assumed symmetry between stores/hubs caused CS1061 (RoomStore has no ExistsAsync; HubCallerContext has no CallAborted) and CS1503 (appointment access authorizes against the entity, not a DTO). Assuming a permission constant name (DoctorsReadAll) would have shipped a non-functional seeder grant; the controller line was requested and the real constant (DoctorsRead) confirmed before delivering the fix. In Phase 8b the same rule bit twice more: apiClient.postForm was INVENTED (does not exist; the real post stringifies the body) and was caught by requesting the apiClient BODY (not just the TS tooltip, which showed the signature but not the JSON.stringify behavior); and the history projection touches Appointment.Doctor/.Clinic (a cross-layer navigation not seen in the thread) so it is flagged B5 for on-screen confirmation rather than asserted. In Phase 8c the SAME rule governs the storage-provider swap: it is NOT delivered until the .csproj (library + version + API) and the provider decision arrive, because assuming a MinioFileStorage.PutObjectAsync signature would repeat the assumed-symmetry pattern. The IFileStorage seam makes the swap additive, not a redesign.
- EVERY OPERATIONAL ACT IS CLOSED WITH A UI / SERVICE / DOMAIN TABLE in the same draft: if the UI exposes a button, the service and the state machine must already have the corresponding transition written in the same delivery. Misalignment between these three layers caused the recall 400 and the orphaned-call 409.
- CLINICAL NOTES ARE A SEPARATE AGGREGATE REFERENCING THE APPOINTMENT; the appointment IS the encounter and is NOT duplicated, and the appointment does NOT navigate to notes (directionality: dependent layer knows the source of truth, never the reverse). Notes NEVER mutate Appointment.Status.
- CLINICAL IMMUTABILITY: a finalized note cannot be edited or deleted; corrections are additive linked notes (CorrectsNoteId), never rewrites. This is the clinical record standard and is enforced at the domain level (UpdateDraft/CreateCorrection guards) AND mirrored in the UI (Edit hidden on finalized). VERIFIED on screen in Phase 8 (finalize closes the note and hides Edit; correct produces a new linked note while the original stays intact).
- ATTACHMENTS EXTEND CLINICAL IMMUTABILITY TO FILES: an attachment is an immutable, Create-only aggregate that can ONLY be attached to a DRAFT note; finalizing a note seals its attachments (upload hidden in UI + 409 on API). The attachment knows the note (HasOne -> Note.WithMany()); the note does NOT navigate to attachments (ClinicalNote untouched). VERIFIED on screen in Phase 8b (finalized note shows no "Adjuntar archivo" and 409 on API upload).
- ATTACHMENT REMOVAL IS A SOFT-DELETE, NOT A HARD-DELETE (8c): removing an attachment from a DRAFT marks IsDeleted + DeletedAt + DeletedByUserId (idempotent no-op if already deleted) and writes a SANITIZED audit entry (extension/size/sha256, never the original name); the DB row and the PHYSICAL file are PRESERVED for clinical traceability / audit / possible recovery. There is NO user-facing hard delete and NO delete from a finalized note (409 "sealed"). Physical purge of soft-deleted files is 8d background-cleanup debt. This supersedes the 8b "no delete endpoint" rule while keeping the immutability spirit (nothing is erased; removal is reversible-by-audit). VERIFIED on screen in Phase 8c core (remove-from-draft persists IsDeleted + list excludes + file preserved).
- SOFT-DELETE FILTERS APPLY TO LIST + COUNT, NOT TO TRACKED-BY-ID (8c, the distinction that the "quitar no se quitaba" bug exposed): GetByNoteAsync and GetCountsByNoteIdsAsync MUST exclude IsDeleted rows (so the dialog list and the 📎 badge never show removed files), while GetTrackedByIdAsync MUST keep RETURNING deleted rows (no filter) so the service can SEE IsDeleted to (a) deny download of a removed file and (b) make DeleteAsync idempotent. Filtering tracked-by-id by mistake breaks both the download guard and the double-click no-op. The attachment-count is a SEPARATE GroupBy/Count query over the attachments DbSet, NOT a collection navigation on the note (rule 6 intact: the note does not navigate to attachments).
- PAYLOAD/LOG NAME SANITIZATION (8c): the original FileName (and ContentType) are REMOVED from the ClinicalAttachmentUpdated SignalR payload and from the AttachmentDeleted audit entry; a descriptive filename like "lab-juan-perez-oncologia.pdf" leaks patient context and must never travel over the wire or land in logs. The legible name lives ONLY in the DB and is served to AUTHORIZED readers via listByNote (CanReadNotesForAppointment) / downloadBlob. The outbox handler and groups are unchanged (same ClinicalAttachmentUpdated, same doctor/clinic/patient/appointment/user groups); stripping the fields from the record strips them from the JSON.
- STORAGE BEHIND AN ABSTRACTION, DEFAULT LOCAL DISK: IFileStorage with LocalDiskFileStorage (open-source/free, zero external infra, works out-of-the-box); the seam allows swapping to MinIO/S3-compatible later WITHOUT refactor (provider choice is a pending decision, satisfying "prefer open-source/free, no mandatory paid service"). StorageKey is GUID-based + sanitized extension (NEVER the original filename) -> eliminates path-traversal and collisions; the legible name lives only in metadata (FileName). DeleteAsync is internal technical cleanup (rollback of failed/infected uploads), NOT a user endpoint. The 8c swap adapter is additive on this seam and is withheld until the .csproj + provider decision (no assumed library API).
- ANTIVIRUS BEHIND AN ABSTRACTION, PLUGABLE AND DEFAULT NON-BLOCKING IN DEV: IFileScanner with ClamAvFileScanner (real clamd INSTREAM over sockets, no external library) + internal fallback. Attachments:Scanner:Enabled default false in Development (ScanStatus=Skipped so the vertical runs without clamd; MUST be true in production). The scan RESULT affects the flow: Infected -> rollback + 409 + audit, never persisted; scanner unavailable/unexpected -> Skipped + log (explicit config decision: AV downtime does not block clinical recording). This is a complete vertical with a configurable default, NOT a half-implemented upload (the contract is implemented and wired; the only concession is a config flag).
- ATTACHMENT/HISTORY PERMISSIONS REUSE THE PHASE 8 MATRIX (NO new permissions, Permissions.cs untouched): upload = CanUpdateNote (same as editing the draft); REMOVE (8c) = CanUpdateNote + draft gate (same as editing the draft — removing is an edit of the draft's file set); list/download = CanReadNotesForAppointment (same as reading the note's appointment); history = CanReadNotes + read_assigned scope. Inventing new grants would fork the permission matrix (the rule that kept Phase 7 from duplicating appointment access).
- PATIENT HISTORY IS A READ-ONLY AGGREGATED PROJECTION, NOT A NEW AGGREGATE: it groups ClinicalNotes of all the patient's appointments with scope (read_all / read_assigned by DoctorId); the source of truth remains ClinicalNote -> Appointment; NO Encounter/PatientHistory aggregate. Reached FROM the patient row, not via a global history route (avoids exposing a global clinical list; patient-facing history is a separate phase). The 📎 attachment-count badge (8c) is a separate per-note count query folded into the projection, not a new aggregate.
- PHASE 8b/8c SIGNALR PAYLOAD FOR ATTACHMENTS IS METADATA-ONLY AND NAME-SANITIZED: ClinicalAttachmentUpdated carries attachmentId/noteId/appointmentId/doctorId/clinicId/patientId/sizeBytes/scanStatus/changeType/timestamps but NEVER binary, StorageKey, Sha256, clinical Title/Content, NOR fileName/contentType (8c). Clients invalidate cache; they do not receive file bytes, clinical text, or the original filename over the wire. VERIFIED on screen in Phase 8b (no binary/StorageKey/Sha256/clinical text); the 8c name-strip is flagged for on-screen confirmation (see Known Issues 8c-REALTIME).
- USER ADMINISTRATION ROLE ASSIGNMENT: POST /api/v1/users now assigns roles for real (CreateAsync loops user.AssignRole over resolved role ids; AssignRolesAsync uses user.ReplaceRoles which raises the SecurityStamp). Accounts created by administration are born IsEmailConfirmed=true (no email round-trip). This replaced the prior bug where CreateAsync echoed command.Roles without assigning them.
- LINKED-PROFILE CREATION MUST VERIFY THE LINK, NOT JUST EXISTENCE: when creating a profile (doctor/patient-user) tied to a login, confirm Doctors.UserId/Patients.UserId equals the login's Users.Id (SQL arbitrage), because the Phase 3 profile DTOs permit unlinked records (UserId=null) and a fragile response-key assumption (u.id ?? u.userId) silently produced an unlinked profile that passes the admin list but fails runtime resolution. Do not assume the id key of a response; confirm the shape or arbitrage with SQL. (This rule was the root cause of the initial Phase 8 block; the link was repaired via SQL UPDATE during verification and dra.ana then authored/corrected notes successfully.)
- PERMISSION CONSTANT NAMES MUST BE READ, NOT GUESSED: before adding a grant to a role, confirm the exact constant name in Permissions.cs and the exact policy on the controller action. The doctor-directory fix hinged on DoctorsRead (the real constant guarding GetList/GetById), not a guessed DoctorsReadAll (which does not exist). Requesting the two lines (Permissions.cs + DoctorsController) turned a would-be broken one-liner into a correct one.
- CLIENT TRANSPORT MUST BE READ FROM THE BODY, NOT THE TYPE SIGNATURE: a TypeScript tooltip shows a method's SIGNATURE (post<T>(path, body: unknown, ...)) but NOT its BEHAVIOR (post does JSON.stringify(body) + forces Content-Type application/json; request does response.text()+JSON.parse on every success path). Passing FormData to post COMPILES (unknown accepts it) but sends "{}" -> backend [FromForm] IFormFile null -> 400. "Compile-clean != runtime-correct." The fix (rawRequest + postMultipart + downloadBlob) was delivered only after reading the apiClient BODY. For binary/multipart transport, add a path that bypasses the JSON core; never force JSON on a FormData/Blob. get/post/put/patch/delete and request stay UNCHANGED (zero regression). The 8c remove uses apiClient.delete, whose signature was CONFIRMED in the same pasted body (delete<T>(path, options?)) — not assumed.
- GENERIC CRUD SLOTS MUST BE READ, NOT INVENTED: CrudResourcePage already exposes CrudColumn.render?: (row: CrudRow) => ReactNode (propagated to the internal tableColumns) and CrudRow.id is declared. A per-row action button (patient history) is added via a column with render, WITHOUT patching the generic component. Inventing a rowActions prop would have been another assumed symmetry; reading the existing slot beat it. If a button must live INSIDE the hard-coded "Acciones" column (gated by canManage), that DOES need an additive prop — deferred, not assumed.
- A "MODIFY EXISTING METHOD" DIFF IS THE PATCH THAT GETS LOST (8c lesson): the "quitar no se quitaba" bug was the GetByNoteAsync && !x.IsDeleted filter not being applied — the kind of change that edits an existing method body (add one clause to a Where) rather than adding a new file, and is therefore easy to skip when applying a multi-file patch set. When a symptom is "the write worked but the read still shows it", suspect the READ-side filter on the existing list/count query before suspecting the write path or the storage. And a single user report of "it works now" for ONE symptom (remove disappears from the list) evidences ONLY that symptom's path (list-filter + DELETE-persisted); it does NOT evidence the adjacent gates (finalized, idempotency, download-deleted, realtime-payload-shape, count-badge, sanitization) which are DISTINCT clicks and must be confirmed individually — do not generalize one green click into a whole-phase green.

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
- PATIENT HAS NO CLINICAL RECORDS ACCESS IN PHASE 8/8b/8c: no clinical_records.* grants; the clinical history dialog is absent for Patient and GET notes/history/attachments -> 403. Exposing history to the patient requires consent/redaction/portal and is a separate phase. VERIFIED on screen in Phase 8 (Patient sees no "Notas clínicas" button and GET notes -> 403) and Phase 8b (Patient sees no "Historial" column and GET history -> 403).
- CLINICAL NOTE READ SCOPE mirrors appointment read scope: read_all sees all notes; read_assigned sees notes only for appointments where subject.DoctorId == appointment.DoctorId; a cross-doctor note read -> 403 (enforced in the service, not only by list filtering).
- CLINICAL NOTE AUTHORING SCOPE: create requires a doctor profile AND ownership of the appointment's doctor (or manage_all); update/finalize require authorship (or manage_all); correct requires authorship of the original (or manage_all). A staff user without a profile gets 409 on authoring even if they can read. VERIFIED on screen in Phase 8 (SuperAdmin reads but cannot author -> 409 profile).
- FINALIZED NOTE IMMUTABILITY IS BACKEND-ENFORCED, not just UI-hidden: PUT on a finalized note -> 409/400 even if the UI hides the Edit button (double barrier).
- NO CLINICAL CONTENT IN SIGNALR: the ClinicalNoteUpdated payload excludes Title/Content; clients invalidate cache, they do not receive clinical text over the wire. VERIFIED on screen in Phase 8.
- ATTACHMENTS ARE IMMUTABLE AND DRAFT-ONLY: an attachment can only be added to a draft note; finalizing seals its attachments (upload hidden in UI + 409 on API). Removal from a draft is a SOFT-DELETE gated to the same draft condition (CanUpdateNote + !note.IsFinal); removing from a finalized note -> 409 "sealed" (UI hides "quitar" on finalized cards, same gate as "Adjuntar"). This extends the Phase 8 clinical-immutability guarantee to files WITHOUT erasing them (traceability). VERIFIED on screen in Phase 8b (upload) and Phase 8c core (remove-from-draft persists IsDeleted + list excludes).
- SOFT-DELETED FILES MUST NOT BE DOWNLOADABLE: a removed (IsDeleted) attachment must return 404/409 on download, never serve its binary (the file is preserved on disk for audit, but it is logically removed from the record). This guard is ADDITIVE in 8c (8b had no delete, so 8b's DownloadAsync lacked it) — SEE Known Issues 8c-DL-GUARD: confirm on screen; if a forced GET .../download of a removed file returns 200, the guard was not applied and it is a traceability leak, not a cosmetic issue.
- INFECTED FILES NEVER ENTER THE RECORD: the AV scan result affects the flow — Infected -> rollback of the temp file + 409 + audit, no row persisted (the persisted ScanStatus is only Clean or Skipped). Scanner unavailability/unexpected response -> Skipped + log (explicit config decision; AV downtime does not block clinical recording, but a POSITIVE detection always blocks).
- STORAGE KEYS ARE GUID-BASED AND TRAVERSAL-SAFE: StorageKey = notes/{noteId:N}/{guid}{sanitized-ext}, NEVER the original filename (eliminates path-traversal and collisions); the legible name lives only in metadata (FileName). LocalDiskFileStorage.ResolveSafe additionally validates the resolved path stays under the root (double defense). DeleteAsync is internal technical cleanup only, not a user endpoint.
- PATIENT HISTORY SCOPE MIRRORS NOTE READ SCOPE: read_all sees all the patient's notes across all doctors; read_assigned sees only notes whose appointment.DoctorId == subject.DoctorId (a doctor sees only THEIR encounters with that patient); a cross-doctor history read is filtered out (enforced in the service/query, not only by UI). Patient has no access (403). The 📎 attachment-count badge respects the same scope (it counts only notes the reader is allowed to see).
- NO FILE BYTES, CLINICAL TEXT, OR ORIGINAL NAME IN SIGNALR/LOGS: the ClinicalAttachmentUpdated payload excludes binary, StorageKey, Sha256, clinical Title/Content AND (8c) the original fileName/contentType; the AttachmentDeleted audit excludes the original name (logs extension/size/sha256 only). Clients invalidate cache, they do not receive file bytes, clinical text, or the descriptive filename over the wire; the name is served only to AUTHORIZED readers via listByNote/downloadBlob. VERIFIED on screen in Phase 8b (no binary/StorageKey/Sha256/clinical text); the 8c name-strip in wire+log is flagged for on-screen confirmation (see Known Issues 8c-REALTIME / 8c-SANITIZE).
- DOWNLOAD TRANSPORT PRESERVES AUTH AND BASE URL: attachments are downloaded via apiClient.downloadBlob (token + apiBaseUrl internal, intact Blob), NOT via a relative <a href="/api/v1/..."> which would lose the Authorization header (401) and hit the frontend port (404, because apiClient targets the absolute backend origin). This is a security+correctness fix, not cosmetic.
- DOCTORS DIRECTORY vs QUEUE OPERATION ARE SEPARATE GATES: granting doctors.read (Permissions.DoctorsRead) to the Doctor role exposes the MEDICAL DIRECTORY (professional contact/license/specialty/clinic info, NOT PHI) so the medical/clinic selectors populate in Reservar / waiting-room board / Availability, but does NOT grant cross-queue operation or reading; those remain enforced by the Phase 5/7 resource-based access delegating on assigned DoctorId (a doctor acting on or viewing another doctor's queue -> 403). The medical selector may therefore list all doctors while only the doctor's own queue is operable. Optional minimum-scope hardening (filter GetList/GetById by DoctorId for a Doctor subject) is tracked as debt, not blocking.

## Database

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

## Notifications

No external notification channels implemented through Phase 8c.

Offer, call, clinical-note and attachment notifications are SignalR-only (in-app realtime). Email/SMS notifications remain technical debt. Clinical notes and attachments are NEVER pushed with content/binaries/original-names over SignalR (metadata-only, name-sanitized).

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
- PHASE 8c CHECKS NOT ASSERTED GREEN from the global "ya funciona" report (each is a DISTINCT click the report did not break down; confirm on screen, do NOT generalize the one green remove-click into a whole-phase green — see Architecture Decisions last bullet):
  - 8c-FINALIZED: removing from a SIGNED note must be blocked (UI hides "quitar", same canUpdate && !isFinal gate as "Adjuntar"; forced DELETE by API -> 409 "sealed and cannot be removed"). Not reported (the developer tested a DRAFT).
  - 8c-IDEMPOTENT: double-remove (or two windows) on the same draft attachment must be a no-op (no error, no second AuditLogs row, DeletedAt stable). Not reported.
  - 8c-DL-GUARD: downloading a soft-deleted attachment must 404/409, NOT serve the binary. This guard is ADDITIVE in 8c (8b's DownloadAsync lacked it because 8b had no delete). If a forced GET .../download of a removed file returns 200, the guard was NOT applied -> traceability leak; paste DownloadAsync and add `if (attachment.IsDeleted) throw new NotFoundException(...)`. Not reported.
  - 8c-REALTIME: the ClinicalAttachmentUpdated with changeType="Deleted" must carry NO fileName/contentType (8c stripped them from the payload record). If the payload record change was not applied alongside the store fix, the name still leaks over the wire. Not reported.
  - 8c-COUNT: the 📎 N badge in the patient-history timeline must show the REAL non-deleted count (not the 8b fixed 0). This depends on IClinicalAttachmentStore being injected into ClinicalRecordsService (additive constructor param) AND GetCountsByNoteIdsAsync being wired. If the badge stays 0 or the history call errors, the injection was not done; paste the service constructor and add the param. Not reported.
  - 8c-SANITIZE: the AuditLogs AttachmentDeleted entry must contain extension/sizeBytes/sha256 and NO original FileName. Not reported.
- PHASE 8b CHECK B5 (STILL OPEN; confirm on screen): the patient-history projection resolves DoctorFullName/ClinicName via Include-in-cascade on Appointment.Doctor/.Clinic. This is the ONLY 8b point depending on a cross-layer navigation NOT seen in the thread (Appointment.cs was never pasted). If the history renders with EMPTY doctor/clinic names, do NOT assume the Include works: paste Appointment.cs and switch the projection to IDoctorStore/IClinicStore (confirmed Phase 6/7) instead of Include. If names ARE populated on screen, B5 is confirmed.
- PHASE 8b CHECK C1 (STILL OPEN; AV EICAR depends on environment): if clamd was enabled (Attachments:Scanner:Enabled:true), exercise the EICAR rejection (409 + AuditLogs AttachmentRejectedInfected + no ClinicalAttachments row + temp rollback) to prove the scan BLOCKS, not decorates. If Scanner:Enabled:false (dev default), the AV rejection is honestly marked "not exercised in dev" (attachments persist as Skipped) — do NOT fake green. Either way the contract is implemented and wired; only the positive-detection path needs a live clamd to be exercised.
- PHASE 8c STORAGE SWAP (PENDING, not a bug): the non-local storage provider (MinIO/S3-compatible/Azure) is NOT delivered because it requires the developer's provider decision (a pending decision) AND the .csproj to confirm the NuGet library + version + API (assuming a PutObjectAsync signature would repeat the apiClient.postForm / DoctorsReadAll assumed-symmetry pattern). The IFileStorage seam from 8b already allows the swap WITHOUT refactor; LocalDiskFileStorage remains the working default. Deliver the adapter COMPLETE in the next pass once .csproj + provider arrive.
- UserAdminService GetPagedAsync/GetByIdAsync/UnlockAsync remain NotImplementedException (debt carried from earlier phases); only CreateAsync and AssignRolesAsync were made functional in Phase 8 verification (enough to create a Doctor user via API).
- DIAGNOSTIC TOOLING NOTE (React DevTools): to confirm that a list/dialog received an EMPTY payload from the backend (e.g. appointments items:[] or notes/attachments/history []), use React Developer Tools. The browser EXTENSION (Chrome/Edge/Firefox) adds the Components and Profiler panels when visiting a React-built site. For other browsers (e.g. Safari) install the react-devtools npm package (npm install -g react-devtools or yarn global add react-devtools), open the developer tools from the terminal (react-devtools), and connect the site by adding <script src="http://localhost:8097"></script> at the START of the website's <head>, then reload. For React Native apps use React Native DevTools, the built-in debugger that integrates React Developer Tools (all features work identically to the browser extension, including native element highlighting and selection); for React Native versions earlier than 0.76 use the standalone build following the Safari/other-browsers guide. IMPORTANT: DevTools confirms THAT the server returned empty (the query state shows items:[] / data undefined), NOT WHY (broken link vs filter bug vs 403 vs Include-not-populating vs client-stringifying-FormData vs missing-read-side-filter). The WHY is decided by SQL/Network status, not DevTools. Use DevTools only to distinguish "backend sent empty" from "component failed to render". In Phase 8c specifically: an attachment that "won't remove" from the list while the BD row shows IsDeleted=1 -> the read-side filter is missing (the 8c bug), NOT a render fault and NOT a storage fault — DevTools would show the query returning the row, Network/SQL prove the flag is set; a 📎 badge stuck at 0 with rows present -> the count injection/query was not wired (8c-COUNT), check the history GET response; a download of a removed file returning 200 -> the additive IsDeleted guard is missing (8c-DL-GUARD), a service issue not a client issue; an empty history for dra.ana but full for SuperAdmin -> scope CORRECT (read_assigned), not a bug.

## Technical Debt

- Complete user administration (GetPagedAsync/GetByIdAsync/UnlockAsync still NotImplementedException).
- Add full room/resource aware availability engine.
- Add appointment calendar UI.
- Add notifications consumer (email/SMS) for waiting list offers, appointment events, room calls, clinical notes and attachments.
- Add background cleanup for expired idempotency records.
- Add more integration tests for concurrent booking, idempotency, waiting list offer/accept/conflict/re-match loop, concurrent call-next / sequence uniqueness, clinical note immutability/correction chain, attachment draft-only immutability + infected-rollback + history scope, AND (8c) soft-delete hides-from-list + count-excludes-deleted + download-deleted-guard + double-remove-idempotency + finalized-remove-409.
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
- PROCESS RULE for future phases: close every operational act with a UI / service / domain table in the same draft, and put navigation properties inside the entity file from the first draft. Do not assume cross-layer signatures OR permission constant names OR client method behavior OR storage-library APIs; confirm them against the existing code (or request the exact line/body/csproj) before delivering. For transport code (multipart/binary/refresh), read the client BODY, not the type signature — compile-clean != runtime-correct. For a "modify existing method" diff (e.g. adding && !IsDeleted to a Where), re-verify the read side after applying patches — it is the change most easily lost. And do NOT generalize one green click ("it works now" for one symptom) into a whole-phase green; confirm each adjacent gate individually.
- PHASE 8c DEBT (8d): physical purge of soft-deleted attachments (delete the disk file + the DB row after N days, with an auditable purge entry) — 8c deliberately PRESERVES file+row for traceability; the purge is a background-cleanup job analogous to idempotency-record cleanup.
- PHASE 8c DEBT (8d): un-delete / recover a soft-deleted attachment from a draft (additive on the same IsDeleted/DeletedAt/DeletedByUserId flags; 8c only removes, does not restore).
- PHASE 8c PENDING (not debt, a decision): storage-provider swap to MinIO/S3-compatible/Azure behind the existing IFileStorage seam (no refactor needed) — blocked on the provider decision + .csproj (library/version/API confirmation), NOT on code.
- PHASE 8b DEBT (still open): optional refetchInterval fallback on the attachments and history queries (like the Phase 7 TV) so the dialogs refresh even if a SignalR event is missed.
- PHASE 8 OPTIONAL HARDENING (not blocking): filter GET /api/v1/doctors and GET /api/v1/doctors/{id} by DoctorId when the subject is a Doctor, so the medical/clinic selectors show only the doctor's own profile (minimum scope) instead of the full directory granted by doctors.read. Requires confirming IDoctorService.GetPagedAsync and a doctorId resolver/access for the subject; do not assume those signatures.
- PHASE 8 DEBT: add a fine-grained gate on the create-note form visibility (hide it when the user is not the appointment's doctor) — currently the backend enforces it and the error is shown in a red Alert; exposing subject.DoctorId to the client (via /auth/me claims) would let the UI hide it proactively.
- PHASE 8d (NOT in 8c): structured clinical data — vitales / CIE-10 coded diagnoses / allergies / structured medications. A NEW clinical domain, deliberately EXCLUDED from 8c (which is attachment hardening) to keep 8c cohesive and avoid half-shipped acts (rule 15). ClinicalNote/ClinicalAttachment are prepared to grow without refactor; the history timeline (8b/8c) is the surface it will extend.
- PHASE 9+ (NOT in 8c/8d): patient-facing history portal with consent/redaction; prescriptions / documents / payments / reports / telemedicine.

## Files Changed

Phase 8c introduced ClinicalRecords attachments soft-delete — domain (ClinicalAttachment + IsDeleted/DeletedAt/DeletedByUserId flags + idempotent SoftDelete(deletedByUserId, now) with empty-user guard, all inside the entity file per rule 9), application (ClinicalAttachmentService.DeleteAsync with draft gate via CanUpdateNote + !note.IsFinal + sanitized audit (extension/size/sha256, no original name) + metadata-only event with changeType="Deleted" + idempotent no-op when already deleted; IClinicalAttachmentStore.GetCountsByNoteIdsAsync (GroupBy/Count excluding deleted); ClinicalRecordsService.GetPatientHistoryAsync now mapping the real non-deleted count into ClinicalHistoryItemDto.AttachmentCount via the new store method; ClinicalAttachmentEventPayload stripped of fileName/contentType), infrastructure (ClinicalAttachmentConfiguration mapping the three flags + partial index IX_ClinicalAttachments_NoteId_IsDeleted; EfClinicalAttachmentStore.GetByNoteAsync filtering !IsDeleted while GetTrackedByIdAsync keeps returning deleted rows so the service can enforce the download guard + idempotency; migration AddAttachmentSoftDeletePhase8c), API (ClinicalRecordsController DELETE /clinical-records/attachments/{id} -> 204, reusing ClinicalRecordsManage + IClinicalAttachmentService), frontend (clinicalAttachmentsApi.remove via apiClient.delete [signature CONFIRMED in the 8b-pasted body]; AttachmentsSection "quitar" Button [Trash2] gated to drafts with handleDelete/deleteMutation/localized error; ClinicalHistoryDialog 📎 N badge when attachmentCount > 0), unit tests (ClinicalAttachmentTests SoftDelete flags/idempotency/empty-user). NO new permissions, NO new policies, NO new outbox event type (ClinicalRecords.AttachmentUploaded reused for the Deleted changeType), ClinicalNotes and Appointments tables NOT modified. The storage-provider swap (the 4th 8c act) is NOT in this delivery — withheld pending the .csproj + provider decision (no assumed library API); LocalDiskFileStorage remains default. Phase 8c local verification closed PER REPORT for the core remove-from-draft vertical (IsDeleted persists + list excludes + physical file preserved); the adjacent 8c acts (8c-FINALIZED / 8c-IDEMPOTENT / 8c-DL-GUARD / 8c-REALTIME / 8c-COUNT / 8c-SANITIZE) and the still-open 8b B5/C1 checks and the storage swap are flagged/pending, NOT asserted green from the global report.

Phase 8b introduced ClinicalRecords attachments domain (ClinicalAttachment immutable aggregate + ClinicalAttachmentScanStatus enum + Note navigation inside the entity file), application abstractions (IFileStorage + StoredFile, IFileScanner + ScanResult, AttachmentsOptions with StorageRoot/MaxBytes/AllowedContentTypes/Scanner), application services (ClinicalAttachmentService with draft-only gate + CanUpdateNote reuse + size/type limits + SHA-256 + GUID storage key + scan + infected-rollback + 409 + metadata-only event enqueue + audit; patient-history projection GetPatientHistoryAsync on IClinicalRecordsService with read_all/read_assigned scope + ClinicalHistoryItemDto; IClinicalAttachmentStore; ClinicalAttachmentEventPayload metadata-only; IClinicalRecordsEventNotifier.EnqueueAttachmentEvent), infrastructure (LocalDiskFileStorage with ResolveSafe traversal guard, ClamAvFileScanner with clamd INSTREAM + non-blocking Skipped fallback, ClinicalAttachmentConfiguration with Restrict FK to ClinicalNotes + indexes, EfClinicalAttachmentStore, EfClinicalNoteStore.GetHistoryByPatientAsync with Include-in-cascade, DbSet ClinicalAttachments, DI for storage/scanner/store/service/outbox-handler/options), contracts (ClinicalAttachmentResponse + ClinicalHistoryItemResponse), API (ClinicalRecordsController attachments list/upload/download + patients/{id}/history + [RequestSizeLimit] + IClinicalAttachmentService injection + mappers), outbox handler (ClinicalAttachmentUploadedOutboxHandler publishing ClinicalAttachmentUpdated metadata-only to the confirmed groups), realtime addition (ClinicalAttachmentUpdated event), frontend (clinicalAttachments API via apiClient.postMultipart/downloadBlob, useClinicalAttachmentsRealtime hook, AttachmentsSection + ScanBadge inside ClinicalNotesDialog note cards with draft-only upload + blob download, ClinicalHistoryDialog, PatientsPage per-row "Historial clínico" button via the existing CrudColumn.render slot WITHOUT patching CrudResourcePage), unit + architecture tests (ClinicalAttachmentTests + ClinicalRecordsLayerTests extended), migration AddClinicalAttachmentsPhase8b (ClinicalAttachments; ClinicalNotes and Appointments untouched), and Attachments configuration. PLUS the apiClient fix applied during 8b verification: internal rawRequest (auth/token/baseURL/refresh/correlationId, no forced JSON, no text()+parse on success) + postMultipart (FormData, browser boundary) + downloadBlob (intact Blob); get/post/put/patch/delete and request UNCHANGED. Phase 8b local verification closed per developer report (attachments vertical + history vertical confirmed on screen); B5 (history doctor/clinic names via Include) and C1 (EICAR AV rejection) flagged for on-screen/environment confirmation rather than asserted green from the global report.

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
- PHASE 8: confirm whether clinical notes should also be allowed on CheckedIn appointments (e.g. "motivo de consulta" pre-visit); currently only InProgress/Completed.
- PHASE 8: confirm whether the patient should ever see their own clinical history (currently NO in Phase 8/8b/8c; requires consent/redaction/portal — separate phase).
- PHASE 8: confirm whether a correction should be allowed only by the original author or also by another clinician with manage_all (currently author OR manage_all).
- PHASE 8: confirm whether a Doctor should see the FULL medical directory (current state after the doctors.read grant) or only their own profile (minimum-scope filter by DoctorId, optional hardening). Currently full directory; cross-queue operation stays blocked regardless.
- PHASE 8b: confirm whether attachments may be added to a note that is a CORRECTION draft (currently yes — a correction is a draft until finalized, so it accepts attachments; confirm this is clinically desired).
- PHASE 8b: confirm the AV posture in production (Scanner:Enabled:true + clamd; in dev it is false -> Skipped). Confirm whether a POSITIVE detection should ever be overridden by a clinician with manage_all (currently NO — infected never persists; confirm this hard rule).
- PHASE 8c: confirm whether attachment remove-from-draft should be a SOFT-DELETE with audit (currently YES in 8c — IsDeleted + sanitized audit + preserved file/row; confirm this matches the clinical-traceability requirement, vs a hard delete which would erase evidence).
- PHASE 8c: confirm whether a soft-deleted attachment should be RECOVERABLE (un-delete) from a draft (currently NO — 8c only removes; recovery is 8d debt on the same flags).
- PHASE 8c: confirm whether soft-deleted files/rows should be PURGED after N days with an auditable purge (currently NO — 8c preserves indefinitely; purge is 8d background-cleanup debt).
- PHASE 8c: confirm the storage provider for the swap (local disk vs S3-compatible vs MinIO vs Azure Blob; prefer open-source/free) — required, with the .csproj, to deliver the adapter (the seam is ready; the provider + library API are not assumed).
- PHASE 8d/9: confirm whether vitales/CIE-10/structured meds belong in 8d or a later phase, and whether the patient-facing portal (consent/redaction) is 9+ or a dedicated consent phase.

## Notes for Next AI Agent

Before Phase 8d (structured clinical data) or Phase 9, or the storage swap:

1. Read PROJECT_CONTEXT.md fully, including Architecture Decisions, Security Decisions and Known Issues. Phase 8 is LOCALLY VERIFIED, Phase 8b is LOCALLY VERIFIED per developer report, and Phase 8c CORE soft-delete is LOCALLY VERIFIED per developer report (remove-from-draft persists IsDeleted + list excludes + physical file preserved — evidenced by the "quitar" working after the read-side filter fix). FOUR groups of checks are NOT asserted green and must be confirmed with DISTINCT clicks / evidence, NOT generalized from one green click (see Known Issues): (a) the adjacent 8c acts 8c-FINALIZED / 8c-IDEMPOTENT / 8c-DL-GUARD / 8c-REALTIME / 8c-COUNT / 8c-SANITIZE; (b) the still-open 8b B5 (history doctor/clinic names via Include — paste Appointment.cs if empty, switch to IDoctorStore/IClinicStore, do NOT assume) and C1 (EICAR AV — if Scanner:Enabled:false, mark "not exercised in dev", do NOT fake green); (c) the storage-provider swap (pending the .csproj + provider decision; do NOT assume the library API); (d) the soft-delete filters split (GetByNoteAsync/GetCountsByNoteIdsAsync EXCLUDE deleted; GetTrackedByIdAsync KEEPS deleted so the download guard + idempotency work — filtering tracked-by-id by mistake breaks both). Process rules retained: verify links not just existence; read permission constant names; read the client BODY not the type signature for transport code; read existing CRUD slots not invent them; re-verify the READ side after a "modify existing method" diff; do NOT generalize one green click into a whole-phase green.
2. Do NOT introduce MediatR.
3. Do NOT let clinical records (or waiting room, or attachments) mutate Appointment.Status directly; notes and attachments are SEPARATE aggregates referencing the appointment/note. The appointment does NOT navigate to notes; the note does NOT navigate to attachments (directionality: dependent layer knows the source of truth, never the reverse). The attachment-count is a SEPARATE GroupBy/Count query, not a collection navigation.
4. Keep clinical immutability: finalized notes are never edited/deleted; corrections are additive linked notes (CorrectsNoteId). Attachments extend this: immutable Create-only, DRAFT-ONLY for upload AND for removal (8c soft-delete gated to !note.IsFinal; finalized -> 409 "sealed"); removal is a SOFT-DELETE (IsDeleted + DeletedAt + DeletedByUserId, idempotent) with SANITIZED audit (extension/size/sha256, never the original name), preserving the DB row + physical file for traceability (NO hard delete; purge/recovery are 8d debt). Enforce at the domain level AND mirror in the UI (upload + quitar hidden on finalized). (Verified in Phase 8b upload + Phase 8c core remove.)
5. Note authorship REQUIRES a doctor profile resolved at runtime (IDoctorStore.GetIdByUserIdAsync); snapshot AuthorDisplayName without navigating to User (User is not in the domain). Reading vs authoring are distinct gates. (Verified in Phase 8.)
6. Note/attachment concurrency is optimistic (RowVersion) + normal EF transaction; NO app-lock (neither allocates a slot). Do not assume BookingLockResources for non-booking writes. Soft-delete idempotency is enforced by the domain no-op (already-deleted -> return), not by a lock.
7. Keep SignalR thin and post-persistence via Outbox. Clinical note AND attachment payloads are METADATA-ONLY and NAME-SANITIZED (no Title/Content, no binary, no StorageKey, no Sha256, and 8c: no fileName/contentType). Clients invalidate cache, they do not receive clinical text, file bytes, or the original filename. The legible name is served only to AUTHORIZED readers via listByNote/downloadBlob. (Verified in Phase 8 and 8b; the 8c name-strip is flagged for on-screen confirmation.)
8. Keep availability backend authoritative.
9. NAVIGATION PROPERTIES go INSIDE the entity file from the first draft, never as a footnote on the EF configuration.
10. Do NOT invent endpoints, cross-layer methods, permission constant names, client method behavior OR storage-library APIs. Use ONLY signatures confirmed against existing code or requested from the developer. Dropdowns use GET /api/v1/doctors (paginated) + GET /api/v1/doctors/{id} (GetById, includes Clinics); BOTH are guarded by Permissions.DoctorsRead (there is NO DoctorsReadAll constant). The Doctor role was granted doctors.read during Phase 8 verification so the selectors populate for a pure doctor; the directory is visible but cross-queue operation stays blocked by resource-based access. There is NO /doctors/options endpoint; do not reference it. For apiClient transport (multipart/binary/refresh/delete), READ THE BODY, not the TS tooltip: post stringifies the body and request text()+parses every success path, so FormData/Blob need rawRequest + postMultipart + downloadBlob (compile-clean != runtime-correct); apiClient.delete was CONFIRMED in the pasted body for the 8c remove. For per-row actions in CrudResourcePage, use the EXISTING CrudColumn.render slot (and CrudRow.id); do NOT invent a rowActions prop or patch the generic component unless a button must live inside the hard-coded canManage "Acciones" column. For the storage swap, do NOT assume the adapter's method signatures — require the .csproj (library + version + API) + the provider decision before delivering MinioFileStorage/S3/Azure : IFileStorage. After a "modify existing method" diff (e.g. adding && !IsDeleted to GetByNoteAsync), re-verify the READ side: it is the change most easily lost, and "the write worked but the read still shows it" points at the read-side filter, not the write/storage.
11. Close every operational act with a UI / service / domain table in the same draft: if the UI exposes a button, the service and the state machine must already have the matching transition.
12. When creating a profile (doctor/patient-user) tied to a login, VERIFY THE LINK (Doctors.UserId/Patients.UserId == login Users.Id via SQL), not just that the record appears in the admin list. The Phase 3 profile DTOs permit unlinked records (UserId=null), and a fragile response-key assumption (u.id ?? u.userId) silently produces an unlinked profile that passes the list but fails runtime resolution. Do not assume the id key of a response; confirm the shape or arbitrage with SQL. (This was the root cause of the initial Phase 8 block; the link was repaired via SQL during verification.)
13. For diagnosing "did the backend return empty?" use React Developer Tools (browser extension adds Components/Profiler in Chrome/Edge/Firefox; for Safari/other browsers npm install -g react-devtools + <script src="http://localhost:8097"> at the start of <head> + reload; for React Native use React Native DevTools, the built-in debugger integrating React Developer Tools — all features identical to the browser extension including native element highlighting/selection; for RN < 0.76 use the standalone build per the Safari guide). DevTools confirms THAT the query received an empty payload, NOT WHY (broken link vs filter bug vs 403 vs Include-not-populating vs client-stringifying-FormData vs missing-read-side-filter); the WHY is decided by SQL/Network status. (React Native tooling does NOT apply to this web React/Vite stack.) In 8c: attachment "won't remove" while BD row has IsDeleted=1 -> read-side filter missing (the 8c bug), not render/storage; 📎 badge stuck at 0 with rows present -> count injection/query not wired (8c-COUNT); download of a removed file returning 200 -> additive IsDeleted guard missing (8c-DL-GUARD, a service issue); empty history for dra.ana but full for SuperAdmin -> scope CORRECT.
14. UserAdminService: only CreateAsync and AssignRolesAsync are functional; GetPagedAsync/GetByIdAsync/UnlockAsync are still NotImplementedException (debt).
15. Phase 8c DELIVERED the attachment hardening vertical in one draft with its UI/service/domain table: soft-delete-from-draft (idempotent + sanitized audit + preserved file/row), attachment-count badge in the history timeline (separate count query, scope-respecting), and payload/log name sanitization (fileName/contentType stripped from SignalR + audit). It did NOT deliver the storage-provider swap (withheld pending .csproj + provider decision; the IFileStorage seam makes it additive, not a redesign) and did NOT deliver physical purge / un-delete (8d debt). Do NOT ship a half-implemented delete: the 8c remove is complete (UI quitar button + service draft-gate + domain idempotent SoftDelete + sanitized audit + read-side filters + download guard [additive, confirm applied]). Vitales/CIE-10/structured meds (8d) and the patient-facing portal (9+) are SEPARATE domains, NOT attachment hardening — keep 8c cohesive; do not inflate it with a new clinical domain that risks half-shipped acts.