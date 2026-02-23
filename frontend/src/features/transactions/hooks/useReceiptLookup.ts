import type { AxiosError } from 'axios';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

import { lookupReceipt } from '@/features/transactions/api/receiptApi';
import type {
  LookupReceiptRequest,
  ReceiptLookupResponse,
} from '@/features/transactions/types/receipt';
import { getErrorMessage } from '@/shared/utils/errorMessages';

function getLookupErrorMessage(error: unknown): string {
  if (error && typeof error === 'object' && 'isAxiosError' in error) {
    const axiosError = error as AxiosError;
    const status = axiosError.response?.status;

    if (status === 400) {
      return 'Chave de acesso inválida. Informe os 44 dígitos numéricos.';
    }

    if (status === 404) {
      return 'NFC-e não encontrada. Verifique se a nota está disponível na SEFAZ.';
    }

    if (status === 502) {
      return 'A SEFAZ está indisponível no momento. Tente novamente mais tarde.';
    }
  }

  return getErrorMessage(error);
}

export function useReceiptLookup() {
  return useMutation<ReceiptLookupResponse, Error, LookupReceiptRequest>({
    mutationFn: lookupReceipt,
    onError: (error) => {
      toast.error(getLookupErrorMessage(error));
    },
  });
}
