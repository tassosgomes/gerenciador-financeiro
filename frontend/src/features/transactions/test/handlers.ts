import { http, HttpResponse } from 'msw';
import type {
  TransactionResponse,
  CreateTransactionRequest,
  CreateInstallmentRequest,
  CreateRecurrenceRequest,
  CreateTransferRequest,
  AdjustTransactionRequest,
  CancelTransactionRequest,
  TransactionHistoryEntry,
  PagedResponse,
} from '@/features/transactions/types/transaction';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

const mockTransactions: TransactionResponse[] = [
  {
    id: '1',
    accountId: '1',
    categoryId: '1',
    type: TransactionType.Debit,
    amount: 350.00,
    description: 'Supermercado Pão de Açúcar',
    competenceDate: '2026-02-10',
    dueDate: '2026-02-10',
    status: TransactionStatus.Paid,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    hasReceipt: true,
    createdAt: '2026-02-10T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    accountId: '2',
    categoryId: '2',
    type: TransactionType.Credit,
    amount: 5000.00,
    description: 'Salário',
    competenceDate: '2026-02-01',
    dueDate: null,
    status: TransactionStatus.Paid,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    hasReceipt: false,
    createdAt: '2026-02-01T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    accountId: '2',
    categoryId: '3',
    type: TransactionType.Debit,
    amount: 3000.00,
    description: 'Notebook Dell',
    competenceDate: '2026-02-05',
    dueDate: '2026-03-01',
    status: TransactionStatus.Pending,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: 'inst-1',
    installmentNumber: 1,
    totalInstallments: 12,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    hasReceipt: false,
    createdAt: '2026-02-05T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '4',
    accountId: '1',
    categoryId: '4',
    type: TransactionType.Debit,
    amount: 100.00,
    description: 'Transação cancelada',
    competenceDate: '2026-02-08',
    dueDate: null,
    status: TransactionStatus.Cancelled,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: 'Transação duplicada',
    cancelledBy: 'user-1',
    cancelledAt: '2026-02-09T10:00:00Z',
    isOverdue: false,
    hasReceipt: false,
    createdAt: '2026-02-08T10:00:00Z',
    updatedAt: '2026-02-09T10:00:00Z',
  },
  {
    id: '5',
    accountId: '2',
    categoryId: '5',
    type: TransactionType.Debit,
    amount: 49.90,
    description: 'Netflix Mensalidade',
    competenceDate: '2026-02-01',
    dueDate: '2026-02-01',
    status: TransactionStatus.Paid,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: true,
    recurrenceTemplateId: 'rec-1',
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    hasReceipt: false,
    createdAt: '2026-02-01T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '6',
    accountId: '1',
    categoryId: '6',
    type: TransactionType.Debit,
    amount: 500.00,
    description: 'Transferência Interna',
    competenceDate: '2026-02-15',
    dueDate: null,
    status: TransactionStatus.Paid,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: 'transfer-1',
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    hasReceipt: false,
    createdAt: '2026-02-15T10:00:00Z',
    updatedAt: null,
  },
];

const mockHistory: TransactionHistoryEntry[] = [
  {
    id: 'hist-1',
    transactionId: '1',
    action: 'Created',
    performedBy: 'João Silva',
    performedAt: '2026-02-10T10:00:00Z',
    details: null,
  },
];

const receiptByTransactionId: Record<string, {
  establishment: {
    id: string;
    name: string;
    cnpj: string;
    accessKey: string;
  };
  items: Array<{
    id: string;
    description: string;
    productCode: string | null;
    quantity: number;
    unitOfMeasure: string;
    unitPrice: number;
    totalPrice: number;
    itemOrder: number;
  }>;
}> = {
  '1': {
    establishment: {
      id: 'est-1',
      name: 'SUPERMERCADO PÃO DE AÇÚCAR',
      cnpj: '12345678000190',
      accessKey: '12345678901234567890123456789012345678901234',
    },
    items: [
      {
        id: 'item-1',
        description: 'ARROZ TIPO 1 5KG',
        productCode: '7891234567890',
        quantity: 2,
        unitOfMeasure: 'UN',
        unitPrice: 25.9,
        totalPrice: 51.8,
        itemOrder: 1,
      },
      {
        id: 'item-2',
        description: 'FEIJÃO CARIOCA 1KG',
        productCode: '7890000000001',
        quantity: 3,
        unitOfMeasure: 'UN',
        unitPrice: 8.4,
        totalPrice: 25.2,
        itemOrder: 2,
      },
    ],
  },
};

const BASE_URL = '*';

export const transactionsHandlers = [
  // GET /api/v1/transactions (com filtros e paginação)
  http.get(`${BASE_URL}/api/v1/transactions`, ({ request }) => {
    const url = new URL(request.url);
    const page = Number(url.searchParams.get('_page') ?? 1);
    const size = Number(url.searchParams.get('_size') ?? 20);
    
    // Filtros
    const accountId = url.searchParams.get('accountId');
    const categoryId = url.searchParams.get('categoryId');
    const type = url.searchParams.get('type');
    const status = url.searchParams.get('status');
    
    let filtered = [...mockTransactions];
    
    if (accountId) {
      filtered = filtered.filter((t) => t.accountId === accountId);
    }
    
    if (categoryId) {
      filtered = filtered.filter((t) => t.categoryId === categoryId);
    }
    
    if (type) {
      filtered = filtered.filter((t) => t.type === Number(type));
    }
    
    if (status) {
      filtered = filtered.filter((t) => t.status === Number(status));
    }
    
    const total = filtered.length;
    const totalPages = Math.ceil(total / size);
    const start = (page - 1) * size;
    const end = start + size;
    const data = filtered.slice(start, end);
    
    const response: PagedResponse<TransactionResponse> = {
      data,
      pagination: {
        page,
        size,
        total,
        totalPages,
      },
    };
    
    return HttpResponse.json(response);
  }),

  // GET /api/v1/transactions/:id
  http.get(`${BASE_URL}/api/v1/transactions/:id`, ({ params }) => {
    const { id } = params;
    const transaction = mockTransactions.find((t) => t.id === id);

    if (!transaction) {
      return new HttpResponse(null, { status: 404 });
    }

    return HttpResponse.json(transaction);
  }),

  // POST /api/v1/transactions (criar simples)
  http.post(`${BASE_URL}/api/v1/transactions`, async ({ request }) => {
    const body = (await request.json()) as CreateTransactionRequest;

    const newTransaction: TransactionResponse = {
      id: String(mockTransactions.length + 1),
      accountId: body.accountId,
      categoryId: body.categoryId,
      type: body.type,
      amount: body.amount,
      description: body.description,
      competenceDate: body.competenceDate,
      dueDate: body.dueDate ?? null,
      status: body.status,
      isAdjustment: false,
      originalTransactionId: null,
      hasAdjustment: false,
      installmentGroupId: null,
      installmentNumber: null,
      totalInstallments: null,
      isRecurrent: false,
      recurrenceTemplateId: null,
      transferGroupId: null,
      cancellationReason: null,
      cancelledBy: null,
      cancelledAt: null,
      isOverdue: false,
      hasReceipt: false,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    mockTransactions.push(newTransaction);
    return HttpResponse.json(newTransaction, { status: 201 });
  }),

  // POST /api/v1/transactions/installments
  http.post(`${BASE_URL}/api/v1/transactions/installments`, async ({ request }) => {
    const body = (await request.json()) as CreateInstallmentRequest;
    const installmentGroupId = `inst-${Date.now()}`;
    const installments: TransactionResponse[] = [];

    for (let i = 0; i < body.installmentCount; i++) {
      const installment: TransactionResponse = {
        id: String(mockTransactions.length + i + 1),
        accountId: body.accountId,
        categoryId: body.categoryId,
        type: body.type,
        amount: body.totalAmount / body.installmentCount,
        description: `${body.description} (${i + 1}/${body.installmentCount})`,
        competenceDate: body.firstCompetenceDate,
        dueDate: body.firstDueDate ?? null,
        status: TransactionStatus.Pending,
        isAdjustment: false,
        originalTransactionId: null,
        hasAdjustment: false,
        installmentGroupId,
        installmentNumber: i + 1,
        totalInstallments: body.installmentCount,
        isRecurrent: false,
        recurrenceTemplateId: null,
        transferGroupId: null,
        cancellationReason: null,
        cancelledBy: null,
        cancelledAt: null,
        isOverdue: false,
        hasReceipt: false,
        createdAt: new Date().toISOString(),
        updatedAt: null,
      };

      installments.push(installment);
      mockTransactions.push(installment);
    }

    return HttpResponse.json(installments, { status: 201 });
  }),

  // POST /api/v1/transactions/recurrences
  http.post(`${BASE_URL}/api/v1/transactions/recurrences`, async ({ request }) => {
    const body = (await request.json()) as CreateRecurrenceRequest;

    const newTransaction: TransactionResponse = {
      id: String(mockTransactions.length + 1),
      accountId: body.accountId,
      categoryId: body.categoryId,
      type: body.type,
      amount: body.amount,
      description: body.description,
      competenceDate: body.startDate,
      dueDate: body.dueDate ?? null,
      status: TransactionStatus.Pending,
      isAdjustment: false,
      originalTransactionId: null,
      hasAdjustment: false,
      installmentGroupId: null,
      installmentNumber: null,
      totalInstallments: null,
      isRecurrent: true,
      recurrenceTemplateId: `rec-${Date.now()}`,
      transferGroupId: null,
      cancellationReason: null,
      cancelledBy: null,
      cancelledAt: null,
      isOverdue: false,
      hasReceipt: false,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    mockTransactions.push(newTransaction);
    return HttpResponse.json(newTransaction, { status: 201 });
  }),

  // POST /api/v1/transactions/transfers
  http.post(`${BASE_URL}/api/v1/transactions/transfers`, async ({ request }) => {
    const body = (await request.json()) as CreateTransferRequest;
    const transferGroupId = `transfer-${Date.now()}`;

    const debit: TransactionResponse = {
      id: String(mockTransactions.length + 1),
      accountId: body.sourceAccountId,
      categoryId: body.categoryId,
      type: TransactionType.Debit,
      amount: body.amount,
      description: `Transferência: ${body.description}`,
      competenceDate: body.competenceDate,
      dueDate: null,
      status: TransactionStatus.Paid,
      isAdjustment: false,
      originalTransactionId: null,
      hasAdjustment: false,
      installmentGroupId: null,
      installmentNumber: null,
      totalInstallments: null,
      isRecurrent: false,
      recurrenceTemplateId: null,
      transferGroupId,
      cancellationReason: null,
      cancelledBy: null,
      cancelledAt: null,
      isOverdue: false,
      hasReceipt: false,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    const credit: TransactionResponse = {
      ...debit,
      id: String(mockTransactions.length + 2),
      accountId: body.destinationAccountId,
      type: TransactionType.Credit,
    };

    mockTransactions.push(debit, credit);
    return HttpResponse.json([debit, credit], { status: 201 });
  }),

  // POST /api/v1/transactions/:id/adjustments
  http.post(`${BASE_URL}/api/v1/transactions/:id/adjustments`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as AdjustTransactionRequest;
    const transactionIndex = mockTransactions.findIndex((t) => t.id === id);

    if (transactionIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    const original = mockTransactions[transactionIndex];
    
    const adjustment: TransactionResponse = {
      ...original,
      id: String(mockTransactions.length + 1),
      amount: body.newAmount,
      isAdjustment: true,
      originalTransactionId: original.id,
      createdAt: new Date().toISOString(),
    };

    // Marca original como tendo ajuste
    mockTransactions[transactionIndex] = {
      ...original,
      hasAdjustment: true,
    };

    mockTransactions.push(adjustment);
    return HttpResponse.json(adjustment, { status: 201 });
  }),

  // POST /api/v1/transactions/:id/cancel
  http.post(`${BASE_URL}/api/v1/transactions/:id/cancel`, async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as CancelTransactionRequest;
    const transactionIndex = mockTransactions.findIndex((t) => t.id === id);

    if (transactionIndex === -1) {
      return new HttpResponse(null, { status: 404 });
    }

    mockTransactions[transactionIndex] = {
      ...mockTransactions[transactionIndex],
      status: TransactionStatus.Cancelled,
      cancellationReason: body.reason ?? null,
      cancelledBy: 'user-test',
      cancelledAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    return new HttpResponse(null, { status: 204 });
  }),

  // POST /api/v1/transactions/recurrences/:id/deactivate
  http.post(`${BASE_URL}/api/v1/transactions/recurrences/:id/deactivate`, ({ params }) => {
    const { id } = params;
    const hasRecurrence = mockTransactions.some((t) => t.recurrenceTemplateId === id);

    if (!hasRecurrence) {
      return new HttpResponse(null, { status: 404 });
    }

    return new HttpResponse(null, { status: 204 });
  }),

  // GET /api/v1/transactions/:id/history
  http.get(`${BASE_URL}/api/v1/transactions/:id/history`, () => {
    return HttpResponse.json(mockHistory);
  }),

  // POST /api/v1/receipts/lookup
  http.post(`${BASE_URL}/api/v1/receipts/lookup`, async ({ request }) => {
    const body = (await request.json()) as { input?: string };
    const input = body.input?.replace(/\D/g, '') ?? '';

    if (input.length !== 44) {
      return HttpResponse.json(
        {
          title: 'Bad Request',
          status: 400,
          detail: 'Chave de acesso inválida.',
        },
        { status: 400 },
      );
    }

    return HttpResponse.json({
      accessKey: input,
      establishmentName: 'SUPERMERCADO PÃO DE AÇÚCAR',
      establishmentCnpj: '12345678000190',
      issuedAt: '2026-02-10T14:30:00Z',
      totalAmount: 350,
      discountAmount: 10,
      paidAmount: 340,
      items: [
        {
          id: 'lookup-item-1',
          description: 'ARROZ TIPO 1 5KG',
          productCode: '7891234567890',
          quantity: 2,
          unitOfMeasure: 'UN',
          unitPrice: 25.9,
          totalPrice: 51.8,
          itemOrder: 1,
        },
      ],
      alreadyImported: false,
    });
  }),

  // POST /api/v1/receipts/import
  http.post(`${BASE_URL}/api/v1/receipts/import`, async ({ request }) => {
    const body = (await request.json()) as {
      accessKey: string;
      accountId: string;
      categoryId: string;
      description: string;
      competenceDate: string;
    };

    if (body.accessKey === '00000000000000000000000000000000000000000000') {
      return HttpResponse.json(
        {
          title: 'Conflict',
          status: 409,
          detail: 'Cupom já importado.',
        },
        { status: 409 },
      );
    }

    const newTransaction: TransactionResponse = {
      id: String(mockTransactions.length + 1),
      accountId: body.accountId,
      categoryId: body.categoryId,
      type: TransactionType.Debit,
      amount: 340,
      description: body.description,
      competenceDate: body.competenceDate,
      dueDate: null,
      status: TransactionStatus.Paid,
      isAdjustment: false,
      originalTransactionId: null,
      hasAdjustment: false,
      installmentGroupId: null,
      installmentNumber: null,
      totalInstallments: null,
      isRecurrent: false,
      recurrenceTemplateId: null,
      transferGroupId: null,
      cancellationReason: null,
      cancelledBy: null,
      cancelledAt: null,
      isOverdue: false,
      hasReceipt: true,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };

    mockTransactions.unshift(newTransaction);

    const receiptPayload = {
      establishment: {
        id: `est-${newTransaction.id}`,
        name: 'SUPERMERCADO PÃO DE AÇÚCAR',
        cnpj: '12345678000190',
        accessKey: body.accessKey,
      },
      items: [
        {
          id: `item-${newTransaction.id}-1`,
          description: 'ARROZ TIPO 1 5KG',
          productCode: '7891234567890',
          quantity: 2,
          unitOfMeasure: 'UN',
          unitPrice: 25.9,
          totalPrice: 51.8,
          itemOrder: 1,
        },
      ],
    };

    receiptByTransactionId[newTransaction.id] = receiptPayload;

    return HttpResponse.json(
      {
        transaction: newTransaction,
        establishment: receiptPayload.establishment,
        items: receiptPayload.items,
      },
      { status: 201 },
    );
  }),

  // GET /api/v1/transactions/:id/receipt
  http.get(`${BASE_URL}/api/v1/transactions/:id/receipt`, ({ params }) => {
    const { id } = params;
    const receipt = receiptByTransactionId[String(id)];

    if (!receipt) {
      return new HttpResponse(null, { status: 404 });
    }

    return HttpResponse.json(receipt);
  }),
];
