import { http, HttpResponse } from 'msw';

import type { AccountResponse, CreateAccountRequest, UpdateAccountRequest } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';

const mockAccounts: AccountResponse[] = [
  {
    id: '1',
    name: 'Banco Itaú',
    type: AccountType.Corrente,
    balance: 5230.45,
    allowNegativeBalance: false,
    isActive: true,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
    creditCard: null,
  },
  {
    id: '2',
    name: 'Nubank',
    type: AccountType.Cartao,
    balance: -850.00,
    allowNegativeBalance: true,
    isActive: true,
    createdAt: '2026-01-16T10:00:00Z',
    updatedAt: null,
    creditCard: {
      creditLimit: 5000,
      closingDay: 10,
      dueDay: 20,
      debitAccountId: '1',
      enforceCreditLimit: true,
      availableLimit: 4150,
    },
  },
  {
    id: '3',
    name: 'Carteira',
    type: AccountType.Carteira,
    balance: 320.00,
    allowNegativeBalance: false,
    isActive: true,
    createdAt: '2026-01-17T10:00:00Z',
    updatedAt: null,
    creditCard: null,
  },
  {
    id: '4',
    name: 'XP Investimentos',
    type: AccountType.Investimento,
    balance: 12500.00,
    allowNegativeBalance: false,
    isActive: true,
    createdAt: '2026-01-18T10:00:00Z',
    updatedAt: null,
    creditCard: null,
  },
  {
    id: '5',
    name: 'Conta Poupança',
    type: AccountType.Corrente,
    balance: 1500.00,
    allowNegativeBalance: false,
    isActive: false,
    createdAt: '2026-01-19T10:00:00Z',
    updatedAt: '2026-01-20T10:00:00Z',
    creditCard: null,
  },
];

const BASE_URL = '*';

export const accountsHandlers = [
  // GET /api/v1/accounts
  http.get(`${BASE_URL}/api/v1/accounts`, () => {
    return HttpResponse.json(mockAccounts);
  }),

  // GET /api/v1/accounts/:id
  http.get(`${BASE_URL}/api/v1/accounts/:id`, ({ params }) => {
    const { id } = params;
    const account = mockAccounts.find((acc) => acc.id === id);

    if (!account) {
      return new HttpResponse(null, { status: 404 });
    }

    return HttpResponse.json(account);
  }),

  // POST /api/v1/accounts
  http.post(`${BASE_URL}/api/v1/accounts`, async ({ request }) => {
    const body = (await request.json()) as CreateAccountRequest;

    const newAccount: AccountResponse = {
      id: String(mockAccounts.length + 1),
      name: body.name,
      type: body.type,
      balance: body.initialBalance ?? 0,
      allowNegativeBalance: body.allowNegativeBalance ?? false,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: null,
      creditCard: body.type === AccountType.Cartao && body.creditLimit && body.closingDay && body.dueDay && body.debitAccountId
        ? {
            creditLimit: body.creditLimit,
            closingDay: body.closingDay,
            dueDay: body.dueDay,
            debitAccountId: body.debitAccountId,
            enforceCreditLimit: body.enforceCreditLimit ?? true,
            availableLimit: body.creditLimit,
          }
        : null,
    };

    mockAccounts.push(newAccount);
    return HttpResponse.json(newAccount, { status: 201 });
  }),

  // PUT /api/v1/accounts/:id
  http.put(`${BASE_URL}/api/v1/accounts/:id`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as UpdateAccountRequest;
    const accountIndex = mockAccounts.findIndex((acc) => acc.id === id);

    if (accountIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    const currentAccount = mockAccounts[accountIndex];

    mockAccounts[accountIndex] = {
      ...currentAccount,
      name: body.name,
      allowNegativeBalance: body.allowNegativeBalance ?? currentAccount.allowNegativeBalance,
      updatedAt: new Date().toISOString(),
      creditCard: currentAccount.type === AccountType.Cartao && body.creditLimit
        ? {
            creditLimit: body.creditLimit,
            closingDay: body.closingDay ?? currentAccount.creditCard?.closingDay ?? 1,
            dueDay: body.dueDay ?? currentAccount.creditCard?.dueDay ?? 10,
            debitAccountId: body.debitAccountId ?? currentAccount.creditCard?.debitAccountId ?? '',
            enforceCreditLimit: body.enforceCreditLimit ?? currentAccount.creditCard?.enforceCreditLimit ?? true,
            availableLimit: body.creditLimit - Math.abs(currentAccount.balance),
          }
        : currentAccount.creditCard,
    };

    return HttpResponse.json(mockAccounts[accountIndex]);
  }),

  // PATCH /api/v1/accounts/:id/status
  http.patch(`${BASE_URL}/api/v1/accounts/:id/status`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as { isActive: boolean };
    const accountIndex = mockAccounts.findIndex((acc) => acc.id === id);

    if (accountIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    mockAccounts[accountIndex] = {
      ...mockAccounts[accountIndex],
      isActive: body.isActive,
      updatedAt: new Date().toISOString(),
    };

    return new HttpResponse(null, { status: 204 });
  }),
];
