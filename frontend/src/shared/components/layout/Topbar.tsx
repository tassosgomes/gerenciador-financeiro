import { useMemo, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';

import { useAuthStore } from '@/features/auth/store/authStore';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog';
import { cn, NAV_ITEMS } from '@/shared/utils';

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

  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const userName = user?.name ?? 'Usuario';
  const normalizedRole = user?.role.trim().toUpperCase();
  const isAdmin = normalizedRole === 'ADMIN';
  const userLabel = isAdmin ? 'Administrador' : 'Membro da familia';

  const primaryNavItems = useMemo(
    () => NAV_ITEMS.filter((item) => item.path !== '/admin'),
    [],
  );

  const adminNavItems = useMemo(
    () => NAV_ITEMS.filter((item) => item.path === '/admin'),
    [],
  );

  async function handleLogout(): Promise<void> {
    await logout();
    navigate('/login', { replace: true });
  }

  function handleOpenMobileMenu(): void {
    setIsMobileMenuOpen(true);
  }

  function handleCloseMobileMenu(): void {
    setIsMobileMenuOpen(false);
  }

  return (
    <>
      <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-surface-light px-6 lg:px-8">
        <div className="flex items-center gap-4">
          <button
            aria-label="Abrir menu de navegacao"
            className="flex h-11 w-11 items-center justify-center rounded-lg text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900 md:hidden"
            onClick={handleOpenMobileMenu}
            type="button"
          >
            <span className="material-icons text-2xl">menu</span>
          </button>

          <h1 className="text-xl font-semibold text-slate-800">{pageTitle}</h1>
        </div>

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

      <Dialog onOpenChange={setIsMobileMenuOpen} open={isMobileMenuOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <span aria-hidden="true" className="material-icons text-primary">
                  account_balance_wallet
                </span>
              </div>
              <span className="text-lg font-bold tracking-tight">GestorFinanceiro</span>
            </DialogTitle>
            <DialogDescription className="sr-only">
              Menu de navegacao principal do sistema
            </DialogDescription>
          </DialogHeader>

          <nav aria-label="Menu mobile" className="space-y-1 py-4">
            {primaryNavItems.map((item) => (
              <NavLink
                key={item.path}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-lg px-3 py-3 font-medium transition-colors',
                    isActive
                      ? 'bg-primary/10 text-primary'
                      : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900',
                  )
                }
                onClick={handleCloseMobileMenu}
                to={item.path}
              >
                <span aria-hidden="true" className="material-icons">
                  {item.icon}
                </span>
                {item.label}
              </NavLink>
            ))}

            {isAdmin ? (
              <div className="mt-4 border-t border-slate-200 pt-4">
                <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-slate-400">
                  Configuracoes
                </p>

                {adminNavItems.map((item) => (
                  <NavLink
                    key={item.path}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-3 rounded-lg px-3 py-3 font-medium transition-colors',
                        isActive
                          ? 'bg-primary/10 text-primary'
                          : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900',
                      )
                    }
                    onClick={handleCloseMobileMenu}
                    to={item.path}
                  >
                    <span aria-hidden="true" className="material-icons">
                      {item.icon}
                    </span>
                    {item.label}
                  </NavLink>
                ))}
              </div>
            ) : null}
          </nav>
        </DialogContent>
      </Dialog>
    </>
  );
}
