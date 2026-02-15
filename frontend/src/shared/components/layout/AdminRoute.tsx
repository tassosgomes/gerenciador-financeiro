import { Link, Outlet } from 'react-router-dom';

import { useAuthStore } from '@/features/auth/store/authStore';
import { Card } from '@/shared/components/ui/card';

export function AdminRoute() {
  const { user } = useAuthStore();

  if (!user || user.role !== 'Admin') {
    return (
      <div className="flex h-full items-center justify-center p-8">
        <Card className="max-w-md p-8 text-center">
          <div className="mb-4 flex justify-center">
            <span className="material-icons text-4xl text-danger">block</span>
          </div>
          <h2 className="mb-2 text-xl font-bold">Acesso Restrito</h2>
          <p className="mb-6 text-slate-500">
            Apenas administradores podem acessar esta área.
          </p>
          <Link
            to="/dashboard"
            className="inline-flex items-center gap-2 text-primary hover:underline"
          >
            <span className="material-icons text-sm">arrow_back</span>
            Voltar ao Dashboard
          </Link>
        </Card>
      </div>
    );
  }

  return <Outlet />;
}
