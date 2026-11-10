import { useState } from 'react';
import { AlertTriangle, History, Pill } from 'lucide-react'; // 8d.3/8d.4: iconos de botón
import { CrudResourcePage } from '../../features/medical-structure/components/CrudResourcePage';
import { ClinicalHistoryDialog } from '../patients/ClinicalHistoryDialog';
import { PatientAllergiesDialog } from '../patients/PatientAllergiesDialog'; // 8d.3
import { PatientMedicationsDialog } from '../patients/PatientMedicationsDialog'; // 8d.4
import { usePatientAllergiesRealtime } from '../../hooks/usePatientAllergiesRealtime'; // 8d.3
import { usePatientMedicationsRealtime } from '../../hooks/usePatientMedicationsRealtime'; // 8d.4
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
  // Realtime de standing data montado EN ESTE SITIO (los dialogs solo viven aquí,
  // igual que vitales/diagnósticos en AppointmentsPage). NO lo montes también en el layout,
  // o subscribirás dos veces e invalidarás de más.
  usePatientAllergiesRealtime();
  usePatientMedicationsRealtime();

  const { user } = useAuth();
  const canReadHistory = hasPermission(user, Permissions.ClinicalRecordsRead);
  const [historyTarget, setHistoryTarget] = useState<{ id: string; label: string } | null>(null);
  const [allergiesTarget, setAllergiesTarget] = useState<{ id: string; label: string } | null>(null); // 8d.3
  const [medicationsTarget, setMedicationsTarget] = useState<{ id: string; label: string } | null>(null); // 8d.4

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
          // 8d.3/8d.4: alergias y medicación entran en el MISMO spread condicional,
          // bajo el MISMO canReadHistory (clinical_records.read), con el MISMO cálculo
          // de label inline (no refactorizado a helper: el bloque de 8b queda byte-intacto).
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
              {
                key: 'allergies', // 8d.3
                header: 'Alergias',
                render: (row: any) => (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() =>
                      setAllergiesTarget({
                        id: row.id,
                        label:
                          [String(row.firstName ?? ''), String(row.lastName ?? '')]
                            .filter(Boolean)
                            .join(' ') || 'Paciente',
                      })
                    }
                    leftIcon={<AlertTriangle className="h-4 w-4" />}
                  >
                    Alergias
                  </Button>
                ),
              },
              {
                key: 'medications', // 8d.4
                header: 'Medicación',
                render: (row: any) => (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() =>
                      setMedicationsTarget({
                        id: row.id,
                        label:
                          [String(row.firstName ?? ''), String(row.lastName ?? '')]
                            .filter(Boolean)
                            .join(' ') || 'Paciente',
                      })
                    }
                    leftIcon={<Pill className="h-4 w-4" />}
                  >
                    Medicación
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
        <>
          <ClinicalHistoryDialog
            open={Boolean(historyTarget)}
            patientId={historyTarget?.id ?? null}
            patientLabel={historyTarget?.label ?? ''}
            onClose={() => setHistoryTarget(null)}
          />
          <PatientAllergiesDialog
            open={Boolean(allergiesTarget)}
            patientId={allergiesTarget?.id ?? null}
            patientLabel={allergiesTarget?.label ?? ''}
            onClose={() => setAllergiesTarget(null)}
          />
          <PatientMedicationsDialog
            open={Boolean(medicationsTarget)}
            patientId={medicationsTarget?.id ?? null}
            patientLabel={medicationsTarget?.label ?? ''}
            onClose={() => setMedicationsTarget(null)}
          />
        </>
      ) : null}
    </>
  );
}