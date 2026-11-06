import { CrudResourcePage } from '../../features/medical-structure/components/CrudResourcePage';
import { Permissions } from '../../lib/permissions';

const genderOptions = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'Male', label: 'Male' },
  { value: 'Female', label: 'Female' },
  { value: 'Other', label: 'Other' },
];

const bloodTypeOptions = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'APositive', label: 'APositive' },
  { value: 'ANegative', label: 'ANegative' },
  { value: 'BPositive', label: 'BPositive' },
  { value: 'BNegative', label: 'BNegative' },
  { value: 'ABPositive', label: 'ABPositive' },
  { value: 'ABNegative', label: 'ABNegative' },
  { value: 'OPositive', label: 'OPositive' },
  { value: 'ONegative', label: 'ONegative' },
];

export function PatientsPage() {
  return (
    <CrudResourcePage
      title="Pacientes"
      description="Fichas básicas de pacientes. No almacenes datos clínicos sensibles aquí."
      endpoint="/api/v1/patients"
      queryKey="patients"
      readPermission={Permissions.PatientsRead}
      managePermission={Permissions.PatientsCreate}
      columns={[
        { key: 'firstName', header: 'Nombre' },
        { key: 'lastName', header: 'Apellido' },
        { key: 'identificationNumber', header: 'Identificación' },
        { key: 'phone', header: 'Teléfono' },
        { key: 'email', header: 'Email' },
      ]}
      fields={[
        { name: 'firstName', label: 'Nombre', type: 'text', required: true, maxLength: 200 },
        { name: 'lastName', label: 'Apellido', type: 'text', required: true, maxLength: 200 },
        { name: 'dateOfBirth', label: 'Fecha nacimiento', type: 'date' },
        { name: 'gender', label: 'Género', type: 'select', required: true, options: genderOptions },
        { name: 'identificationNumber', label: 'Identificación', type: 'text', maxLength: 100 },
        { name: 'phone', label: 'Teléfono', type: 'text', maxLength: 50 },
        { name: 'email', label: 'Email', type: 'text', maxLength: 320 },
        { name: 'address', label: 'Dirección', type: 'text', maxLength: 500 },
        { name: 'emergencyContact', label: 'Contacto emergencia', type: 'text', maxLength: 200 },
        { name: 'emergencyPhone', label: 'Teléfono emergencia', type: 'text', maxLength: 50 },
        { name: 'bloodType', label: 'Tipo sangre', type: 'select', required: true, options: bloodTypeOptions },
        { name: 'insuranceProvider', label: 'Aseguradora', type: 'text', maxLength: 200 },
        { name: 'insuranceNumber', label: 'Número póliza', type: 'text', maxLength: 100 },
      ]}
      filters={[
        { name: 'search', label: 'Buscar', type: 'text', placeholder: 'Nombre, identificación, email o teléfono' },
      ]}
    />
  );
}