import { useState } from 'react';
import { History } from 'lucide-react';
import { CrudResourcePage } from '../../features/medical-structure/components/CrudResourcePage';
import { ClinicalHistoryDialog } from '../patients/ClinicalHistoryDialog';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { Button } from '../../components/ui/Button';

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
  const { user } = useAuth();
  const canReadHistory = hasPermission(user, Permissions.ClinicalRecordsRead);
  const [historyTarget, setHistoryTarget] = useState<{ id: string; label: string } | null>(null);

  return (
    <>
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
          // 8b: columna de historial por fila, vía la ranura render de CrudColumn.
          // Solo existe si el usuario puede leer historiales; así quien no tiene
          // clinical_records.read no ve ni el header (celdas no quedan vacías).
          ...(canReadHistory
            ? [
              {
                key: 'history',
                header: 'Historial',
                render: (row: any) => (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() =>
                      setHistoryTarget({
                        id: row.id,
                        label:
                          [String(row.firstName ?? ''), String(row.lastName ?? '')]
                            .filter(Boolean)
                            .join(' ') || 'Paciente',
                      })
                    }
                    leftIcon={<History className="h-4 w-4" />}
                  >
                    Historial clínico
                  </Button>
                ),
              },
            ]
            : []),
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

      {canReadHistory ? (
        <ClinicalHistoryDialog
          open={Boolean(historyTarget)}
          patientId={historyTarget?.id ?? null}
          patientLabel={historyTarget?.label ?? ''}
          onClose={() => setHistoryTarget(null)}
        />
      ) : null}
    </>
  );
}