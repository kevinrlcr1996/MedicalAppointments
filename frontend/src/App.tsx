import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './components/layout/AppLayout';
import { ProtectedRoute } from './components/navigation/ProtectedRoute';
import { RequirePermission } from './components/navigation/RequirePermission';
import { AuthLayout } from './layouts/AuthLayout';
import { ConfirmEmailPage } from './pages/ConfirmEmailPage';
import { DashboardPage } from './pages/DashboardPage';
import { ForbiddenPage } from './pages/ForbiddenPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import { FoundationRealtimePage } from './pages/FoundationRealtimePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ResetPasswordPage } from './pages/ResetPasswordPage';
import { ClinicsPage } from './pages/medical-structure/ClinicsPage';
import { DoctorsPage } from './pages/medical-structure/DoctorsPage';
import { OrganizationsPage } from './pages/medical-structure/OrganizationsPage';
import { PatientsPage } from './pages/medical-structure/PatientsPage';
import { ResourcesPage } from './pages/medical-structure/ResourcesPage';
import { RoomsPage } from './pages/medical-structure/RoomsPage';
import { SpecialtiesPage } from './pages/medical-structure/SpecialtiesPage';
import { Permissions } from './lib/permissions';
import { SchedulesPage } from './pages/medical-structure/SchedulesPage';
import { AvailabilityPage } from './pages/scheduling/AvailabilityPage';
import { RequireAnyPermission } from './components/navigation/RequireAnyPermission';
import { AppointmentsPage } from './pages/appointments/AppointmentsPage';
import { BookAppointmentPage } from './pages/appointments/BookAppointmentPage';
import { WaitingListPage } from './pages/waiting-list/WaitingListPage';
import { WaitingRoomDisplayPage } from './pages/waiting-room/WaitingRoomDisplayPage';
import { WaitingRoomBoardPage } from './pages/waiting-room/WaitingRoomBoardPage';

export default function App() {
  return (
    <Routes>
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/realtime" element={<FoundationRealtimePage />} />

          <Route element={<RequirePermission permission={Permissions.SpecialtiesRead} />}>
            <Route path="/admin/specialties" element={<SpecialtiesPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.OrganizationsRead} />}>
            <Route path="/admin/organizations" element={<OrganizationsPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.ClinicsRead} />}>
            <Route path="/admin/clinics" element={<ClinicsPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.RoomsRead} />}>
            <Route path="/admin/rooms" element={<RoomsPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.ResourcesRead} />}>
            <Route path="/admin/resources" element={<ResourcesPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.DoctorsRead} />}>
            <Route path="/admin/doctors" element={<DoctorsPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.PatientsRead} />}>
            <Route path="/admin/patients" element={<PatientsPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.SchedulesRead} />}>
            <Route path="/admin/schedules" element={<SchedulesPage />} />
          </Route>

          <Route element={<RequirePermission permission={Permissions.AvailabilityRead} />}>
            <Route path="/availability" element={<AvailabilityPage />} />
          </Route>

          <Route
            element={
              <RequireAnyPermission
                permissions={[
                  Permissions.WaitingListReadAll,
                  Permissions.WaitingListReadAssigned,
                  Permissions.WaitingListReadOwn,
                ]}
              />
            }
          >
            <Route path="/waiting-list" element={<WaitingListPage />} />
          </Route>

          <Route element={<RequireAnyPermission permissions={[Permissions.WaitingRoomRead, Permissions.WaitingRoomManage, Permissions.AppointmentsCheckIn, Permissions.AppointmentsComplete, Permissions.AppointmentsMarkNoShow]} />}>
            <Route path="/waiting-room" element={<WaitingRoomBoardPage />} />
          </Route>
          <Route element={<RequireAnyPermission permissions={[Permissions.WaitingRoomRead, Permissions.WaitingRoomManage]} />}>
            <Route path="/waiting-room/display" element={<WaitingRoomDisplayPage />} />
          </Route>

          <Route path="/forbidden" element={<ForbiddenPage />} />
        </Route>
        <Route
          element={
            <RequireAnyPermission
              permissions={[
                Permissions.AppointmentsReadAll,
                Permissions.AppointmentsReadAssigned,
                Permissions.AppointmentsReadOwn,
              ]}
            />
          }
        >
          <Route path="/appointments" element={<AppointmentsPage />} />
        </Route>

        <Route
          element={
            <RequireAnyPermission
              permissions={[
                Permissions.AppointmentsCreateAll,
                Permissions.AppointmentsCreateAssigned,
                Permissions.AppointmentsCreateOwn,
              ]}
            />
          }
        >
          <Route path="/appointments/book" element={<BookAppointmentPage />} />
        </Route>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}