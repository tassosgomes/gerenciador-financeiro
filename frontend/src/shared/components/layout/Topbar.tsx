import { useAuthStore } from '@/features/auth/store/authStore';
import { NAV_ITEMS } from '@/shared/utils';
import { useLocation, useNavigate } from 'react-router-dom';

function getPageTitle(pathname: string): string {
  const matchedItem = NAV_ITEMS.find((item) => pathname.startsWith(item.path));

  if (matchedItem) {
    return matchedItem.title;
  }

  if (pathname === '/login') {
    return 'Login';
  }

  return 'GestorFinanceiro';
}

export function Topbar(): JSX.Element {
  const navigate = useNavigate();
  const location = useLocation();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const pageTitle = getPageTitle(location.pathname);

  const userName = user?.name ?? 'Usuario';
  const normalizedRole = user?.role.trim().toUpperCase();
  const isAdmin = normalizedRole === 'ADMIN';
  const userLabel = isAdmin ? 'Administrador' : 'Membro da familia';

  async function handleLogout(): Promise<void> {
    await logout();
    navigate('/login', { replace: true });
  }

  return (
    <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-surface-light px-6 lg:px-8">
      <h1 className="text-xl font-semibold text-slate-800">{pageTitle}</h1>

      <div className="flex items-center gap-6">
        <button
          aria-label="Notificacoes"
          className="relative p-2 text-slate-400 transition-colors hover:text-primary"
          type="button"
        >
          <span className="material-icons">notifications</span>
          <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-danger ring-2 ring-white" />
        </button>

        <div className="h-6 w-px bg-slate-200" />

        <div className="flex items-center gap-3">
          <div className="hidden text-right sm:block">
            <p className="text-sm font-medium text-slate-700">{userName}</p>
            <p className="text-xs text-slate-500">{userLabel}</p>
          </div>

          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary ring-2 ring-slate-100">
            {userName.charAt(0).toUpperCase()}
          </div>

          <button
            aria-label="Sair"
            className="ml-1 text-slate-400 transition-colors hover:text-danger"
            onClick={() => void handleLogout()}
            title="Sair"
            type="button"
          >
            <span className="material-icons text-xl">logout</span>
          </button>
        </div>
      </div>
    </header>
  );
}
