import { apiBaseUrl } from './config';
import { authService } from './authService';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]> | ValidationError[];
  correlationId?: string;
}

export interface ValidationError {
  propertyName: string;
  errorMessage: string;
}

export class ApiError extends Error {
  status: number;
  problem?: ProblemDetails;

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

interface RequestOptions extends RequestInit {
  skipAuthRefresh?: boolean;
}

const publicAuthPaths = [
  '/api/v1/auth/login',
  '/api/v1/auth/register',
  '/api/v1/auth/forgot-password',
  '/api/v1/auth/reset-password',
  '/api/v1/auth/confirm-email',
  '/api/v1/auth/refresh',
];

async function request<T>(
  path: string,
  init?: RequestOptions,
  retry = true,
): Promise<T> {
  const headers = new Headers(init?.headers);

  headers.set('Accept', 'application/json');
  headers.set('X-Correlation-Id', crypto.randomUUID());

  const token = authService.getAccessToken();

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers,
    credentials: 'include',
  });

  const text = await response.text();
  let data: unknown = null;

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = { title: 'Invalid JSON response.' } satisfies ProblemDetails;
    }
  }

  if (!response.ok) {
    const problem = data as ProblemDetails | null;

    const shouldAttemptRefresh =
      retry &&
      response.status === 401 &&
      !init?.skipAuthRefresh &&
      !publicAuthPaths.some((publicPath) => path.startsWith(publicPath));

    if (shouldAttemptRefresh) {
      const refreshed = await authService.refresh();

      if (refreshed) {
        return request<T>(path, init, false);
      }
    }

    if (response.status === 401) {
      authService.clear();
    }

    throw new ApiError(
      problem?.title ?? `Request failed with status ${response.status}.`,
      response.status,
      problem ?? undefined,
    );
  }

  return data as T;
}

export const apiClient = {
  get<T>(path: string, options?: RequestOptions): Promise<T> {
    return request<T>(path, { ...options, method: 'GET' });
  },

  post<T>(path: string, body: unknown, options?: RequestOptions): Promise<T> {
    return request<T>(path, {
      ...options,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(options?.headers as Record<string, string> | undefined),
      },
      body: JSON.stringify(body),
    });
  },

  put<T>(path: string, body: unknown, options?: RequestOptions): Promise<T> {
    return request<T>(path, {
      ...options,
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...(options?.headers as Record<string, string> | undefined),
      },
      body: JSON.stringify(body),
    });
  },

  patch<T>(path: string, body: unknown, options?: RequestOptions): Promise<T> {
    return request<T>(path, {
      ...options,
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        ...(options?.headers as Record<string, string> | undefined),
      },
      body: JSON.stringify(body),
    });
  },

  delete<T>(path: string, options?: RequestOptions): Promise<T> {
    return request<T>(path, { ...options, method: 'DELETE' });
  },
};