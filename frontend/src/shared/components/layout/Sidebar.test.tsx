import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

import { Sidebar } from './Sidebar';

describe('Sidebar', () => {
  it('shows all expected navigation links', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Sidebar />
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /transacoes/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /contas/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /categorias/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /admin/i })).toBeInTheDocument();
  });

  it('marks current route as active', () => {
    render(
      <MemoryRouter initialEntries={['/transactions']}>
        <Sidebar />
      </MemoryRouter>,
    );

    const activeLink = screen.getByRole('link', { name: /transacoes/i });

    expect(activeLink).toHaveClass('bg-primary/10');
    expect(activeLink).toHaveClass('text-primary');
  });
});
