import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, type Permission } from '../../lib/permissions';

interface RequirePermissionProps {
  permission: Permission;
}

export function RequirePermission({ permission }: RequirePermissionProps) {
  const { user, status } = useAuth();

  if (status === 'loading') {
    return null;
  }

  if (!hasPermission(user, permission)) {
    return <Navigate to="/forbidden" replace />;
  }

  return <Outlet />;
}