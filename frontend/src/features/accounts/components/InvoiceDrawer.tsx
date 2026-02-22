import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetDescription,
  SheetTitle,
  Button,
} from '@/shared/components/ui';
import { useInvoice } from '@/features/accounts/hooks/useInvoice';
import { useMonthNavigation } from '@/features/accounts/hooks/useMonthNavigation';
import { formatCurrency, formatDate } from '@/shared/utils/formatters';
import { cn } from '@/shared/utils';
import { PaymentDialog } from './PaymentDialog';

interface InvoiceDrawerProps {
  accountId: string;
  accountName: string;
  isOpen: boolean;
  onClose: () => void;
}

const TransactionType = {
  Debit: 1,
  Credit: 2,
  Transfer: 3,
  Adjust: 4,
} as const;

export function InvoiceDrawer({ accountId, accountName, isOpen, onClose }: InvoiceDrawerProps): JSX.Element {
  const { currentMonth, currentYear, monthLabel, handlePrevMonth, handleNextMonth } = useMonthNavigation();
  const [isPaymentDialogOpen, setIsPaymentDialogOpen] = useState(false);

  const { data: invoice, isLoading, error } = useInvoice(accountId, currentMonth, currentYear, isOpen);

  const handleOpenPayment = (): void => {
    setIsPaymentDialogOpen(true);
  };

  const handleClosePayment = (): void => {
    setIsPaymentDialogOpen(false);
  };

  return (
    <>
      <Sheet open={isOpen} onOpenChange={onClose}>
        <SheetContent side="right" className="w-full sm:max-w-md overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Fatura de {accountName}</SheetTitle>
            <SheetDescription>
              Detalhes da fatura de {monthLabel} {currentYear}
            </SheetDescription>
          </SheetHeader>

          {/* Navegação de mês */}
          <div className="flex items-center justify-between my-4 border-b pb-4">
            <Button variant="ghost" size="icon" onClick={handlePrevMonth} aria-label="Mês anterior">
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <span className="text-lg font-semibold">
              {monthLabel} {currentYear}
            </span>
            <Button variant="ghost" size="icon" onClick={handleNextMonth} aria-label="Próximo mês">
              <ChevronRight className="h-5 w-5" />
            </Button>
          </div>

          {/* Conteúdo do drawer */}
          {isLoading && (
            <div className="text-center py-8 text-slate-600">Carregando fatura...</div>
          )}

          {error && (
            <div className="text-center py-8 text-red-600">
              Erro ao carregar fatura. Tente novamente.
            </div>
          )}

          {invoice && !isLoading && !error && (
            <div className="space-y-6">
              {/* Resumo da Fatura */}
              <div className="bg-slate-50 rounded-lg p-4 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Período:</span>
                  <span className="font-medium">
                    {formatDate(invoice.periodStart)} — {formatDate(invoice.periodEnd)}
                  </span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Vencimento:</span>
                  <span className="font-medium">{formatDate(invoice.dueDate)}</span>
                </div>
                <div className="border-t pt-2 mt-2">
                  <div className="flex justify-between text-base">
                    <span className="text-slate-600">Total da Fatura:</span>
                    <span className="font-bold">{formatCurrency(invoice.totalAmount)}</span>
                  </div>
                  {invoice.previousBalance > 0 && (
                    <div className="flex justify-between text-sm mt-1">
                      <span className="text-green-700">Crédito anterior:</span>
                      <span className="font-medium text-green-700">
                        -{formatCurrency(invoice.previousBalance)}
                      </span>
                    </div>
                  )}
                  <div className="flex justify-between text-lg mt-2 border-t pt-2">
                    <span className="font-semibold text-slate-900">Valor a Pagar:</span>
                    <span className="font-bold text-slate-900">
                      {formatCurrency(invoice.amountDue)}
                    </span>
                  </div>
                </div>
              </div>

              {/* Lista de Transações */}
              <div>
                <h3 className="text-sm font-semibold text-slate-700 mb-3">Transações</h3>
                {invoice.transactions.length === 0 ? (
                  <div className="text-center py-8 text-slate-500 border rounded-lg">
                    Nenhuma transação neste período
                  </div>
                ) : (
                  <div className="space-y-2">
                    {invoice.transactions.map((transaction) => (
                      <div
                        key={transaction.id}
                        className="flex justify-between items-start border-b pb-2 last:border-b-0"
                      >
                        <div className="flex-1">
                          <div className="text-xs text-slate-500 mb-1">
                            {formatDate(transaction.competenceDate)}
                          </div>
                          <div className="text-sm">
                            {transaction.installmentNumber !== null && transaction.totalInstallments !== null
                              ? `Parcela ${transaction.installmentNumber}/${transaction.totalInstallments} — ${transaction.description}`
                              : transaction.description}
                          </div>
                        </div>
                        <div
                          className={cn(
                            'text-sm font-semibold ml-4',
                            transaction.type === TransactionType.Credit
                              ? 'text-green-700'
                              : 'text-slate-900'
                          )}
                        >
                          {transaction.type === TransactionType.Credit && '-'}
                          {formatCurrency(transaction.amount)}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Botão Pagar Fatura */}
              <div className="border-t pt-4">
                <Button
                  onClick={handleOpenPayment}
                  disabled={invoice.amountDue <= 0}
                  className="w-full"
                >
                  Pagar Fatura
                </Button>
                {invoice.amountDue <= 0 && (
                  <p className="text-xs text-slate-500 text-center mt-2">
                    Nada a pagar neste momento
                  </p>
                )}
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>

      {/* Dialog de Pagamento */}
      {invoice && isPaymentDialogOpen && (
        <PaymentDialog
          accountId={accountId}
          accountName={accountName}
          amountDue={invoice.amountDue}
          isOpen={isPaymentDialogOpen}
          onClose={handleClosePayment}
        />
      )}
    </>
  );
}
