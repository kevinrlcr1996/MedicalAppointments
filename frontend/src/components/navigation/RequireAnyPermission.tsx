import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { hasAnyPermission, type Permission } from '../../lib/permissions';

interface RequireAnyPermissionProps {
  permissions: Permission[];
}

export function RequireAnyPermission({ permissions }: RequireAnyPermissionProps) {
  const { user, status } = useAuth();

  if (status === 'loading') {
    return null;
  }

  if (!hasAnyPermission(user, permissions)) {
    return <Navigate to="/forbidden" replace />;
  }

  return <Outlet />;
}