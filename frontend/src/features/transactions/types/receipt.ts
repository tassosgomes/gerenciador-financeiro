import type { TransactionResponse } from '@/features/transactions/types/transaction';

export interface ReceiptItemResponse {
  id: string;
  description: string;
  productCode: string | null;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  totalPrice: number;
  itemOrder: number;
}

export interface EstablishmentResponse {
  id: string;
  name: string;
  cnpj: string;
  accessKey: string;
}

export interface ReceiptLookupResponse {
  accessKey: string;
  establishmentName: string;
  establishmentCnpj: string;
  issuedAt: string;
  totalAmount: number;
  discountAmount: number;
  paidAmount: number;
  items: ReceiptItemResponse[];
  alreadyImported: boolean;
}

export interface ImportReceiptResponse {
  transaction: TransactionResponse;
  establishment: EstablishmentResponse;
  items: ReceiptItemResponse[];
}

export interface TransactionReceiptResponse {
  establishment: EstablishmentResponse;
  items: ReceiptItemResponse[];
}

export interface LookupReceiptRequest {
  input: string;
}

export interface ImportReceiptRequest {
  accessKey: string;
  accountId: string;
  categoryId: string;
  description: string;
  competenceDate: string;
  operationId?: string;
}
