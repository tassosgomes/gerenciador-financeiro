import type { AxiosError } from 'axios';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

import { importReceipt } from '@/features/transactions/api/receiptApi';
import type {
  ImportReceiptRequest,
  ImportReceiptResponse,
} from '@/features/transactions/types/receipt';
import { getErrorMessage } from '@/shared/utils/errorMessages';

function getImportErrorMessage(error: unknown): string {
  if (error && typeof error === 'object' && 'isAxiosError' in error) {
    const axiosError = error as AxiosError;
    const status = axiosError.response?.status;

    if (status === 400) {
      return 'Não foi possível importar o cupom. Revise os dados informados.';
    }

    if (status === 404) {
      return 'NFC-e não encontrada. Verifique se a nota está disponível na SEFAZ.';
    }

    if (status === 409) {
      return 'Este cupom fiscal já foi importado anteriormente.';
    }

    if (status === 502) {
      return 'A SEFAZ está indisponível no momento. Tente novamente mais tarde.';
    }
  }

  return getErrorMessage(error);
}

export function useReceiptImport() {
  const queryClient = useQueryClient();

  return useMutation<ImportReceiptResponse, Error, ImportReceiptRequest>({
    mutationFn: importReceipt,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Cupom fiscal importado com sucesso!');
    },
    onError: (error) => {
      toast.error(getImportErrorMessage(error));
    },
  });
}
