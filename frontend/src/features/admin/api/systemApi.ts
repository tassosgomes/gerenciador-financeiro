import { apiClient } from '@/shared/services/apiClient';

export async function resetSystem(): Promise<void> {
  await apiClient.post('/api/v1/system/reset');
}
