import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';

import { UserForm } from '@/features/admin/components/UserForm';

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('UserForm', () => {
  it('renders form fields', () => {
    renderWithProviders(<UserForm open={true} onOpenChange={() => {}} />);

    expect(screen.getByLabelText(/Nome/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/E-mail/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Senha Temporária/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Papel/i)).toBeInTheDocument();
  });

  it('validates required fields', async () => {
    const user = userEvent.setup();
    renderWithProviders(<UserForm open={true} onOpenChange={() => {}} />);

    const submitButton = screen.getByRole('button', { name: /Criar/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Nome deve ter no mínimo 2 caracteres/i)).toBeInTheDocument();
    });
  });

  it('validates email format', async () => {
    const user = userEvent.setup();
    renderWithProviders(<UserForm open={true} onOpenChange={() => {}} />);

    // Fill required fields with valid data except email
    await user.type(screen.getByLabelText(/Nome/i), 'Test User');
    await user.type(screen.getByLabelText(/E-mail/i), 'invalid-email');
    await user.type(screen.getByLabelText(/Senha Temporária/i), 'ValidPass123');

    const submitButton = screen.getByRole('button', { name: /Criar/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/E-mail inválido/i)).toBeInTheDocument();
    }, { timeout: 10000 });
  }, 15000);

  it('validates password strength requirements', async () => {
    const user = userEvent.setup();
    renderWithProviders(<UserForm open={true} onOpenChange={() => {}} />);

    // Fill required fields with valid data except password
    await user.type(screen.getByLabelText(/Nome/i), 'Test User');
    await user.type(screen.getByLabelText(/E-mail/i), 'test@example.com');
    await user.type(screen.getByLabelText(/Senha Temporária/i), 'weak');

    const submitButton = screen.getByRole('button', { name: /Criar/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/Senha deve ter no mínimo 8 caracteres/i)).toBeInTheDocument();
    });
  });

  it('shows password strength indicator', async () => {
    const user = userEvent.setup();
    renderWithProviders(<UserForm open={true} onOpenChange={() => {}} />);

    const passwordInput = screen.getByLabelText(/Senha Temporária/i);
    await user.type(passwordInput, 'StrongPass123');

    await waitFor(() => {
      expect(screen.getByText(/Força da senha/i)).toBeInTheDocument();
    });
  });

  it('submits form with valid data', async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    renderWithProviders(<UserForm open={true} onOpenChange={onOpenChange} />);

    await user.type(screen.getByLabelText(/Nome/i), 'Test User');
    await user.type(screen.getByLabelText(/E-mail/i), 'test@example.com');
    await user.type(screen.getByLabelText(/Senha Temporária/i), 'ValidPass123');

    const submitButton = screen.getByRole('button', { name: /Criar/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });

  it('clears previously typed values after closing and reopening', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });

    const { rerender } = render(
      <QueryClientProvider client={queryClient}>
        <UserForm open={true} onOpenChange={() => {}} />
      </QueryClientProvider>
    );

    await user.type(screen.getByLabelText(/Nome/i), 'Usuário Temporário');
    await user.type(screen.getByLabelText(/E-mail/i), 'temp@example.com');

    expect((screen.getByLabelText(/Nome/i) as HTMLInputElement).value).toBe('Usuário Temporário');

    rerender(
      <QueryClientProvider client={queryClient}>
        <UserForm open={false} onOpenChange={() => {}} />
      </QueryClientProvider>
    );

    rerender(
      <QueryClientProvider client={queryClient}>
        <UserForm open={true} onOpenChange={() => {}} />
      </QueryClientProvider>
    );

    expect((screen.getByLabelText(/Nome/i) as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText(/E-mail/i) as HTMLInputElement).value).toBe('');
  });
});
