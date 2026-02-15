import type { ReactNode } from 'react';
import { Navigate, Outlet } from 'react-router-dom';

interface ProtectedRouteProps {
  children?: ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps): JSX.Element {
  const isAuthenticated = true;

  if (!isAuthenticated) {
    return <Navigate replace to="/login" />;
  }

  return children ? <>{children}</> : <Outlet />;
}
