import { useQuery } from '@tanstack/react-query';
import { crudApi } from '../../services/api/crudApi';
import { CrudResourcePage, type SelectOption } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';
import { Spinner } from '../../components/ui/Spinner';

interface ClinicOption {
  id: string;
  name: string;
}

const resourceKindOptions: SelectOption[] = [
  { value: '', label: 'Todos' },
  { value: 'Room', label: 'Room' },
  { value: 'Equipment', label: 'Equipment' },
  { value: 'Vehicle', label: 'Vehicle' },
  { value: 'Space', label: 'Space' },
  { value: 'Other', label: 'Other' },
];

export function ResourcesPage() {
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
      title="Recursos"
      description="Recursos reservables: equipos, salas, espacios."
      endpoint="/api/v1/resources"
      queryKey="resources"
      readPermission={Permissions.ResourcesRead}
      managePermission={Permissions.ResourcesManage}
      canStatus
      columns={[
        { key: 'name', header: 'Nombre' },
        { key: 'clinicName', header: 'Clínica' },
        { key: 'code', header: 'Código' },
        { key: 'kind', header: 'Tipo' },
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
        {
          name: 'kind',
          label: 'Tipo',
          type: 'select',
          required: true,
          options: resourceKindOptions.filter((option) => option.value !== ''),
        },
        { name: 'description', label: 'Descripción', type: 'textarea', maxLength: 1000 },
        { name: 'isActive', label: 'Activo', type: 'switch' },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre o código' },
        { name: 'clinicId', label: 'Clínica', type: 'select', options: clinicOptions },
        { name: 'kind', label: 'Tipo', type: 'select', options: resourceKindOptions },
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