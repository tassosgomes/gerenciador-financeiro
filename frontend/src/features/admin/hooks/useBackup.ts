import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

import { exportBackup, importBackup } from '@/features/admin/api/backupApi';
import { getErrorMessage } from '@/shared/utils/errorMessages';

export function useExportBackup() {
  return useMutation({
    mutationFn: exportBackup,
    onSuccess: () => {
      toast.success('Backup exportado com sucesso!');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}

export function useImportBackup() {
  return useMutation({
    mutationFn: (file: File) => importBackup(file),
    onSuccess: () => {
      toast.success('Backup importado com sucesso!');
      // Reload page to refresh all data
      setTimeout(() => {
        window.location.reload();
      }, 1500);
    },
    onError: (error) => {
      toast.error(getErrorMessage(error));
    },
  });
}
