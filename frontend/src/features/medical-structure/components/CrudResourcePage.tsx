import { useMemo, useState, type ReactNode } from 'react';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Pencil, Plus, Power, RefreshCcw } from 'lucide-react';
import { crudApi, type QueryParams } from '../../../services/api/crudApi';
import { ApiError, type ValidationError } from '../../../services/api/apiClient';
import { hasPermission, type Permission } from '../../../lib/permissions';
import { useAuth } from '../../../contexts/AuthContext';
import { Alert } from '../../../components/ui/Alert';
import { Badge } from '../../../components/ui/Badge';
import { Button } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { DataTable, type DataColumn } from '../../../components/ui/DataTable';
import { FormField } from '../../../components/ui/FormField';
import { Input } from '../../../components/ui/Input';
import { Modal } from '../../../components/ui/Modal';
import { PageHeader } from '../../../components/ui/PageHeader';
import { Pagination } from '../../../components/ui/Pagination';
import { Select, type SelectOption } from '../../../components/ui/Select';
import { Switch } from '../../../components/ui/Switch';
import { Textarea } from '../../../components/ui/Textarea';

export interface CrudField {
  name: string;
  label: string;
  type: 'text' | 'textarea' | 'number' | 'date' | 'select' | 'switch';
  required?: boolean;
  options?: SelectOption[];
  maxLength?: number;
  placeholder?: string;
  help?: string;
}

export interface CrudFilter {
  name: string;
  label: string;
  type: 'text' | 'select';
  options?: SelectOption[];
  placeholder?: string;
}

export interface CrudColumn {
  key: string;
  header: string;
  render?: (row: CrudRow) => ReactNode;
}

export interface CrudRow {
  id: string;
  isActive?: boolean;
  [key: string]: string | number | boolean | null | undefined | Record<string, unknown>;
}

interface CrudResourcePageProps {
  title: string;
  description?: string;
  endpoint: string;
  queryKey: string;
  readPermission: Permission;
  managePermission: Permission;
  columns: CrudColumn[];
  fields: CrudField[];
  filters?: CrudFilter[];
  canStatus?: boolean;
  defaultFormValues?: Record<string, string | number | boolean | null>;
}

type FormValues = Record<string, string | number | boolean | null>;

function buildSchema(fields: CrudField[]) {
  const shape: Record<string, z.ZodTypeAny> = {};

  for (const field of fields) {
    if (field.type === 'switch') {
      shape[field.name] = z.boolean();
      continue;
    }

    if (field.type === 'number') {
      shape[field.name] = z
        .union([z.literal(''), z.coerce.number().int().min(1)])
        .optional()
        .or(z.literal(null));
      continue;
    }

    if (field.type === 'date') {
      shape[field.name] = z
        .union([z.literal(''), z.string().regex(/^\d{4}-\d{2}-\d{2}$/)])
        .optional()
        .or(z.literal(null));
      continue;
    }

    if (field.type === 'select') {
      shape[field.name] = field.required
        ? z.string().min(1, 'Campo obligatorio.')
        : z.string().optional().or(z.literal(''));
      continue;
    }

    if (field.name.toLowerCase().includes('email')) {
      const emailSchema = z.string().trim().email('Introduce un email válido.');
      shape[field.name] = field.required
        ? emailSchema.min(1, 'Campo obligatorio.').max(field.maxLength ?? 320)
        : emailSchema.max(field.maxLength ?? 320).optional().or(z.literal(''));
      continue;
    }

    if (field.type === 'textarea') {
      shape[field.name] = field.required
        ? z.string().trim().min(1, 'Campo obligatorio.').max(field.maxLength ?? 2000)
        : z.string().trim().max(field.maxLength ?? 2000).optional().or(z.literal(''));
      continue;
    }

    shape[field.name] = field.required
      ? z.string().trim().min(1, 'Campo obligatorio.').max(field.maxLength ?? 200)
      : z.string().trim().max(field.maxLength ?? 200).optional().or(z.literal(''));
  }

  return z.object(shape);
}

function toFormDefaults(fields: CrudField[], row?: CrudRow): FormValues {
  const values: FormValues = {};

  for (const field of fields) {
    const raw = row?.[field.name];

    if (field.type === 'switch') {
      values[field.name] = Boolean(raw ?? true);
      continue;
    }

    if (raw === null || raw === undefined) {
      values[field.name] = '';
      continue;
    }

    values[field.name] = raw as string | number | boolean;
  }

  return values;
}

function toPayload(fields: CrudField[], values: FormValues) {
  const payload: Record<string, unknown> = {};

  for (const field of fields) {
    const value = values[field.name];

    if (field.type === 'number' || field.type === 'date') {
      payload[field.name] = value === '' ? null : value;
      continue;
    }

    if (field.type === 'select') {
      payload[field.name] = value === '' ? null : value;
      continue;
    }

    payload[field.name] = value;
  }

  return payload;
}

export function CrudResourcePage({
  title,
  description,
  endpoint,
  queryKey,
  readPermission,
  managePermission,
  columns,
  fields,
  filters = [],
  canStatus = false,
  defaultFormValues = {},
}: CrudResourcePageProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [filterValues, setFilterValues] = useState<Record<string, string>>({});
  const [modalOpen, setModalOpen] = useState(false);
  const [editingRow, setEditingRow] = useState<CrudRow | null>(null);

  const canManage = hasPermission(user, managePermission);

  const queryParams: QueryParams = useMemo(
    () => ({
      page,
      pageSize,
      ...filterValues,
    }),
    [page, pageSize, filterValues],
  );

  const listQuery = useQuery({
    queryKey: [queryKey, queryParams],
    queryFn: () => crudApi.list<CrudRow>(endpoint, queryParams),
    enabled: hasPermission(user, readPermission),
  });

  const schema = useMemo(() => buildSchema(fields), [fields]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema) as Resolver<FormValues>,
    defaultValues: toFormDefaults(fields, undefined),
  });

  const closeModal = () => {
    setModalOpen(false);
    setEditingRow(null);
    form.reset(toFormDefaults(fields, undefined));
  };

  const openCreate = () => {
    setEditingRow(null);
    form.reset({ ...toFormDefaults(fields, undefined), ...defaultFormValues });
    setModalOpen(true);
  };

  const openEdit = (row: CrudRow) => {
    setEditingRow(row);
    form.reset(toFormDefaults(fields, row));
    setModalOpen(true);
  };

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const payload = toPayload(fields, values);

      if (editingRow) {
        return crudApi.update(endpoint, editingRow.id, payload);
      }

      return crudApi.create(endpoint, payload);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [queryKey] });
      closeModal();
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
        const field = fields.find(
          (candidate) => candidate.name.toLowerCase() === validationError.propertyName.toLowerCase(),
        );

        if (field) {
          form.setError(field.name, {
            type: 'server',
            message: validationError.errorMessage,
          });
        }
      }
    },
  });

  const statusMutation = useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) =>
      crudApi.setStatus(endpoint, id, isActive),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [queryKey] });
    },
  });

  const tableColumns: DataColumn<CrudRow>[] = [
    ...columns.map((column) => ({
      key: column.key,
      header: column.header,
      render: column.render,
    })),
    {
      key: 'isActive',
      header: 'Estado',
      render: (row) =>
        row.isActive === undefined ? (
          <Badge variant="neutral">—</Badge>
        ) : row.isActive ? (
          <Badge variant="success">Activo</Badge>
        ) : (
          <Badge variant="danger">Inactivo</Badge>
        ),
    },
    {
      key: 'actions',
      header: 'Acciones',
      render: (row) =>
        canManage ? (
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" size="sm" onClick={() => openEdit(row)} leftIcon={<Pencil className="h-4 w-4" />}>
              Editar
            </Button>

            {canStatus && row.isActive !== undefined ? (
              <Button
                variant={row.isActive ? 'danger' : 'primary'}
                size="sm"
                onClick={() =>
                  statusMutation.mutate({ id: row.id, isActive: !row.isActive })
                }
                leftIcon={<Power className="h-4 w-4" />}
              >
                {row.isActive ? 'Desactivar' : 'Activar'}
              </Button>
            ) : null}
          </div>
        ) : null,
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title={title}
        description={description}
        actions={
          canManage ? (
            <Button onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              Nuevo
            </Button>
          ) : null
        }
      />

      {listQuery.isError ? (
        <Alert variant="error" title="Error al cargar registros">
          {listQuery.error instanceof ApiError ? listQuery.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {filters.length > 0 ? (
        <Card>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {filters.map((filter) => (
              <FormField key={filter.name} label={filter.label} htmlFor={`filter-${filter.name}`}>
                {filter.type === 'select' ? (
                  <Select
                    id={`filter-${filter.name}`}
                    value={filterValues[filter.name] ?? ''}
                    onChange={(event) => {
                      setPage(1);
                      setFilterValues((previous) => ({
                        ...previous,
                        [filter.name]: event.target.value,
                      }));
                    }}
                    options={filter.options ?? []}
                    placeholder={filter.placeholder ?? 'Todos'}
                  />
                ) : (
                  <Input
                    id={`filter-${filter.name}`}
                    value={filterValues[filter.name] ?? ''}
                    onChange={(event) => {
                      setPage(1);
                      setFilterValues((previous) => ({
                        ...previous,
                        [filter.name]: event.target.value,
                      }));
                    }}
                    placeholder={filter.placeholder ?? 'Buscar...'}
                  />
                )}
              </FormField>
            ))}

            <div className="flex items-end">
              <Button
                variant="ghost"
                onClick={() => {
                  setFilterValues({});
                  setPage(1);
                }}
                leftIcon={<RefreshCcw className="h-4 w-4" />}
              >
                Limpiar
              </Button>
            </div>
          </div>
        </Card>
      ) : null}

      <Card>
        <DataTable
          columns={tableColumns}
          rows={listQuery.data?.items ?? []}
          loading={listQuery.isLoading}
          emptyTitle="Sin registros"
          emptyDescription="No hay elementos para mostrar."
        />

        {listQuery.data ? (
          <Pagination
            page={listQuery.data.page}
            pageSize={listQuery.data.pageSize}
            totalCount={listQuery.data.totalCount}
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
        title={editingRow ? `Editar ${title.toLowerCase()}` : `Nuevo ${title.toLowerCase()}`}
        onClose={closeModal}
        size="lg"
      >
        <form
          className="space-y-4"
          onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
        >
          <div className="grid gap-4 sm:grid-cols-2">
            {fields.map((field) => {
              const error = form.formState.errors[field.name]?.message as string | undefined;

              if (field.type === 'switch') {
                return (
                  <div key={field.name} className="flex items-center justify-between rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                    <span className="text-sm font-medium text-slate-800 dark:text-slate-200">{field.label}</span>
                    <Switch
                      checked={Boolean(form.watch(field.name))}
                      onChange={(checked) => form.setValue(field.name, checked)}
                      label={field.label}
                    />
                  </div>
                );
              }

              return (
                <FormField
                  key={field.name}
                  label={field.label}
                  htmlFor={field.name}
                  required={field.required}
                  error={error}
                  hint={field.help}
                >
                  {field.type === 'textarea' ? (
                    <Textarea
                      id={field.name}
                      {...form.register(field.name)}
                      invalid={Boolean(error)}
                      maxLength={field.maxLength}
                      placeholder={field.placeholder}
                    />
                  ) : field.type === 'select' ? (
                    <Select
                      id={field.name}
                      {...form.register(field.name)}
                      invalid={Boolean(error)}
                      options={field.options ?? []}
                      placeholder={field.placeholder ?? 'Selecciona...'}
                    />
                  ) : (
                    <Input
                      id={field.name}
                      type={field.type === 'date' ? 'date' : field.type === 'number' ? 'number' : 'text'}
                      {...form.register(field.name)}
                      invalid={Boolean(error)}
                      maxLength={field.maxLength}
                      placeholder={field.placeholder}
                    />
                  )}
                </FormField>
              );
            })}
          </div>

          {saveMutation.isError ? (
            <Alert variant="error" title="No se pudo guardar">
              {saveMutation.error instanceof ApiError ? saveMutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={closeModal}>
              Cancelar
            </Button>
            <Button type="submit" loading={saveMutation.isPending}>
              Guardar
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

export { SelectOption };
