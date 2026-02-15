import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

import { AppShell } from './AppShell';

describe('AppShell', () => {
  it('renders sidebar, topbar and outlet content', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route element={<AppShell />} path="/">
            <Route element={<div>Dashboard Content</div>} path="dashboard" />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole('navigation', { name: /navegacao principal/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /visão geral/i })).toBeInTheDocument();
    expect(screen.getByRole('main')).toHaveTextContent('Dashboard Content');
  });
});
