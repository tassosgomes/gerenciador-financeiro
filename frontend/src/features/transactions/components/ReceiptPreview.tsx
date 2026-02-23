import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/components/ui/table';
import { formatCurrency } from '@/shared/utils/formatters';
import type { ReceiptLookupResponse, TransactionReceiptResponse } from '@/features/transactions/types/receipt';
import { formatCnpj, formatDateTime } from '@/features/transactions/utils/receiptFormatters';

interface ReceiptPreviewProps {
  receipt: ReceiptLookupResponse | TransactionReceiptResponse;
  issuedAt?: string;
  title?: string;
}

export function ReceiptPreview({ receipt, issuedAt, title = 'Preview do Cupom Fiscal' }: ReceiptPreviewProps): JSX.Element {
  const totalAmount = 'paidAmount' in receipt ? receipt.totalAmount : receipt.items.reduce((acc, item) => acc + item.totalPrice, 0);
  const discountAmount = 'discountAmount' in receipt ? receipt.discountAmount : 0;
  const paidAmount = 'paidAmount' in receipt ? receipt.paidAmount : totalAmount;
  const establishmentName = 'establishmentName' in receipt ? receipt.establishmentName : receipt.establishment.name;
  const establishmentCnpj = 'establishmentCnpj' in receipt ? receipt.establishmentCnpj : receipt.establishment.cnpj;

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
          <div>
            <p className="text-sm text-muted-foreground">Estabelecimento</p>
            <p className="font-medium">{establishmentName}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">CNPJ</p>
            <p className="font-medium">{formatCnpj(establishmentCnpj)}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Data da compra</p>
            <p className="font-medium">{issuedAt ? formatDateTime(issuedAt) : '--'}</p>
          </div>
        </div>

        <div className="overflow-x-auto">
          <Table className="min-w-[760px]">
            <TableHeader>
              <TableRow>
                <TableHead className="w-[60px]">#</TableHead>
                <TableHead>Descrição</TableHead>
                <TableHead className="text-right">Qtd</TableHead>
                <TableHead>Unidade</TableHead>
                <TableHead className="text-right">Valor Unitário (R$)</TableHead>
                <TableHead className="text-right">Valor Total (R$)</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {receipt.items.map((item, index) => (
                <TableRow key={item.id || `${item.description}-${index}`}>
                  <TableCell>{item.itemOrder || index + 1}</TableCell>
                  <TableCell>{item.description}</TableCell>
                  <TableCell className="text-right">{item.quantity.toLocaleString('pt-BR')}</TableCell>
                  <TableCell>{item.unitOfMeasure}</TableCell>
                  <TableCell className="text-right">{formatCurrency(item.unitPrice)}</TableCell>
                  <TableCell className="text-right">{formatCurrency(item.totalPrice)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
            <TableFooter>
              <TableRow>
                <TableCell colSpan={5} className="text-right font-medium">Subtotal</TableCell>
                <TableCell className="text-right">{formatCurrency(totalAmount)}</TableCell>
              </TableRow>
              {discountAmount > 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="text-right font-medium text-emerald-700">Desconto</TableCell>
                  <TableCell className="text-right text-emerald-700">- {formatCurrency(discountAmount)}</TableCell>
                </TableRow>
              )}
              <TableRow>
                <TableCell colSpan={5} className="text-right text-base font-semibold">Total Pago</TableCell>
                <TableCell className="text-right text-base font-semibold">{formatCurrency(paidAmount)}</TableCell>
              </TableRow>
            </TableFooter>
          </Table>
        </div>
      </CardContent>
    </Card>
  );
}
