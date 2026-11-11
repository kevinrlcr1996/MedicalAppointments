import { useEffect, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import {
  Activity,
  Building2,
  CalendarCheck,
  CalendarClock,
  CalendarPlus,
  DoorOpen,
  Hourglass,
  LayoutDashboard,
  LogOut,
  Menu,
  MonitorPlay,
  PanelLeftClose, // FIX-sidebar (flaggeado): si tu lucide no lo exporta, usa ChevronsLeft o reusa Menu/X
  PanelLeftOpen,  // FIX-sidebar (flaggeado): idem
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
  {
    to: '/waiting-list',
    label: 'Lista de espera',
    icon: Hourglass,
    permissions: [
      Permissions.WaitingListReadAll,
      Permissions.WaitingListReadAssigned,
      Permissions.WaitingListReadOwn,
    ],
  },
  { to: '/waiting-room', label: 'Sala de espera', icon: DoorOpen, permissions: [Permissions.WaitingRoomRead, Permissions.WaitingRoomManage, Permissions.AppointmentsCheckIn, Permissions.AppointmentsComplete, Permissions.AppointmentsMarkNoShow] },
  { to: '/waiting-room/display', label: 'Pantalla de sala', icon: MonitorPlay, permissions: [Permissions.WaitingRoomRead, Permissions.WaitingRoomManage] },
];

const SIDEBAR_KEY = 'sidebar-collapsed';

export function AppLayout() {
  const { user, signOut } = useAuth();
  const { state } = useSignalR(notificationSignalRClient);
  const [mobileOpen, setMobileOpen] = useState(false);

  // FIX-sidebar: colapso SOLO escritorio, persistido. Initializer lazy (browser-only, igual que
  // tu `useState(() => new Date()...)` en AvailabilityPage). No afecta el drawer móvil.
  const [collapsed, setCollapsed] = useState<boolean>(
    () => typeof window !== 'undefined' && window.localStorage.getItem(SIDEBAR_KEY) === '1',
  );

  useEffect(() => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(SIDEBAR_KEY, collapsed ? '1' : '0');
    }
  }, [collapsed]);

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
        {/* FIX-sidebar: base w-72 (drawer móvil intacto); en lg alterna w-72 <-> w-20 según collapsed.
            transition incluye width para animar el colapso de escritorio; transform sigue para el móvil. */}
        <aside
          className={cn(
            'fixed inset-y-0 left-0 z-30 w-72 transform border-r border-slate-200 bg-white p-4 transition-[transform,width] dark:border-slate-800 dark:bg-slate-900 lg:static lg:translate-x-0',
            collapsed ? 'lg:w-20' : 'lg:w-72',
            mobileOpen ? 'translate-x-0' : '-translate-x-full',
          )}
        >
          {/* Toggle de colapso: solo escritorio (hidden lg:flex). Centrado cuando está contraído. */}
          <div className={cn('mb-3 hidden lg:flex', collapsed ? 'justify-center' : 'items-center justify-between')}>
            {!collapsed ? (
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-400 dark:text-slate-500">Menú</span>
            ) : null}
            <button
              type="button"
              onClick={() => setCollapsed((c) => !c)}
              className="rounded-lg p-1.5 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-600 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-100"
              aria-label={collapsed ? 'Expandir menú lateral' : 'Contraer menú lateral'}
              aria-expanded={!collapsed}
              title={collapsed ? 'Expandir menú' : 'Contraer menú'}
            >
              {collapsed ? <PanelLeftOpen className="h-4 w-4" /> : <PanelLeftClose className="h-4 w-4" />}
            </button>
          </div>

          <nav className="space-y-1">
            {visibleItems.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end
                  onClick={() => setMobileOpen(false)}
                  title={item.label}            // tooltip al hacer hover cuando está contraído (cero deps)
                  aria-label={item.label}       // SR conserva el nombre aunque el label se oculte con lg:hidden
                  className={({ isActive }) =>
                    cn(
                      // base = look expandido (móvil siempre así); el colapso es lg-only
                      'flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium transition',
                      collapsed && 'lg:justify-center lg:gap-0 lg:px-2',
                      isActive
                        ? 'bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-200'
                        : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
                    )
                  }
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {/* label en el DOM siempre; se oculta SOLO en lg contraído => el drawer móvil no lo pierde */}
                  <span className={cn('truncate', collapsed && 'lg:hidden')}>{item.label}</span>
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

        {/* min-w-0 YA estaba => las tablas no revientan al estrechar el sidebar. No se toca. */}
        <main className="min-w-0 flex-1 p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}