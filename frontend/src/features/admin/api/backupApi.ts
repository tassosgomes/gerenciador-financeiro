import { format } from 'date-fns';

import { apiClient } from '@/shared/services/apiClient';

export async function exportBackup(): Promise<void> {
  const response = await apiClient.get('/api/v1/backup/export', {
    responseType: 'blob',
  });

  const url = window.URL.createObjectURL(new Blob([response.data]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', `backup-${format(new Date(), 'yyyy-MM-dd')}.json`);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function importBackup(file: File): Promise<void> {
  const formData = new FormData();
  formData.append('file', file);

  await apiClient.post('/api/v1/backup/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 120000, // 2 minutos
  });
}
