export interface NavItem {
  label: string;
  path: string;
  icon: string;
  title: string;
}

export const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', title: 'Visão Geral' },
  { label: 'Transações', path: '/transactions', icon: 'receipt_long', title: 'Transações' },
  { label: 'Contas', path: '/accounts', icon: 'account_balance', title: 'Minhas Contas' },
  { label: 'Categorias', path: '/categories', icon: 'category', title: 'Categorias' },
  { label: 'Admin', path: '/admin', icon: 'admin_panel_settings', title: 'Painel Admin' },
];

export const STATUS_COLORS = {
  paid: 'bg-green-100 text-green-800',
  pending: 'bg-yellow-100 text-yellow-800',
  cancelled: 'bg-slate-100 text-slate-700 line-through',
} as const;

export const ACCOUNT_TYPE_LABELS: Record<number, string> = {
  1: 'Corrente',
  2: 'Cartao',
  3: 'Investimento',
  4: 'Carteira',
};

export const ACCOUNT_TYPE_ICONS: Record<number, string> = {
  1: 'account_balance',
  2: 'credit_card',
  3: 'trending_up',
  4: 'wallet',
};

export const TRANSACTION_STATUS_LABELS: Record<number, string> = {
  1: 'Pago',
  2: 'Pendente',
  3: 'Cancelado',
};
