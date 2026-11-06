import { CrudResourcePage } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';

export function OrganizationsPage() {
  return (
    <CrudResourcePage
      title="Organizaciones"
      description="Organizaciones que agrupan clínicas."
      endpoint="/api/v1/organizations"
      queryKey="organizations"
      readPermission={Permissions.OrganizationsRead}
      managePermission={Permissions.OrganizationsManage}
      canStatus
      columns={[
        { key: 'name', header: 'Nombre' },
        { key: 'taxId', header: 'Tax ID' },
        { key: 'city', header: 'Ciudad' },
        { key: 'country', header: 'País' },
      ]}
      fields={[
        { name: 'name', label: 'Nombre', type: 'text', required: true, maxLength: 200 },
        { name: 'legalName', label: 'Razón social', type: 'text', maxLength: 200 },
        { name: 'taxId', label: 'Tax ID', type: 'text', maxLength: 100 },
        { name: 'email', label: 'Email', type: 'text', maxLength: 320 },
        { name: 'phone', label: 'Teléfono', type: 'text', maxLength: 50 },
        { name: 'address', label: 'Dirección', type: 'text', maxLength: 500 },
        { name: 'city', label: 'Ciudad', type: 'text', maxLength: 200 },
        { name: 'country', label: 'País', type: 'text', maxLength: 100 },
        { name: 'isActive', label: 'Activo', type: 'switch' },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre o tax id' },
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