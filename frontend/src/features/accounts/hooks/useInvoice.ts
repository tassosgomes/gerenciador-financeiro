import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { getInvoice, payInvoice } from '@/features/accounts/api/accountsApi';
import type { PayInvoiceRequest } from '@/features/accounts/types/invoice';

export function useInvoice(accountId: string, month: number, year: number) {
  return useQuery({
    queryKey: ['invoice', accountId, month, year],
    queryFn: () => getInvoice(accountId, month, year),
    enabled: !!accountId && !!month && !!year,
  });
}

export function usePayInvoice() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      accountId,
      request,
    }: {
      accountId: string;
      request: PayInvoiceRequest;
    }) => payInvoice(accountId, request),
    onSuccess: (_, { accountId }) => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
      queryClient.invalidateQueries({ queryKey: ['invoice', accountId] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      toast.success('Fatura paga com sucesso');
    },
    onError: () => {
      toast.error('Erro ao pagar fatura');
    },
  });
}
