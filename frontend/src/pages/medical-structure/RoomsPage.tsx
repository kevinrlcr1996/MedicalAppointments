import { useQuery } from '@tanstack/react-query';
import { crudApi } from '../../services/api/crudApi';
import { CrudResourcePage, type SelectOption } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';
import { Spinner } from '../../components/ui/Spinner';

interface ClinicOption {
  id: string;
  name: string;
}

export function RoomsPage() {
  const clinicsQuery = useQuery({
    queryKey: ['clinics-options'],
    queryFn: () => crudApi.list<ClinicOption>('/api/v1/clinics', { page: 1, pageSize: 100, isActive: true }),
  });

  if (clinicsQuery.isLoading) {
    return <Spinner label="Cargando clínicas..." />;
  }

  const clinicOptions: SelectOption[] = [
    { value: '', label: 'Todas' },
    ...(clinicsQuery.data?.items ?? []).map((clinic) => ({
      value: clinic.id,
      label: clinic.name,
    })),
  ];

  return (
    <CrudResourcePage
      title="Consultorios"
      description="Rooms físicos dentro de una clínica."
      endpoint="/api/v1/rooms"
      queryKey="rooms"
      readPermission={Permissions.RoomsRead}
      managePermission={Permissions.RoomsManage}
      canStatus
      columns={[
        { key: 'name', header: 'Nombre' },
        { key: 'clinicName', header: 'Clínica' },
        { key: 'code', header: 'Código' },
        { key: 'capacity', header: 'Capacidad' },
      ]}
      fields={[
        {
          name: 'clinicId',
          label: 'Clínica',
          type: 'select',
          required: true,
          options: clinicOptions.filter((option) => option.value !== ''),
        },
        { name: 'name', label: 'Nombre', type: 'text', required: true, maxLength: 200 },
        { name: 'code', label: 'Código', type: 'text', maxLength: 100 },
        { name: 'capacity', label: 'Capacidad', type: 'number' },
        { name: 'notes', label: 'Notas', type: 'textarea', maxLength: 1000 },
        { name: 'isActive', label: 'Activo', type: 'switch' },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre o código' },
        { name: 'clinicId', label: 'Clínica', type: 'select', options: clinicOptions },
        {
          name: 'isActive',
          label: 'Estado',
          type: 'select',
          options: [
            { value: '', label: 'Todos' },
            { value: 'true', label: 'Activos' },
            { value: 'false', label: 'Inactivos' },
          ],
        },
      ]}
    />
  );
}