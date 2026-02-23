import { render, screen } from '@testing-library/react';

import { ReceiptPreview } from '@/features/transactions/components/ReceiptPreview';
import type { ReceiptLookupResponse } from '@/features/transactions/types/receipt';

describe('ReceiptPreview', () => {
  const receipt: ReceiptLookupResponse = {
    accessKey: '12345678901234567890123456789012345678901234',
    establishmentName: 'SUPERMERCADO TESTE',
    establishmentCnpj: '12345678000190',
    issuedAt: '2026-02-20T14:30:00Z',
    totalAmount: 150,
    discountAmount: 5,
    paidAmount: 145,
    items: [
      {
        id: 'item-1',
        description: 'ARROZ 5KG',
        productCode: '7891234567890',
        quantity: 2,
        unitOfMeasure: 'UN',
        unitPrice: 25.9,
        totalPrice: 51.8,
        itemOrder: 1,
      },
      {
        id: 'item-2',
        description: 'FEIJÃO 1KG',
        productCode: '7891234567800',
        quantity: 1,
        unitOfMeasure: 'UN',
        unitPrice: 12.5,
        totalPrice: 12.5,
        itemOrder: 2,
      },
    ],
    alreadyImported: false,
  };

  it('renders receipt items table and totals', () => {
    render(<ReceiptPreview receipt={receipt} issuedAt={receipt.issuedAt} />);

    expect(screen.getByText('Preview do Cupom Fiscal')).toBeInTheDocument();
    expect(screen.getByText('SUPERMERCADO TESTE')).toBeInTheDocument();
    expect(screen.getByText('12.345.678/0001-90')).toBeInTheDocument();
    expect(screen.getByText('ARROZ 5KG')).toBeInTheDocument();
    expect(screen.getByText('FEIJÃO 1KG')).toBeInTheDocument();
    expect(screen.getByText('Subtotal')).toBeInTheDocument();
    expect(screen.getByText('Total Pago')).toBeInTheDocument();
  });

  it('shows discount row and formatted currency values', () => {
    render(<ReceiptPreview receipt={receipt} issuedAt={receipt.issuedAt} />);

    expect(screen.getByText('Desconto')).toBeInTheDocument();
    expect(screen.getByText('- R$ 5,00')).toBeInTheDocument();
    expect(screen.getByText('R$ 145,00')).toBeInTheDocument();
  });
});
