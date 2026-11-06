# SignalR

Purpose:

- Realtime change notifications.

Not purpose:

- Source of truth.
- Business rules.
- Reservation creation.

Hub:

- /hubs/foundation

Groups:

- test:foundation

Events:

- TestEvent

Security:

- JWT.
- Authorization policy.
- Server-controlled group membership.

Future events:

- AppointmentCreated
- AppointmentCancelled
- AppointmentRescheduled
- AppointmentStatusChanged
- AvailabilityChanged
- ScheduleChanged
- PatientCheckedIn
- PatientCalled
- WaitingRoomUpdated
- NotificationCreated
- NotificationRead
- NotificationCountChanged