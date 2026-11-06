
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import {
  authService,
  type AuthenticationResponse,
  type CurrentUser,
} from '../services/api/authService';

import { logout as logoutApi } from '../services/api/auth';

import {
  foundationSignalRClient,
  notificationSignalRClient,
} from '../services/realtime/signalrClient';

type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

interface AuthContextValue {
  status: AuthStatus;
  user: CurrentUser | null;
  signIn: (response: AuthenticationResponse) => void;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<CurrentUser | null>(null);

  // Initialize authentication and restore the session.
  useEffect(() => {
    let mounted = true;

    const unsubscribe = authService.subscribe(() => {
      if (!mounted) {
        return;
      }

      const nextUser = authService.getUser();

      if (authService.isAuthenticated() && nextUser) {
        setUser(nextUser);
        setStatus('authenticated');
      } else {
        setUser(null);
        setStatus('unauthenticated');
      }
    });

    void authService.initialize().catch((error) => {
      console.error('Authentication initialization failed.', error);

      if (mounted) {
        setUser(null);
        setStatus('unauthenticated');
      }
    });

    return () => {
      mounted = false;
      unsubscribe();
    };
  }, []);

  // Establish NotificationHub connection when authenticated.
  useEffect(() => {
    if (status !== 'authenticated') {
      return;
    }

    let active = true;

    void notificationSignalRClient.connect().catch((error) => {
      if (active) {
        console.error(
          'NotificationHub connection failed.',
          error,
        );
      }
    });

    return () => {
      active = false;
      void notificationSignalRClient.disconnect();
    };
  }, [status]);

  // Update authentication after successful login.
  const signIn = useCallback((response: AuthenticationResponse) => {
    authService.setAuthentication(response);
    setUser(response.user);
    setStatus('authenticated');
  }, []);

  // Clear authentication and disconnect realtime connections.
  const signOut = useCallback(async () => {
    try {
      await logoutApi();
    } catch {
      // Ignore logout API errors.
    } finally {
      authService.clear();

      await Promise.allSettled([
        foundationSignalRClient.disconnect(),
        notificationSignalRClient.disconnect(),
      ]);

      setUser(null);
      setStatus('unauthenticated');
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      signIn,
      signOut,
    }),
    [status, user, signIn, signOut],
  );

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider.');
  }

  return context;
}