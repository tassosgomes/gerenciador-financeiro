import axios from 'axios';

import { API_URL } from '@/shared/config/runtimeConfig';

export const apiClient = axios.create({
  baseURL: API_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
  },
});
