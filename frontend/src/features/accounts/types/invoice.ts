export interface InvoiceTransactionDto {
  id: string;
  description: string;
  amount: number;
  type: number;
  competenceDate: string;
  installmentNumber: number | null;
  totalInstallments: number | null;
}

export interface InvoiceResponse {
  accountId: string;
  accountName: string;
  month: number;
  year: number;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  totalAmount: number;
  previousBalance: number;
  amountDue: number;
  transactions: InvoiceTransactionDto[];
}

export interface PayInvoiceRequest {
  amount: number;
  competenceDate: string;
  operationId?: string;
}
