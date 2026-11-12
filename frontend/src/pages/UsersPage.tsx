import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, RefreshCcw, ShieldCheck, Unlock } from 'lucide-react';
import { usersApi, type User } from '../services/api/users';
import { ApiError } from '../services/api/apiClient';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { DataTable, type DataColumn } from '../components/ui/DataTable';
import { Input } from '../components/ui/Input';
import { Modal } from '../components/ui/Modal';
import { PageHeader } from '../components/ui/PageHeader';
import { Pagination } from '../components/ui/Pagination';

const availableRoles = ['SuperAdmin', 'Admin', 'Doctor', 'Pharmacist', 'Receptionist', 'Patient'];
const roleLabels: Record<string, string> = { Pharmacist: 'Farmacia' };

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : 'No se pudo completar la operación.';
}

export function UsersPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [rolesTarget, setRolesTarget] = useState<User | null>(null);
  const [formError, setFormError] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [roles, setRoles] = useState<string[]>(['Patient']);

  const usersQuery = useQuery({
    queryKey: ['users', { page, pageSize, search }],
    queryFn: () => usersApi.list({ page, pageSize, search: search || undefined }),
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['users'] });

  const createMutation = useMutation({
    mutationFn: () => usersApi.create({ email, password, firstName, lastName, roles }),
    onSuccess: async () => {
      setCreateOpen(false);
      setEmail(''); setPassword(''); setFirstName(''); setLastName(''); setRoles(['Patient']); setFormError('');
      await refresh();
    },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const rolesMutation = useMutation({
    mutationFn: () => usersApi.assignRoles(rolesTarget!.id, rolesTarget!.roles),
    onSuccess: async () => { setRolesTarget(null); setFormError(''); await refresh(); },
    onError: (error) => setFormError(errorMessage(error)),
  });

  const unlockMutation = useMutation({
    mutationFn: (id: string) => usersApi.unlock(id),
    onSuccess: refresh,
    onError: (error) => setFormError(errorMessage(error)),
  });

  const columns: DataColumn<User>[] = [
    { key: 'email', header: 'Correo' },
    { key: 'name', header: 'Nombre', render: (row) => [row.firstName, row.lastName].filter(Boolean).join(' ') || '—' },
    { key: 'roles', header: 'Roles', render: (row) => row.roles.join(', ') || 'Sin roles' },
    { key: 'status', header: 'Estado', render: (row) => row.lockoutEnd && new Date(row.lockoutEnd) > new Date() ? 'Bloqueado' : 'Activo' },
    {
      key: 'actions', header: 'Acciones', render: (row) => (
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="secondary" onClick={() => { setRolesTarget({ ...row }); setFormError(''); }} leftIcon={<ShieldCheck className="h-4 w-4" />}>Roles</Button>
          {row.lockoutEnd && new Date(row.lockoutEnd) > new Date() ? <Button size="sm" variant="secondary" onClick={() => unlockMutation.mutate(row.id)} loading={unlockMutation.isPending} leftIcon={<Unlock className="h-4 w-4" />}>Desbloquear</Button> : null}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader title="Usuarios" description="Administra cuentas, roles y bloqueos de acceso." actions={<><Button variant="secondary" onClick={() => void usersQuery.refetch()} leftIcon={<RefreshCcw className="h-4 w-4" />}>Recargar</Button><Button onClick={() => { setCreateOpen(true); setFormError(''); }} leftIcon={<Plus className="h-4 w-4" />}>Nuevo usuario</Button></>} />
      {formError ? <Alert variant="error">{formError}</Alert> : null}
      <Card>
        <form className="mb-4 flex flex-col gap-2 sm:flex-row" onSubmit={(event) => { event.preventDefault(); setPage(1); setSearch(searchInput.trim()); }}>
          <Input value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Buscar por correo o nombre" />
          <Button type="submit">Buscar</Button>
        </form>
        <DataTable columns={columns} rows={usersQuery.data?.items ?? []} loading={usersQuery.isLoading} />
        <div className="mt-4"><Pagination page={page} pageSize={pageSize} totalCount={usersQuery.data?.totalCount ?? 0} onPageChange={setPage} onPageSizeChange={(size) => { setPageSize(size); setPage(1); }} /></div>
      </Card>

      <Modal open={createOpen} title="Nuevo usuario" onClose={() => setCreateOpen(false)} footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={() => setCreateOpen(false)}>Cancelar</Button><Button onClick={() => createMutation.mutate()} loading={createMutation.isPending}>Crear usuario</Button></div>}>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="text-sm font-medium">Correo<Input type="email" value={email} onChange={(event) => setEmail(event.target.value)} /></label>
          <label className="text-sm font-medium">Contraseña<Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
          <label className="text-sm font-medium">Nombre<Input value={firstName} onChange={(event) => setFirstName(event.target.value)} /></label>
          <label className="text-sm font-medium">Apellidos<Input value={lastName} onChange={(event) => setLastName(event.target.value)} /></label>
        </div>
        <RoleSelector roles={roles} onChange={setRoles} />
      </Modal>

      <Modal open={rolesTarget !== null} title="Asignar roles" onClose={() => setRolesTarget(null)} footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={() => setRolesTarget(null)}>Cancelar</Button><Button onClick={() => rolesMutation.mutate()} loading={rolesMutation.isPending}>Guardar roles</Button></div>}>
        <RoleSelector roles={rolesTarget?.roles ?? []} onChange={(next) => setRolesTarget((current) => current ? { ...current, roles: next } : current)} />
      </Modal>
    </div>
  );
}

function RoleSelector({ roles, onChange }: { roles: string[]; onChange: (roles: string[]) => void }) {
  return <fieldset className="mt-5 space-y-2"><legend className="text-sm font-semibold">Roles</legend>{availableRoles.map((role) => <label key={role} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={roles.includes(role)} onChange={(event) => onChange(event.target.checked ? [...roles, role] : roles.filter((item) => item !== role))} />{roleLabels[role] ?? role}</label>)}</fieldset>;
}
