import { NavLink } from 'react-router-dom';

import { NAV_ITEMS, cn } from '@/shared/utils';

const primaryNavItems = NAV_ITEMS.filter((item) => item.path !== '/admin');
const adminNavItems = NAV_ITEMS.filter((item) => item.path === '/admin');

export function Sidebar(): JSX.Element {
  return (
    <aside className="hidden h-full w-64 flex-shrink-0 flex-col border-r border-slate-200 bg-surface-light shadow-sm md:flex">
      <div className="flex h-16 items-center border-b border-slate-200 px-6">
        <div className="flex items-center gap-3">
          <div className="rounded-lg bg-primary/10 p-2">
            <span aria-hidden="true" className="material-icons text-primary">
              account_balance_wallet
            </span>
          </div>
          <span className="text-lg font-bold tracking-tight">GestorFinanceiro</span>
        </div>
      </div>

      <nav aria-label="Navegacao principal" className="flex-1 space-y-1 overflow-y-auto px-4 py-6">
        {primaryNavItems.map((item) => (
          <NavLink
            key={item.path}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2.5 font-medium transition-colors',
                isActive
                  ? 'bg-primary/10 text-primary'
                  : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900',
              )
            }
            to={item.path}
          >
            <span aria-hidden="true" className="material-icons">
              {item.icon}
            </span>
            {item.label}
          </NavLink>
        ))}

        <div className="mt-4 border-t border-slate-200 pt-4">
          <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-slate-400">
            Configuracoes
          </p>

          {adminNavItems.map((item) => (
            <NavLink
              key={item.path}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 font-medium transition-colors',
                  isActive
                    ? 'bg-primary/10 text-primary'
                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900',
                )
              }
              to={item.path}
            >
              <span aria-hidden="true" className="material-icons">
                {item.icon}
              </span>
              {item.label}
            </NavLink>
          ))}
        </div>
      </nav>

      <div className="border-t border-slate-200 p-4">
        <div className="mb-3 flex items-center gap-3 rounded-xl bg-slate-50 p-3">
          <span className="h-2 w-2 animate-pulse rounded-full bg-green-500" />
          <span className="text-xs font-medium text-slate-500">Sistema Online</span>
        </div>

        <div className="flex items-center gap-3 px-1">
          <img
            alt="Carlos Silva"
            className="h-9 w-9 rounded-full ring-2 ring-slate-100"
            src="https://i.pravatar.cc/72?img=11"
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-slate-800">Carlos Silva</p>
            <p className="truncate text-xs text-slate-500">Plano Familiar</p>
          </div>
        </div>
      </div>
    </aside>
  );
}
