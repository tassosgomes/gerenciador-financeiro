import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';

import { LoginForm } from '@/features/auth/components/LoginForm';
import { useAuthStore } from '@/features/auth/store/authStore';

export default function LoginPage(): JSX.Element {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const sessionExpired = searchParams.get('session') === 'expired';

  if (isAuthenticated) {
    return <Navigate replace to="/dashboard" />;
  }

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-background-light px-4 py-8">
      <div className="pointer-events-none absolute -left-[12%] -top-[8%] h-[40%] w-[40%] rounded-full bg-primary/5 blur-[120px]" />
      <div className="pointer-events-none absolute -bottom-[12%] -right-[10%] h-[36%] w-[36%] rounded-full bg-primary/10 blur-[100px]" />

      <section className="relative w-full max-w-md overflow-hidden rounded-xl border border-slate-100 bg-white shadow-lg">
        <header className="px-8 pb-6 pt-10 text-center">
          <div className="mb-6 inline-flex h-16 w-16 items-center justify-center rounded-full bg-primary/10">
            <span className="material-icons text-3xl text-primary">account_balance_wallet</span>
          </div>

          <p className="text-lg font-bold tracking-tight text-slate-900">GestorFinanceiro</p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">Bem-vindo de volta</h1>
          <p className="mt-2 text-sm text-slate-500">Acesse sua conta para gerenciar suas finanças.</p>
        </header>

        <div className="px-8 pb-8">
          {sessionExpired ? (
            <p className="mb-4 rounded-lg border border-warning/30 bg-warning/10 px-3 py-2 text-sm text-slate-700">
              Sua sessão expirou. Faça login novamente.
            </p>
          ) : null}

          <LoginForm onSuccess={() => navigate('/dashboard', { replace: true })} />
        </div>

        <footer className="border-t border-slate-100 bg-slate-50 px-8 py-4">
          <div className="flex items-center justify-center gap-2 text-slate-500">
            <span className="material-icons text-sm">lock</span>
            <p className="text-center text-xs font-medium">Ambiente seguro e criptografado.</p>
          </div>
        </footer>
      </section>
    </main>
  );
}
