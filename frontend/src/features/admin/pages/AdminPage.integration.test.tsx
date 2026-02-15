import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';

import { AdminPage } from '@/features/admin/pages/AdminPage';

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('AdminPage Integration', () => {
  it('renders admin page with tabs', async () => {
    renderWithProviders(<AdminPage />);

    await waitFor(() => {
      expect(screen.getByText('Painel Administrativo')).toBeInTheDocument();
    });

    expect(screen.getByRole('tab', { name: /Usuários/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Backup/i })).toBeInTheDocument();
  });

  it('displays user list in users tab', async () => {
    renderWithProviders(<AdminPage />);

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument();
      expect(screen.getByText('João Silva')).toBeInTheDocument();
    });
  });

  it('opens user form when clicking new user button', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdminPage />);

    await waitFor(() => {
      expect(screen.getByText('Painel Administrativo')).toBeInTheDocument();
    });

    const newUserButton = screen.getByRole('button', { name: /Novo Usuário/i });
    await user.click(newUserButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByLabelText(/Nome/i)).toBeInTheDocument();
    });
  });

  it('switches to backup tab', async () => {
    const user = userEvent.setup();
    renderWithProviders(<AdminPage />);

    await waitFor(() => {
      expect(screen.getByText('Painel Administrativo')).toBeInTheDocument();
    });

    const backupTab = screen.getByRole('tab', { name: /Backup/i });
    await user.click(backupTab);

    await waitFor(() => {
      expect(screen.getByText('Backup & Restauração')).toBeInTheDocument();
      expect(screen.getByText('Exportar Backup')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Importar Backup/i })).toBeInTheDocument();
    });
  });
});
