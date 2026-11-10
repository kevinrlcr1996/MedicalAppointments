export const SignalREvents = {
  TestEvent: 'TestEvent',
  ScheduleChanged: 'ScheduleChanged',
  AvailabilityChanged: 'AvailabilityChanged',
  AppointmentCreated: 'AppointmentCreated',
  AppointmentCancelled: 'AppointmentCancelled',
  AppointmentRescheduled: 'AppointmentRescheduled',
  AppointmentStatusChanged: 'AppointmentStatusChanged',
  WaitingListOfferCreated: 'WaitingListOfferCreated',
  WaitingListUpdated: 'WaitingListUpdated',
  WaitingRoomUpdated: 'WaitingRoomUpdated',
  PatientCalled: 'PatientCalled',
  PatientRecalled: 'PatientRecalled',
  ClinicalNoteUpdated: 'ClinicalNoteUpdated',
  ClinicalAttachmentUpdated: 'ClinicalAttachmentUpdated',
  VitalSignsUpdated: 'VitalSignsUpdated',
  DiagnosisUpdated: 'DiagnosisUpdated',
  PatientAllergyUpdated: 'PatientAllergyUpdated',
  PatientMedicationUpdated: 'PatientMedicationUpdated',
} as const;

export const SignalRGroups = {
  TestFoundation: 'test:foundation',
} as const;
