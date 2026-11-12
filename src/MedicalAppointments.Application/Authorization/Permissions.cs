namespace MedicalAppointments.Application.Authorization;

public static class Permissions
{
    public const string RealtimeTest = "realtime.test";
    public const string FoundationTestWrite = "foundation.test.write";

    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string PermissionsRead = "permissions.read";

    public const string PatientsRead = "patients.read";
    public const string PatientsCreate = "patients.create";
    public const string PatientsUpdate = "patients.update";
    public const string PatientsManageOwn = "patients.manage_own";

    public const string DoctorsRead = "doctors.read";
    public const string DoctorsManage = "doctors.manage";

    public const string SpecialtiesRead = "specialties.read";
    public const string SpecialtiesManage = "specialties.manage";

    public const string OrganizationsRead = "organizations.read";
    public const string OrganizationsManage = "organizations.manage";

    public const string ClinicsRead = "clinics.read";
    public const string ClinicsManage = "clinics.manage";

    public const string RoomsRead = "rooms.read";
    public const string RoomsManage = "rooms.manage";

    public const string ResourcesRead = "resources.read";
    public const string ResourcesManage = "resources.manage";

    public const string SchedulesRead = "schedules.read";
    public const string SchedulesManage = "schedules.manage";

    public const string AvailabilityRead = "availability.read";

    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsCreate = "appointments.create";
    public const string AppointmentsUpdate = "appointments.update";
    public const string AppointmentsCancel = "appointments.cancel";
    public const string AppointmentsReschedule = "appointments.reschedule";
    public const string AppointmentsCheckIn = "appointments.check_in";
    public const string AppointmentsComplete = "appointments.complete";

    public const string AppointmentsReadAll = "appointments.read_all";
    public const string AppointmentsCreateAll = "appointments.create_all";
    public const string AppointmentsUpdateAll = "appointments.update_all";
    public const string AppointmentsCancelAll = "appointments.cancel_all";
    public const string AppointmentsRescheduleAll = "appointments.reschedule_all";

    public const string AppointmentsReadAssigned = "appointments.read_assigned";
    public const string AppointmentsCreateAssigned = "appointments.create_assigned";
    public const string AppointmentsUpdateAssigned = "appointments.update_assigned";
    public const string AppointmentsCancelAssigned = "appointments.cancel_assigned";
    public const string AppointmentsRescheduleAssigned = "appointments.reschedule_assigned";

    public const string AppointmentsReadOwn = "appointments.read_own";
    public const string AppointmentsCreateOwn = "appointments.create_own";
    public const string AppointmentsCancelOwn = "appointments.cancel_own";
    public const string AppointmentsRescheduleOwn = "appointments.reschedule_own";

    public const string AppointmentsMarkNoShow = "appointments.mark_no_show";
    public const string AppointmentsOverridePolicy = "appointments.override_policy";

    public const string WaitingListRead = "waiting_list.read";
    public const string WaitingListManage = "waiting_list.manage";

    public const string WaitingRoomRead = "waiting_room.read";
    public const string WaitingRoomManage = "waiting_room.manage";

    public const string MedicalRecordsRead = "medical_records.read";
    public const string MedicalRecordsCreate = "medical_records.create";
    public const string MedicalRecordsUpdate = "medical_records.update";

    public const string PrescriptionsRead = "prescriptions.read";
    public const string PrescriptionsCreate = "prescriptions.create";

    public const string DocumentsRead = "documents.read";
    public const string DocumentsManage = "documents.manage";

    public const string NotificationsRead = "notifications.read";
    public const string NotificationsManage = "notifications.manage";

    public const string PaymentsRead = "payments.read";
    public const string PaymentsManage = "payments.manage";

    public const string ReportsRead = "reports.read";

    public const string AuditRead = "audit.read";

    public const string SettingsManage = "settings.manage";

    public const string ConsentManage = "consent.manage";

    public const string TelemedicineUse = "telemedicine.use";

    public const string WaitingListReadOwn = "waiting_list.read_own";
    public const string WaitingListManageOwn = "waiting_list.manage_own";
    public const string WaitingListReadAssigned = "waiting_list.read_assigned";
    public const string WaitingListReadAll = "waiting_list.read_all";
    public const string WaitingListManageAll = "waiting_list.manage_all";

    public const string ClinicalRecordsRead = "clinical_records.read";
    public const string ClinicalRecordsCreate = "clinical_records.create";
    public const string ClinicalRecordsUpdate = "clinical_records.update";
    public const string ClinicalRecordsFinalize = "clinical_records.finalize";
    public const string ClinicalRecordsCorrect = "clinical_records.correct";
    public const string ClinicalRecordsManageAll = "clinical_records.manage_all";

    public const string PrescriptionsIssue = "prescriptions.issue";
    public const string PrescriptionsCorrect = "prescriptions.correct";
    public const string PrescriptionsRenew = "prescriptions.renew";
    public const string PrescriptionsCancel = "prescriptions.cancel";
    public const string PrescriptionsManageAll = "prescriptions.manage_all";
    public const string PrescriptionsDispense = "prescriptions.dispense";
    public const string PrescriptionsExpire = "prescriptions.expire";

    public static readonly IReadOnlyList<string> All =
    [
        RealtimeTest,
        FoundationTestWrite,
        UsersRead,
        UsersManage,
        RolesManage,
        PermissionsRead,
        PatientsRead,
        PatientsCreate,
        PatientsUpdate,
        PatientsManageOwn,
        DoctorsRead,
        DoctorsManage,
        SpecialtiesRead,
        SpecialtiesManage,
        OrganizationsRead,
        OrganizationsManage,
        ClinicsRead,
        ClinicsManage,
        RoomsRead,
        RoomsManage,
        ResourcesRead,
        ResourcesManage,
        SchedulesRead,
        SchedulesManage,
        AvailabilityRead,
        AppointmentsRead,
        AppointmentsCreate,
        AppointmentsUpdate,
        AppointmentsCancel,
        AppointmentsReschedule,
        AppointmentsCheckIn,
        AppointmentsComplete,
        AppointmentsReadAll,
        AppointmentsCreateAll,
        AppointmentsUpdateAll,
        AppointmentsCancelAll,
        AppointmentsRescheduleAll,
        AppointmentsReadAssigned,
        AppointmentsCreateAssigned,
        AppointmentsUpdateAssigned,
        AppointmentsCancelAssigned,
        AppointmentsRescheduleAssigned,
        AppointmentsReadOwn,
        AppointmentsCreateOwn,
        AppointmentsCancelOwn,
        AppointmentsRescheduleOwn,
        AppointmentsMarkNoShow,
        AppointmentsOverridePolicy,
        WaitingListRead,
        WaitingListManage,
        WaitingRoomRead,
        WaitingRoomManage,
        MedicalRecordsRead,
        MedicalRecordsCreate,
        MedicalRecordsUpdate,
        PrescriptionsRead,
        PrescriptionsCreate,
        DocumentsRead,
        DocumentsManage,
        NotificationsRead,
        NotificationsManage,
        PaymentsRead,
        PaymentsManage,
        ReportsRead,
        AuditRead,
        SettingsManage,
        ConsentManage,
        TelemedicineUse,
        WaitingListReadOwn,
        WaitingListManageOwn,
        WaitingListReadAssigned,
        WaitingListReadAll,
        WaitingListManageAll,
        ClinicalRecordsRead,
        ClinicalRecordsCreate,
        ClinicalRecordsUpdate,
        ClinicalRecordsFinalize,
        ClinicalRecordsCorrect,
        ClinicalRecordsManageAll,
        PrescriptionsIssue,
        PrescriptionsCorrect,
        PrescriptionsRenew,
        PrescriptionsCancel,
        PrescriptionsManageAll,
        PrescriptionsDispense,
        PrescriptionsExpire
    ];
}
