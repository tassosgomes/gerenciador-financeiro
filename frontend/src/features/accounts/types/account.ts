export const AccountType = {
  Corrente: 1,
  Cartao: 2,
  Investimento: 3,
  Carteira: 4,
} as const;

export type AccountType = (typeof AccountType)[keyof typeof AccountType];

export interface CreditCardDetailsResponse {
  creditLimit: number;
  closingDay: number;
  dueDay: number;
  debitAccountId: string;
  enforceCreditLimit: boolean;
  availableLimit: number;
}

export interface AccountResponse {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  allowNegativeBalance: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  creditCard: CreditCardDetailsResponse | null;
}

export interface CreateAccountRequest {
  name: string;
  type: AccountType;
  initialBalance?: number;
  allowNegativeBalance?: boolean;
  // Credit card fields
  creditLimit?: number;
  closingDay?: number;
  dueDay?: number;
  debitAccountId?: string;
  enforceCreditLimit?: boolean;
  operationId?: string;
}

export interface UpdateAccountRequest {
  name: string;
  allowNegativeBalance?: boolean;
  // Credit card fields
  creditLimit?: number;
  closingDay?: number;
  dueDay?: number;
  debitAccountId?: string;
  enforceCreditLimit?: boolean;
}
