import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

import { resetSystem } from '@/features/admin/api/systemApi';
import { getErrorMessage } from '@/shared/utils/errorMessages';

export function useResetSystem() {
  return useMutation({
    mutationFn: resetSystem,
    onSuccess: () => {
      toast.success('Sistema resetado com sucesso!');
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
