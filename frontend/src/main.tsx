import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { useAuthStore } from '@/features/auth/store/authStore';

import App from './App';
import { AppProviders } from './app/providers/AppProviders';
import './index.css';

useAuthStore.getState().hydrate();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </StrictMode>,
);
