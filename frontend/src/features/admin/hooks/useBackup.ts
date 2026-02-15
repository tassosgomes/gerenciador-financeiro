import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

import { exportBackup, importBackup } from '@/features/admin/api/backupApi';

export function useExportBackup() {
  return useMutation({
    mutationFn: exportBackup,
    onSuccess: () => {
      toast.success('Backup exportado com sucesso!');
    },
    onError: () => {
      toast.error('Erro ao exportar backup. Tente novamente.');
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
    onError: () => {
      toast.error('Erro ao importar backup. Tente novamente.');
    },
  });
}
