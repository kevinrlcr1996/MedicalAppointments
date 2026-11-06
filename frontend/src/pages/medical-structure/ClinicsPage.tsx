import { useQuery } from '@tanstack/react-query';
import { crudApi } from '../../services/api/crudApi';
import { CrudResourcePage, type SelectOption } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';
import { Spinner } from '../../components/ui/Spinner';

interface OrganizationOption {
  id: string;
  name: string;
}

export function ClinicsPage() {
  const organizationsQuery = useQuery({
    queryKey: ['organizations-options'],
    queryFn: () => crudApi.list<OrganizationOption>('/api/v1/organizations', { page: 1, pageSize: 100, isActive: true }),
  });

  if (organizationsQuery.isLoading) {
    return <Spinner label="Cargando organizaciones..." />;
  }

  const organizationOptions: SelectOption[] = [
    { value: '', label: 'Todas' },
    ...(organizationsQuery.data?.items ?? []).map((organization) => ({
      value: organization.id,
      label: organization.name,
    })),
  ];

  return (
    <CrudResourcePage
      title="Clínicas"
      description="Clínicas asociadas a organizaciones."
      endpoint="/api/v1/clinics"
      queryKey="clinics"
      readPermission={Permissions.ClinicsRead}
      managePermission={Permissions.ClinicsManage}
      canStatus
      columns={[
        { key: 'name', header: 'Nombre' },
        { key: 'organizationName', header: 'Organización' },
        { key: 'code', header: 'Código' },
        { key: 'city', header: 'Ciudad' },
      ]}
      fields={[
        {
          name: 'organizationId',
          label: 'Organización',
          type: 'select',
          required: true,
          options: organizationOptions.filter((option) => option.value !== ''),
        },
        { name: 'name', label: 'Nombre', type: 'text', required: true, maxLength: 200 },
        { name: 'code', label: 'Código', type: 'text', maxLength: 100 },
        { name: 'description', label: 'Descripción', type: 'textarea', maxLength: 1000 },
        { name: 'address', label: 'Dirección', type: 'text', maxLength: 500 },
        { name: 'city', label: 'Ciudad', type: 'text', maxLength: 200 },
        { name: 'country', label: 'País', type: 'text', maxLength: 100 },
        { name: 'phone', label: 'Teléfono', type: 'text', maxLength: 50 },
        { name: 'email', label: 'Email', type: 'text', maxLength: 320 },
        { name: 'timeZoneId', label: 'Time Zone', type: 'text', maxLength: 100, placeholder: 'Europe/Madrid' },
        { name: 'isActive', label: 'Activo', type: 'switch' },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre o código' },
        { name: 'organizationId', label: 'Organización', type: 'select', options: organizationOptions },
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