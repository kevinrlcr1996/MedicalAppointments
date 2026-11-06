export interface CurrentUser {
  id: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  isEmailConfirmed: boolean;
  roles: string[];
  permissions: string[];
}

export interface AuthenticationResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

type Listener = () => void;

class AuthService {
  private accessToken: string | null = null;
  private expiresAt: number | null = null;
  private user: CurrentUser | null = null;
  private refreshPromise: Promise<boolean> | null = null;
  private readonly listeners = new Set<Listener>();

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getUser(): CurrentUser | null {
    return this.user;
  }

  isAuthenticated(): boolean {
    return this.accessToken !== null && (this.expiresAt === null || this.expiresAt > Date.now() + 30_000);
  }

  setAuthentication(response: AuthenticationResponse): void {
    this.accessToken = response.accessToken;
    this.expiresAt = new Date(response.expiresAt).getTime();
    this.user = response.user;
    this.emit();
  }

  clear(): void {
    this.accessToken = null;
    this.expiresAt = null;
    this.user = null;
    this.emit();
  }

  subscribe(listener: Listener): () => void {
    listener();
    this.listeners.add(listener);

    return () => {
      this.listeners.delete(listener);
    };
  }

  async initialize(): Promise<boolean> {
    return this.refresh();
  }

  async refresh(): Promise<boolean> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = this.performRefresh().finally(() => {
      this.refreshPromise = null;
    });

    return this.refreshPromise;
  }

  async getValidAccessToken(): Promise<string> {
    if (!this.accessToken || this.expiresAt === null || this.expiresAt <= Date.now() + 30_000) {
      const refreshed = await this.refresh();

      if (!refreshed || !this.accessToken) {
        throw new Error('UNAUTHENTICATED');
      }
    }

    return this.accessToken;
  }

  private async performRefresh(): Promise<boolean> {
    try {
      const response = await fetch(`${apiBaseUrl()}/api/v1/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          Accept: 'application/json',
          'X-Correlation-Id': crypto.randomUUID(),
        },
      });

      if (!response.ok) {
        this.clear();
        return false;
      }

      const data = (await response.json()) as AuthenticationResponse;
      this.setAuthentication(data);

      return true;
    } catch {
      this.clear();
      return false;
    }
  }

  private emit(): void {
    for (const listener of this.listeners) {
      listener();
    }
  }
}

function apiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');
}

export const authService = new AuthService();