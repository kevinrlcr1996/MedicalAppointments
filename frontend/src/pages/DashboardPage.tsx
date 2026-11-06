import { Link } from 'react-router-dom';
import {
  Activity,
  Building2,
  CalendarClock,
  DoorOpen,
  Stethoscope,
  Users,
} from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { hasPermission, Permissions } from '../lib/permissions';
import { Badge } from '../components/ui/Badge';
import { Card } from '../components/ui/Card';
import { PageHeader } from '../components/ui/PageHeader';

const cards = [
  {
    title: 'Especialidades',
    description: 'Administra el catálogo de especialidades médicas.',
    to: '/admin/specialties',
    icon: Stethoscope,
    permission: Permissions.SpecialtiesRead,
  },
  {
    title: 'Organizaciones',
    description: 'Gestiona organizaciones madre del sistema.',
    to: '/admin/organizations',
    icon: Building2,
    permission: Permissions.OrganizationsRead,
  },
  {
    title: 'Clínicas',
    description: 'Administra clínicas por organización.',
    to: '/admin/clinics',
    icon: Building2,
    permission: Permissions.ClinicsRead,
  },
  {
    title: 'Consultorios',
    description: 'Gestiona rooms físicos de atención.',
    to: '/admin/rooms',
    icon: DoorOpen,
    permission: Permissions.RoomsRead,
  },
  {
    title: 'Recursos',
    description: 'Administra recursos reservables.',
    to: '/admin/resources',
    icon: CalendarClock,
    permission: Permissions.ResourcesRead,
  },
  {
    title: 'Médicos',
    description: 'Gestiona perfiles médicos, especialidades y clínicas.',
    to: '/admin/doctors',
    icon: Users,
    permission: Permissions.DoctorsRead,
  },
  {
    title: 'Pacientes',
    description: 'Administra fichas de pacientes.',
    to: '/admin/patients',
    icon: Users,
    permission: Permissions.PatientsRead,
  },
  {
    title: 'Realtime Test',
    description: 'Prueba SignalR autenticado + Outbox.',
    to: '/realtime',
    icon: Activity,
    permission: Permissions.RealtimeTest,
  },
];

export function DashboardPage() {
  const { user } = useAuth();

  const visibleCards = cards.filter((card) => hasPermission(user, card.permission));

  return (
    <div className="space-y-6">
      <PageHeader
        title="Dashboard"
        description="Estructura médica base del sistema."
        actions={
          <Badge variant="info">
            {user?.roles.join(' · ') || 'Sin roles'}
          </Badge>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {visibleCards.map((card) => {
          const Icon = card.icon;

          return (
            <Link key={card.to} to={card.to} className="block">
              <Card className="h-full transition hover:border-sky-300 hover:shadow-md dark:hover:border-sky-800">
                <div className="flex items-start gap-4">
                  <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-sky-100 text-sky-700 dark:bg-sky-950 dark:text-sky-200">
                    <Icon className="h-5 w-5" />
                  </div>

                  <div>
                    <h2 className="font-semibold text-slate-900 dark:text-slate-50">{card.title}</h2>
                    <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">{card.description}</p>
                  </div>
                </div>
              </Card>
            </Link>
          );
        })}
      </div>
    </div>
  );
}