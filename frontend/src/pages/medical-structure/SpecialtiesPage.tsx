import { CrudResourcePage } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';

export function SpecialtiesPage() {
  return (
    <CrudResourcePage
      title="Especialidades"
      description="Catálogo de especialidades médicas."
      endpoint="/api/v1/specialties"
      queryKey="specialties"
      readPermission={Permissions.SpecialtiesRead}
      managePermission={Permissions.SpecialtiesManage}
      canStatus
      columns={[
        { key: 'name', header: 'Nombre' },
        { key: 'code', header: 'Código' },
        { key: 'description', header: 'Descripción' },
      ]}
      fields={[
        { name: 'name', label: 'Nombre', type: 'text', required: true, maxLength: 200 },
        { name: 'code', label: 'Código', type: 'text', maxLength: 100 },
        { name: 'description', label: 'Descripción', type: 'textarea', maxLength: 1000 },
        { name: 'isActive', label: 'Activo', type: 'switch' },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre o código' },
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