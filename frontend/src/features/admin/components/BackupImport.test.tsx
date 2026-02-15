import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';

import { BackupImport } from '@/features/admin/components/BackupImport';

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('BackupImport', () => {
  it('renders upload area', () => {
    renderWithProviders(<BackupImport />);

    expect(screen.getByText(/Arraste o arquivo JSON aqui/i)).toBeInTheDocument();
    expect(screen.getByText(/Selecionar Arquivo/i)).toBeInTheDocument();
  });

  it('shows warning about data replacement', () => {
    renderWithProviders(<BackupImport />);

    expect(screen.getByText(/A importação substituirá TODOS os dados existentes/i)).toBeInTheDocument();
    expect(screen.getByText(/Esta ação é irreversível/i)).toBeInTheDocument();
  });

  it('disables import button when no file is selected', () => {
    renderWithProviders(<BackupImport />);

    const importButton = screen.getByRole('button', { name: /Importar Backup/i });
    expect(importButton).toBeDisabled();
  });
});
