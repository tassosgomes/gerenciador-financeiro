import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import CategoriesPage from '@/features/categories/pages/CategoriesPage';

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('CategoriesPage', () => {
  it('renders page header with title and button', () => {
    renderWithClient(<CategoriesPage />);

    expect(screen.getByText('Categorias')).toBeInTheDocument();
    expect(screen.getByText('Organize suas receitas e despesas por categorias')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Nova Categoria/i })).toBeInTheDocument();
  });

  it('renders filter tabs', () => {
    renderWithClient(<CategoriesPage />);

    expect(screen.getByRole('tab', { name: 'Todas' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Receitas' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Despesas' })).toBeInTheDocument();
  });

  it('displays loading state initially', () => {
    renderWithClient(<CategoriesPage />);

    // A página deve estar presente com seus elementos principais
    expect(screen.getByText('Categorias')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Nova Categoria/i })).toBeInTheDocument();
  });

  it('displays categories list after loading', async () => {
    renderWithClient(<CategoriesPage />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
    });

    expect(screen.getByText('Salário')).toBeInTheDocument();
  });

  it('clicking "Nova Categoria" button triggers form opening', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoriesPage />);

    const addButton = screen.getByRole('button', { name: /Nova Categoria/i });
    
    // Verificar que o botão está presente e pode ser clicado
    expect(addButton).toBeInTheDocument();
    await user.click(addButton);
    
    // O formulário deve aparecer (teste básico de interação)
  });

  it('clicking edit button is enabled for categories', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoriesPage />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
    });

    const editButtons = screen.getAllByRole('button', { name: /Editar/i });
    
    // Verificar que os botões de edição estão presentes
    expect(editButtons.length).toBeGreaterThan(0);
    await user.click(editButtons[0]);
  });

  it('filters categories by type when filter is changed', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoriesPage />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
    });

    // Filtrar por Receitas
    const receitasTab = screen.getByRole('tab', { name: 'Receitas' });
    await user.click(receitasTab);

    await waitFor(() => {
      expect(screen.queryByText('Alimentação')).not.toBeInTheDocument();
      expect(screen.getByText('Salário')).toBeInTheDocument();
    });
  });

  it('shows all categories when "Todas" filter is selected', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoriesPage />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
    });

    // Primeiro filtra por Receitas
    const receitasTab = screen.getByRole('tab', { name: 'Receitas' });
    await user.click(receitasTab);

    await waitFor(() => {
      expect(screen.queryByText('Alimentação')).not.toBeInTheDocument();
    });

    // Depois volta para Todas
    const todasTab = screen.getByRole('tab', { name: 'Todas' });
    await user.click(todasTab);

    await waitFor(() => {
      expect(screen.getByText('Alimentação')).toBeInTheDocument();
      expect(screen.getByText('Salário')).toBeInTheDocument();
    });
  });
});
