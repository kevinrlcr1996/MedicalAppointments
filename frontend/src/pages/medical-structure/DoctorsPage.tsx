import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Pencil, Plus, RefreshCcw, Save } from 'lucide-react';
import { apiClient, ApiError, type ValidationError } from '../../services/api/apiClient';
import { crudApi } from '../../services/api/crudApi';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { FormField } from '../../components/ui/FormField';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';
import { TagsInput } from '../../components/ui/TagsInput';
import { Textarea } from '../../components/ui/Textarea';

interface DoctorSpecialty {
  specialtyId: string;
  name: string;
  code?: string | null;
}

interface DoctorClinic {
  clinicId: string;
  clinicName: string;
  clinicCode?: string | null;
  isPrimary: boolean;
  startDate?: string | null;
  endDate?: string | null;
}

interface Doctor {
  id: string;
  userId?: string | null;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  licenseNumber: string;
  licenseCountry: string;
  biography?: string | null;
  photoUrl?: string | null;
  gender: string;
  dateOfBirth?: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  specialties: DoctorSpecialty[];
  clinics: DoctorClinic[];
  languages: string[];
}

interface SpecialtyOption {
  id: string;
  name: string;
}

interface ClinicOption {
  id: string;
  name: string;
}

const genderOptions = [
  { value: 'Unknown', label: 'Unknown' },
  { value: 'Male', label: 'Male' },
  { value: 'Female', label: 'Female' },
  { value: 'Other', label: 'Other' },
];

const statusOptions = [
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' },
  { value: 'Suspended', label: 'Suspended' },
];

const doctorSchema = z.object({
  firstName: z.string().trim().min(1, 'Nombre obligatorio.').max(200),
  lastName: z.string().trim().min(1, 'Apellido obligatorio.').max(200),
  email: z.string().trim().email('Email inválido.').max(320).optional().or(z.literal('')),
  phone: z.string().trim().max(50).optional().or(z.literal('')),
  licenseNumber: z.string().trim().min(1, 'Licencia obligatoria.').max(100),
  licenseCountry: z.string().trim().min(2, 'País obligatorio.').max(2),
  biography: z.string().trim().max(4000).optional().or(z.literal('')),
  photoUrl: z.string().trim().max(2048).optional().or(z.literal('')),
  gender: z.string().min(1),
  dateOfBirth: z.union([z.literal(''), z.string().regex(/^\d{4}-\d{2}-\d{2}$/)]).optional(),
  status: z.string().min(1),
});

type DoctorFormValues = z.infer<typeof doctorSchema>;

export function DoctorsPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [specialtyId, setSpecialtyId] = useState('');
  const [clinicId, setClinicId] = useState('');
  const [status, setStatus] = useState('');

  const [modalOpen, setModalOpen] = useState(false);
  const [editingDoctor, setEditingDoctor] = useState<Doctor | null>(null);

  const [selectedSpecialtyIds, setSelectedSpecialtyIds] = useState<string[]>([]);
  const [selectedClinicIds, setSelectedClinicIds] = useState<string[]>([]);
  const [primaryClinicId, setPrimaryClinicId] = useState('');
  const [languages, setLanguages] = useState<string[]>([]);

  const canManage = hasPermission(user, Permissions.DoctorsManage);

  const queryParams = useMemo(
    () => ({
      page,
      pageSize,
      search: search || undefined,
      specialtyId: specialtyId || undefined,
      clinicId: clinicId || undefined,
      status: status || undefined,
    }),
    [page, pageSize, search, specialtyId, clinicId, status],
  );

  const doctorsQuery = useQuery({
    queryKey: ['doctors', queryParams],
    queryFn: () => crudApi.list<Doctor>('/api/v1/doctors', queryParams),
    enabled: hasPermission(user, Permissions.DoctorsRead),
  });

  const specialtiesQuery = useQuery({
    queryKey: ['specialties-options'],
    queryFn: () => crudApi.list<SpecialtyOption>('/api/v1/specialties', { page: 1, pageSize: 100, isActive: true }),
  });

  const clinicsQuery = useQuery({
    queryKey: ['clinics-options'],
    queryFn: () => crudApi.list<ClinicOption>('/api/v1/clinics', { page: 1, pageSize: 100, isActive: true }),
  });

  const form = useForm<DoctorFormValues>({
    resolver: zodResolver(doctorSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      licenseNumber: '',
      licenseCountry: 'ES',
      biography: '',
      photoUrl: '',
      gender: 'Unknown',
      dateOfBirth: '',
      status: 'Active',
    },
  });

  useEffect(() => {
    if (!modalOpen) {
      setSelectedSpecialtyIds([]);
      setSelectedClinicIds([]);
      setPrimaryClinicId('');
      setLanguages([]);
      form.reset();
    }
  }, [modalOpen, form]);

  const openCreate = () => {
    setEditingDoctor(null);
    form.reset();
    setModalOpen(true);
  };

  const openEdit = (doctor: Doctor) => {
    setEditingDoctor(doctor);
    form.reset({
      firstName: doctor.firstName,
      lastName: doctor.lastName,
      email: doctor.email ?? '',
      phone: doctor.phone ?? '',
      licenseNumber: doctor.licenseNumber,
      licenseCountry: doctor.licenseCountry,
      biography: doctor.biography ?? '',
      photoUrl: doctor.photoUrl ?? '',
      gender: doctor.gender,
      dateOfBirth: doctor.dateOfBirth ?? '',
      status: doctor.status,
    });
    setSelectedSpecialtyIds(doctor.specialties.map((x) => x.specialtyId));
    setSelectedClinicIds(doctor.clinics.map((x) => x.clinicId));
    setPrimaryClinicId(doctor.clinics.find((x) => x.isPrimary)?.clinicId ?? '');
    setLanguages(doctor.languages);
    setModalOpen(true);
  };

  const saveMutation = useMutation({
    mutationFn: async (values: DoctorFormValues) => {
      const payload = {
        userId: null,
        ...values,
        dateOfBirth: values.dateOfBirth === '' ? null : values.dateOfBirth,
        email: values.email === '' ? null : values.email,
        phone: values.phone === '' ? null : values.phone,
        biography: values.biography === '' ? null : values.biography,
        photoUrl: values.photoUrl === '' ? null : values.photoUrl,
        specialtyIds: selectedSpecialtyIds,
        clinics: selectedClinicIds.map((id) => ({
          clinicId: id,
          isPrimary: id === primaryClinicId,
          startDate: null,
          endDate: null,
        })),
        languages,
      };

      if (editingDoctor) {
        return apiClient.put<Doctor>(`/api/v1/doctors/${editingDoctor.id}`, payload);
      }

      return apiClient.post<Doctor>('/api/v1/doctors', payload);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['doctors'] });
      setModalOpen(false);
    },
    onError: (error) => {
      if (!(error instanceof ApiError)) {
        return;
      }

      const validationErrors = error.problem?.errors;
      const normalizedErrors: ValidationError[] = Array.isArray(validationErrors)
        ? validationErrors
        : Object.entries(validationErrors ?? {}).flatMap(([propertyName, messages]) =>
            messages.map((errorMessage) => ({ propertyName, errorMessage })),
          );

      for (const validationError of normalizedErrors) {
        const fieldName = Object.keys(doctorSchema.shape).find(
          (name) => name.toLowerCase() === validationError.propertyName.toLowerCase(),
        );

        if (fieldName) {
          form.setError(fieldName as keyof DoctorFormValues, {
            type: 'server',
            message: validationError.errorMessage,
          });
        }
      }
    },
  });

  if (specialtiesQuery.isLoading || clinicsQuery.isLoading) {
    return <Spinner label="Cargando catálogos..." />;
  }

  const specialtyOptions = (specialtiesQuery.data?.items ?? []).map((specialty) => ({
    value: specialty.id,
    label: specialty.name,
  }));

  const clinicOptions = (clinicsQuery.data?.items ?? []).map((clinic) => ({
    value: clinic.id,
    label: clinic.name,
  }));

  return (
    <div className="space-y-6">
      <PageHeader
        title="Médicos"
        description="Perfiles médicos, especialidades, clínicas e idiomas."
        actions={
          canManage ? (
            <Button onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              Nuevo médico
            </Button>
          ) : null
        }
      />

      <Card>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          <FormField label="Buscar" htmlFor="doctor-search">
            <Input
              id="doctor-search"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
              placeholder="Nombre, email, licencia..."
            />
          </FormField>

          <FormField label="Especialidad" htmlFor="doctor-specialty">
            <Select
              id="doctor-specialty"
              value={specialtyId}
              onChange={(event) => {
                setSpecialtyId(event.target.value);
                setPage(1);
              }}
              options={[{ value: '', label: 'Todas' }, ...specialtyOptions]}
            />
          </FormField>

          <FormField label="Clínica" htmlFor="doctor-clinic">
            <Select
              id="doctor-clinic"
              value={clinicId}
              onChange={(event) => {
                setClinicId(event.target.value);
                setPage(1);
              }}
              options={[{ value: '', label: 'Todas' }, ...clinicOptions]}
            />
          </FormField>

          <FormField label="Estado" htmlFor="doctor-status">
            <Select
              id="doctor-status"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value);
                setPage(1);
              }}
              options={[{ value: '', label: 'Todos' }, ...statusOptions]}
            />
          </FormField>

          <div className="flex items-end">
            <Button
              variant="ghost"
              onClick={() => {
                setSearch('');
                setSpecialtyId('');
                setClinicId('');
                setStatus('');
                setPage(1);
              }}
              leftIcon={<RefreshCcw className="h-4 w-4" />}
            >
              Limpiar
            </Button>
          </div>
        </div>
      </Card>

      {doctorsQuery.isError ? (
        <Alert variant="error" title="Error al cargar médicos">
          {doctorsQuery.error instanceof ApiError ? doctorsQuery.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      <Card>
        <DataTable
          loading={doctorsQuery.isLoading}
          rows={doctorsQuery.data?.items ?? []}
          columns={[
            {
              key: 'fullName',
              header: 'Nombre',
              render: (row: Doctor) => `${row.firstName} ${row.lastName}`,
            },
            { key: 'licenseNumber', header: 'Licencia' },
            { key: 'licenseCountry', header: 'País' },
            {
              key: 'status',
              header: 'Estado',
              render: (row: Doctor) => (
                <Badge variant={row.status === 'Active' ? 'success' : row.status === 'Suspended' ? 'danger' : 'neutral'}>
                  {row.status}
                </Badge>
              ),
            },
            {
              key: 'specialties',
              header: 'Especialidades',
              render: (row: Doctor) => (
                <div className="flex flex-wrap gap-1">
                  {row.specialties.slice(0, 3).map((specialty) => (
                    <Badge key={specialty.specialtyId} variant="info">
                      {specialty.name}
                    </Badge>
                  ))}
                  {row.specialties.length > 3 ? <Badge>+{row.specialties.length - 3}</Badge> : null}
                </div>
              ),
            },
            {
              key: 'clinics',
              header: 'Clínicas',
              render: (row: Doctor) => (
                <div className="flex flex-wrap gap-1">
                  {row.clinics.slice(0, 3).map((clinic) => (
                    <Badge key={clinic.clinicId} variant={clinic.isPrimary ? 'success' : 'neutral'}>
                      {clinic.clinicName}
                    </Badge>
                  ))}
                  {row.clinics.length > 3 ? <Badge>+{row.clinics.length - 3}</Badge> : null}
                </div>
              ),
            },
            {
              key: 'actions',
              header: 'Acciones',
              render: (row: Doctor) =>
                canManage ? (
                  <Button variant="secondary" size="sm" onClick={() => openEdit(row)} leftIcon={<Pencil className="h-4 w-4" />}>
                    Editar
                  </Button>
                ) : null,
            },
          ]}
        />

        {doctorsQuery.data ? (
          <Pagination
            page={doctorsQuery.data.page}
            pageSize={doctorsQuery.data.pageSize}
            totalCount={doctorsQuery.data.totalCount}
            onPageChange={setPage}
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        ) : null}
      </Card>

      <Modal
        open={modalOpen}
        title={editingDoctor ? 'Editar médico' : 'Nuevo médico'}
        onClose={() => setModalOpen(false)}
        size="xl"
      >
        <form className="space-y-6" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Nombre" htmlFor="firstName" required error={form.formState.errors.firstName?.message}>
              <Input id="firstName" {...form.register('firstName')} invalid={Boolean(form.formState.errors.firstName)} />
            </FormField>

            <FormField label="Apellido" htmlFor="lastName" required error={form.formState.errors.lastName?.message}>
              <Input id="lastName" {...form.register('lastName')} invalid={Boolean(form.formState.errors.lastName)} />
            </FormField>

            <FormField label="Email" htmlFor="email" error={form.formState.errors.email?.message}>
              <Input id="email" type="email" {...form.register('email')} />
            </FormField>

            <FormField label="Teléfono" htmlFor="phone" error={form.formState.errors.phone?.message}>
              <Input id="phone" {...form.register('phone')} />
            </FormField>

            <FormField label="Licencia" htmlFor="licenseNumber" required error={form.formState.errors.licenseNumber?.message}>
              <Input id="licenseNumber" {...form.register('licenseNumber')} />
            </FormField>

            <FormField label="País licencia" htmlFor="licenseCountry" required error={form.formState.errors.licenseCountry?.message}>
              <Input id="licenseCountry" maxLength={2} {...form.register('licenseCountry')} />
            </FormField>

            <FormField label="Género" htmlFor="gender" required>
              <Select id="gender" options={genderOptions} {...form.register('gender')} />
            </FormField>

            <FormField label="Estado" htmlFor="status" required>
              <Select id="status" options={statusOptions} {...form.register('status')} />
            </FormField>

            <FormField label="Fecha nacimiento" htmlFor="dateOfBirth">
              <Input id="dateOfBirth" type="date" {...form.register('dateOfBirth')} />
            </FormField>

            <FormField label="Foto URL" htmlFor="photoUrl">
              <Input id="photoUrl" {...form.register('photoUrl')} />
            </FormField>
          </div>

          <FormField label="Biografía" htmlFor="biography">
            <Textarea id="biography" {...form.register('biography')} maxLength={4000} />
          </FormField>

          <div className="grid gap-4 lg:grid-cols-2">
            <FormField label="Especialidades" htmlFor="specialties">
              <select
                id="specialties"
                multiple
                value={selectedSpecialtyIds}
                onChange={(event) =>
                  setSelectedSpecialtyIds(Array.from(event.target.selectedOptions).map((option) => option.value))
                }
                className="h-40 w-full rounded-lg border border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-900"
              >
                {specialtyOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField label="Clínicas" htmlFor="clinics">
              <select
                id="clinics"
                multiple
                value={selectedClinicIds}
                onChange={(event) => {
                  const ids = Array.from(event.target.selectedOptions).map((option) => option.value);
                  setSelectedClinicIds(ids);

                  if (primaryClinicId && !ids.includes(primaryClinicId)) {
                    setPrimaryClinicId('');
                  }
                }}
                className="h-40 w-full rounded-lg border border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-900"
              >
                {clinicOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </FormField>
          </div>

          <FormField label="Clínica principal" htmlFor="primaryClinic">
            <Select
              id="primaryClinic"
              value={primaryClinicId}
              onChange={(event) => setPrimaryClinicId(event.target.value)}
              options={[
                { value: '', label: 'Ninguna' },
                ...clinicOptions.filter((option) => selectedClinicIds.includes(option.value)),
              ]}
            />
          </FormField>

          <FormField label="Idiomas" htmlFor="languages">
            <TagsInput value={languages} onChange={setLanguages} placeholder="ES, EN, FR..." />
          </FormField>

          {saveMutation.isError ? (
            <Alert variant="error" title="No se pudo guardar el médico">
              {saveMutation.error instanceof ApiError ? saveMutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setModalOpen(false)}>
              Cancelar
            </Button>
            <Button type="submit" loading={saveMutation.isPending} leftIcon={<Save className="h-4 w-4" />}>
              Guardar
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}