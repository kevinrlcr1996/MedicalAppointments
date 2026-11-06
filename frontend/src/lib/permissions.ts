export const Permissions = {
  RealtimeTest: 'realtime.test',
  FoundationTestWrite: 'foundation.test.write',

  UsersRead: 'users.read',
  UsersManage: 'users.manage',

  PatientsRead: 'patients.read',
  PatientsCreate: 'patients.create',
  PatientsUpdate: 'patients.update',
  PatientsManageOwn: 'patients.manage_own',

  DoctorsRead: 'doctors.read',
  DoctorsManage: 'doctors.manage',

  SpecialtiesRead: 'specialties.read',
  SpecialtiesManage: 'specialties.manage',

  OrganizationsRead: 'organizations.read',
  OrganizationsManage: 'organizations.manage',

  ClinicsRead: 'clinics.read',
  ClinicsManage: 'clinics.manage',

  RoomsRead: 'rooms.read',
  RoomsManage: 'rooms.manage',

  ResourcesRead: 'resources.read',
  ResourcesManage: 'resources.manage',

  SchedulesRead: 'schedules.read',
  SchedulesManage: 'schedules.manage',

  AvailabilityRead: 'availability.read',

  AppointmentsRead: 'appointments.read',
  AppointmentsCreate: 'appointments.create',

  AppointmentsReadAll: 'appointments.read_all',
  AppointmentsCreateAll: 'appointments.create_all',
  AppointmentsUpdateAll: 'appointments.update_all',
  AppointmentsCancelAll: 'appointments.cancel_all',
  AppointmentsRescheduleAll: 'appointments.reschedule_all',

  AppointmentsReadAssigned: 'appointments.read_assigned',
  AppointmentsCreateAssigned: 'appointments.create_assigned',
  AppointmentsUpdateAssigned: 'appointments.update_assigned',
  AppointmentsCancelAssigned: 'appointments.cancel_assigned',
  AppointmentsRescheduleAssigned: 'appointments.reschedule_assigned',

  AppointmentsReadOwn: 'appointments.read_own',
  AppointmentsCreateOwn: 'appointments.create_own',
  AppointmentsCancelOwn: 'appointments.cancel_own',
  AppointmentsRescheduleOwn: 'appointments.reschedule_own',

  AppointmentsCheckIn: 'appointments.check_in',
  AppointmentsComplete: 'appointments.complete',
  AppointmentsMarkNoShow: 'appointments.mark_no_show',
  AppointmentsOverridePolicy: 'appointments.override_policy',

  MedicalRecordsRead: 'medical_records.read',
  ReportsRead: 'reports.read',
} as const;

export type Permission = (typeof Permissions)[keyof typeof Permissions];

export function hasPermission(
  user: { roles: string[]; permissions: string[] } | null,
  permission: Permission,
): boolean {
  if (!user) {
    return false;
  }

  if (user.roles.includes('SuperAdmin')) {
    return true;
  }

  return user.permissions.includes(permission);
}

export function hasAnyPermission(
  user: { roles: string[]; permissions: string[] } | null,
  permissions: Permission[],
): boolean {
  if (!user) {
    return false;
  }

  if (user.roles.includes('SuperAdmin')) {
    return true;
  }

  return permissions.some((permission) => user.permissions.includes(permission));
}