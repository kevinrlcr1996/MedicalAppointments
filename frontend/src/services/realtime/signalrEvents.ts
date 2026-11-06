export const SignalREvents = {
  TestEvent: 'TestEvent',
  ScheduleChanged: 'ScheduleChanged',
  AvailabilityChanged: 'AvailabilityChanged',
  AppointmentCreated: 'AppointmentCreated',
  AppointmentCancelled: 'AppointmentCancelled',
  AppointmentRescheduled: 'AppointmentRescheduled',
  AppointmentStatusChanged: 'AppointmentStatusChanged',
} as const;

export const SignalRGroups = {
  TestFoundation: 'test:foundation',
} as const;
