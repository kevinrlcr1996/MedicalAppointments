import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { UserPlus } from 'lucide-react';
import { patientsApi, type SelfPatientPayload } from '../../services/api/patients';
import { ApiError } from '../../services/api/apiClient';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { FormField } from '../../components/ui/FormField';
import { Input } from '../../components/ui/Input';
import { Select } from '../../components/ui/Select';
import { Textarea } from '../../components/ui/Textarea';

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

const onboardingSchema = z.object({
  firstName: z.string().trim().min(1, 'Nombre obligatorio.').max(200),
  lastName: z.string().trim().min(1, 'Apellido obligatorio.').max(200),
  dateOfBirth: z
    .union([z.literal(''), z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Formato yyyy-mm-dd.')])
    .optional(),
  gender: z.string().min(1, 'Género obligatorio.'),
  identificationNumber: z.string().trim().max(100).optional().or(z.literal('')),
  phone: z.string().trim().max(50).optional().or(z.literal('')),
  email: z.string().trim().email('Email inválido.').max(320).optional().or(z.literal('')),
  address: z.string().trim().max(500).optional().or(z.literal('')),
  emergencyContact: z.string().trim().max(200).optional().or(z.literal('')),
  emergencyPhone: z.string().trim().max(50).optional().or(z.literal('')),
  bloodType: z.string().min(1, 'Tipo de sangre obligatorio.'),
  insuranceProvider: z.string().trim().max(200).optional().or(z.literal('')),
  insuranceNumber: z.string().trim().max(100).optional().or(z.literal('')),
});

type OnboardingValues = z.infer<typeof onboardingSchema>;

interface PatientOnboardingFormProps {
  onDone: () => void;
}

export function PatientOnboardingForm({ onDone }: PatientOnboardingFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<OnboardingValues>({
    resolver: zodResolver(onboardingSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      dateOfBirth: '',
      gender: 'Unknown',
      identificationNumber: '',
      phone: '',
      email: '',
      address: '',
      emergencyContact: '',
      emergencyPhone: '',
      bloodType: 'Unknown',
      insuranceProvider: '',
      insuranceNumber: '',
    },
  });

  const mutation = useMutation({
    mutationFn: (values: OnboardingValues) => {
      const payload: SelfPatientPayload = {
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        dateOfBirth: values.dateOfBirth ? values.dateOfBirth : null,
        gender: values.gender,
        identificationNumber: values.identificationNumber || null,
        phone: values.phone || null,
        email: values.email || null,
        address: values.address || null,
        emergencyContact: values.emergencyContact || null,
        emergencyPhone: values.emergencyPhone || null,
        bloodType: values.bloodType,
        insuranceProvider: values.insuranceProvider || null,
        insuranceNumber: values.insuranceNumber || null,
      };

      return patientsApi.createSelf(payload);
    },
    onSuccess: () => {
      onDone();
    },
  });

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Completa tu ficha de paciente</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Para poder reservar citas necesitamos tus datos básicos. Solo tú y el personal autorizado podrán verlos.
          </p>
        </div>

        <form className="space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Nombre" htmlFor="firstName" required error={errors.firstName?.message}>
              <Input id="firstName" {...register('firstName')} invalid={Boolean(errors.firstName)} />
            </FormField>

            <FormField label="Apellido" htmlFor="lastName" required error={errors.lastName?.message}>
              <Input id="lastName" {...register('lastName')} invalid={Boolean(errors.lastName)} />
            </FormField>

            <FormField label="Fecha de nacimiento" htmlFor="dateOfBirth" error={errors.dateOfBirth?.message}>
              <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
            </FormField>

            <FormField label="Género" htmlFor="gender" required>
              <Select id="gender" options={genderOptions} {...register('gender')} />
            </FormField>

            <FormField label="Identificación" htmlFor="identificationNumber" error={errors.identificationNumber?.message}>
              <Input id="identificationNumber" {...register('identificationNumber')} />
            </FormField>

            <FormField label="Tipo de sangre" htmlFor="bloodType" required>
              <Select id="bloodType" options={bloodTypeOptions} {...register('bloodType')} />
            </FormField>

            <FormField label="Teléfono" htmlFor="phone" error={errors.phone?.message}>
              <Input id="phone" {...register('phone')} />
            </FormField>

            <FormField label="Email" htmlFor="email" error={errors.email?.message}>
              <Input id="email" type="email" {...register('email')} />
            </FormField>
          </div>

          <FormField label="Dirección" htmlFor="address" error={errors.address?.message}>
            <Textarea id="address" {...register('address')} maxLength={500} />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Contacto de emergencia" htmlFor="emergencyContact" error={errors.emergencyContact?.message}>
              <Input id="emergencyContact" {...register('emergencyContact')} />
            </FormField>

            <FormField label="Teléfono de emergencia" htmlFor="emergencyPhone" error={errors.emergencyPhone?.message}>
              <Input id="emergencyPhone" {...register('emergencyPhone')} />
            </FormField>

            <FormField label="Aseguradora" htmlFor="insuranceProvider" error={errors.insuranceProvider?.message}>
              <Input id="insuranceProvider" {...register('insuranceProvider')} />
            </FormField>

            <FormField label="Número de póliza" htmlFor="insuranceNumber" error={errors.insuranceNumber?.message}>
              <Input id="insuranceNumber" {...register('insuranceNumber')} />
            </FormField>
          </div>

          {mutation.isError ? (
            <Alert variant="error" title="No se pudo guardar la ficha">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <Button type="submit" loading={mutation.isPending} leftIcon={<UserPlus className="h-4 w-4" />}>
            Guardar ficha y continuar
          </Button>
        </form>
      </div>
    </Card>
  );
}