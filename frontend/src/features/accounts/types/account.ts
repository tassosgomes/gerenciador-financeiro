export const AccountType = {
  Corrente: 1,
  Cartao: 2,
  Investimento: 3,
  Carteira: 4,
} as const;

export type AccountType = (typeof AccountType)[keyof typeof AccountType];

export interface AccountResponse {
  id: string;
  name: string;
  type: AccountType;
  balance: number;
  allowNegativeBalance: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateAccountRequest {
  name: string;
  type: AccountType;
  initialBalance: number;
  allowNegativeBalance: boolean;
  operationId?: string;
}

export interface UpdateAccountRequest {
  name: string;
  allowNegativeBalance: boolean;
}
