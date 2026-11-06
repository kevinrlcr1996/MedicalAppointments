import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import {
  Activity,
  Building2,
  CalendarCheck,
  CalendarClock,
  CalendarPlus,
  DoorOpen,
  LayoutDashboard,
  LogOut,
  Menu,
  Stethoscope,
  Users,
  X,
  type LucideIcon,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { hasAnyPermission, Permissions, type Permission } from '../../lib/permissions';
import { cn } from '../../utils/cn';
import { Button } from '../ui/Button';
import { StatusBadge } from '../ui/StatusBadge';
import { useSignalR } from '../../hooks/useSignalR';
import { notificationSignalRClient } from '../../services/realtime/signalrClient';

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  permissions: Permission[];
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, permissions: [] },
  { to: '/realtime', label: 'Realtime Test', icon: Activity, permissions: [Permissions.RealtimeTest] },

  {
    to: '/appointments',
    label: 'Citas',
    icon: CalendarCheck,
    permissions: [
      Permissions.AppointmentsReadAll,
      Permissions.AppointmentsReadAssigned,
      Permissions.AppointmentsReadOwn,
    ],
  },
  {
    to: '/appointments/book',
    label: 'Reservar',
    icon: CalendarPlus,
    permissions: [
      Permissions.AppointmentsCreateAll,
      Permissions.AppointmentsCreateAssigned,
      Permissions.AppointmentsCreateOwn,
    ],
  },

  { to: '/availability', label: 'Disponibilidad', icon: CalendarClock, permissions: [Permissions.AvailabilityRead] },
  { to: '/admin/schedules', label: 'Agendas', icon: CalendarClock, permissions: [Permissions.SchedulesRead] },

  { to: '/admin/specialties', label: 'Especialidades', icon: Stethoscope, permissions: [Permissions.SpecialtiesRead] },
  { to: '/admin/organizations', label: 'Organizaciones', icon: Building2, permissions: [Permissions.OrganizationsRead] },
  { to: '/admin/clinics', label: 'Clínicas', icon: Building2, permissions: [Permissions.ClinicsRead] },
  { to: '/admin/rooms', label: 'Consultorios', icon: DoorOpen, permissions: [Permissions.RoomsRead] },
  { to: '/admin/resources', label: 'Recursos', icon: CalendarClock, permissions: [Permissions.ResourcesRead] },
  { to: '/admin/doctors', label: 'Médicos', icon: Users, permissions: [Permissions.DoctorsRead] },
  { to: '/admin/patients', label: 'Pacientes', icon: Users, permissions: [Permissions.PatientsRead] },
];

export function AppLayout() {
  const { user, signOut } = useAuth();
  const { state } = useSignalR(notificationSignalRClient);
  const [mobileOpen, setMobileOpen] = useState(false);

  const visibleItems = navItems.filter(
    (item) => item.permissions.length === 0 || hasAnyPermission(user, item.permissions),
  );

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 backdrop-blur dark:border-slate-800 dark:bg-slate-900/90">
        <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <button
              type="button"
              className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden dark:text-slate-300 dark:hover:bg-slate-800"
              onClick={() => setMobileOpen((open) => !open)}
              aria-label="Toggle navigation"
            >
              {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </button>

            <div className="flex items-center gap-2">
              <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-sky-600 text-white">
                <Stethoscope className="h-5 w-5" />
              </div>
              <div className="hidden sm:block">
                <p className="text-sm font-semibold text-slate-900 dark:text-slate-50">Medical Appointments</p>
                <p className="text-xs text-slate-500 dark:text-slate-400">Fase 5 — Citas</p>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <StatusBadge state={state} />

            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium text-slate-900 dark:text-slate-50">{user?.email}</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {user?.roles.join(', ') || 'Sin roles'}
              </p>
            </div>

            <Button variant="ghost" size="sm" onClick={() => void signOut()} leftIcon={<LogOut className="h-4 w-4" />}>
              Salir
            </Button>
          </div>
        </div>
      </header>

      <div className="flex">
        <aside
          className={cn(
            'fixed inset-y-0 left-0 z-30 w-72 transform border-r border-slate-200 bg-white p-4 transition-transform dark:border-slate-800 dark:bg-slate-900 lg:static lg:translate-x-0',
            mobileOpen ? 'translate-x-0' : '-translate-x-full',
          )}
        >
          <nav className="space-y-1">
            {visibleItems.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  onClick={() => setMobileOpen(false)}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium transition',
                      isActive
                        ? 'bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-200'
                        : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
                    )
                  }
                >
                  <Icon className="h-4 w-4" />
                  {item.label}
                </NavLink>
              );
            })}
          </nav>
        </aside>

        {mobileOpen ? (
          <div
            className="fixed inset-0 z-20 bg-slate-950/40 lg:hidden"
            onClick={() => setMobileOpen(false)}
            aria-hidden="true"
          />
        ) : null}

        <main className="min-w-0 flex-1 p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}