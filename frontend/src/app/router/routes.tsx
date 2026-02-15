import { lazy, Suspense } from 'react';
import {
  createBrowserRouter,
  Navigate,
  type RouteObject,
} from 'react-router-dom';

import { AppShell, ProtectedRoute } from '@/shared/components/layout';
import { Skeleton } from '@/shared/components/ui';

const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/pages/DashboardPage'));
const TransactionsPage = lazy(() => import('@/features/transactions/pages/TransactionsPage'));
const TransactionDetailPage = lazy(() => import('@/features/transactions').then(m => ({ default: m.TransactionDetailPage })));
const AccountsPage = lazy(() => import('@/features/accounts/pages/AccountsPage'));
const CategoriesPage = lazy(() => import('@/features/categories/pages/CategoriesPage'));
const AdminPage = lazy(() => import('@/features/admin/pages/AdminPage'));

const routeFallback = (
  <div className="space-y-4">
    <Skeleton className="h-8 w-1/3" />
    <Skeleton className="h-24 w-full" />
    <Skeleton className="h-24 w-full" />
  </div>
);

function withSuspense(page: JSX.Element): JSX.Element {
  return <Suspense fallback={routeFallback}>{page}</Suspense>;
}

export const routes: RouteObject[] = [
  {
    path: '/login',
    element: withSuspense(<LoginPage />),
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AppShell />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: withSuspense(<DashboardPage />) },
      { path: 'transactions', element: withSuspense(<TransactionsPage />) },
      { path: 'transactions/:id', element: withSuspense(<TransactionDetailPage />) },
      { path: 'accounts', element: withSuspense(<AccountsPage />) },
      { path: 'categories', element: withSuspense(<CategoriesPage />) },
      { path: 'admin', element: withSuspense(<AdminPage />) },
    ],
  },
  {
    path: '*',
    element: <Navigate to="/dashboard" replace />,
  },
];

export const router = createBrowserRouter(routes);
