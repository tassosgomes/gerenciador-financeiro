import { http, HttpResponse } from 'msw';

import {
  getTransactionReceipt,
  importReceipt,
  lookupReceipt,
} from '@/features/transactions/api/receiptApi';
import { server } from '@/shared/test/mocks/server';

describe('receiptApi', () => {
  it('lookupReceipt should return receipt lookup payload', async () => {
    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json({
          accessKey: '12345678901234567890123456789012345678901234',
          establishmentName: 'SUPERMERCADO TESTE',
          establishmentCnpj: '12345678000190',
          issuedAt: '2026-02-20T14:30:00Z',
          totalAmount: 150,
          discountAmount: 5,
          paidAmount: 145,
          items: [
            {
              id: '00000000-0000-0000-0000-000000000000',
              description: 'ARROZ 5KG',
              productCode: '7891234567890',
              quantity: 1,
              unitOfMeasure: 'UN',
              unitPrice: 25.9,
              totalPrice: 25.9,
              itemOrder: 1,
            },
          ],
          alreadyImported: false,
        }),
      ),
    );

    const response = await lookupReceipt({
      input: '12345678901234567890123456789012345678901234',
    });

    expect(response.accessKey).toBe('12345678901234567890123456789012345678901234');
    expect(response.items).toHaveLength(1);
  });

  it('importReceipt should return transaction and items', async () => {
    server.use(
      http.post('*/api/v1/receipts/import', () =>
        HttpResponse.json(
          {
            transaction: {
              id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
              categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
              type: 1,
              amount: 145,
              description: 'Supermercado Teste',
              competenceDate: '2026-02-20',
              dueDate: null,
              status: 1,
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
              createdAt: '2026-02-20T14:30:00Z',
              updatedAt: null,
            },
            establishment: {
              id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
              name: 'SUPERMERCADO TESTE',
              cnpj: '12345678000190',
              accessKey: '12345678901234567890123456789012345678901234',
            },
            items: [
              {
                id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
                description: 'ARROZ 5KG',
                productCode: '7891234567890',
                quantity: 1,
                unitOfMeasure: 'UN',
                unitPrice: 25.9,
                totalPrice: 25.9,
                itemOrder: 1,
              },
            ],
          },
          { status: 201 },
        ),
      ),
    );

    const response = await importReceipt({
      accessKey: '12345678901234567890123456789012345678901234',
      accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
      categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
      description: 'Supermercado Teste',
      competenceDate: '2026-02-20',
    });

    expect(response.transaction.id).toBe('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');
    expect(response.items).toHaveLength(1);
  });

  it('getTransactionReceipt should return establishment and items', async () => {
    server.use(
      http.get('*/api/v1/transactions/:transactionId/receipt', () =>
        HttpResponse.json({
          establishment: {
            id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
            name: 'SUPERMERCADO TESTE',
            cnpj: '12345678000190',
            accessKey: '12345678901234567890123456789012345678901234',
          },
          items: [
            {
              id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
              description: 'ARROZ 5KG',
              productCode: '7891234567890',
              quantity: 1,
              unitOfMeasure: 'UN',
              unitPrice: 25.9,
              totalPrice: 25.9,
              itemOrder: 1,
            },
          ],
        }),
      ),
    );

    const response = await getTransactionReceipt('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');

    expect(response.establishment.name).toBe('SUPERMERCADO TESTE');
    expect(response.items[0].description).toBe('ARROZ 5KG');
  });
});
