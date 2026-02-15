import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/components/ui/table';
import { formatCurrency, formatDate } from '@/shared/utils/formatters';

interface InstallmentPreviewItem {
  number: number;
  competenceDate: string;
  dueDate: string | null;
  amount: number;
}

interface InstallmentPreviewProps {
  totalAmount: number;
  installmentCount: number;
  firstCompetenceDate: string;
  firstDueDate?: string;
}

function calculateInstallments(
  totalAmount: number,
  count: number,
  firstDate: string,
  firstDue?: string
): InstallmentPreviewItem[] {
  const installmentValue = Math.floor((totalAmount * 100) / count) / 100;
  const remainder = totalAmount - installmentValue * count;

  return Array.from({ length: count }, (_, i) => {
    const competenceDate = addMonths(firstDate, i);
    const dueDate = firstDue ? addMonths(firstDue, i) : null;

    return {
      number: i + 1,
      competenceDate,
      dueDate,
      amount: i === 0 ? installmentValue + remainder : installmentValue,
    };
  });
}

function addMonths(dateString: string, months: number): string {
  const date = new Date(dateString + 'T00:00:00');
  date.setMonth(date.getMonth() + months);
  
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  
  return `${year}-${month}-${day}`;
}

export function InstallmentPreview({
  totalAmount,
  installmentCount,
  firstCompetenceDate,
  firstDueDate,
}: InstallmentPreviewProps) {
  if (!totalAmount || !installmentCount || !firstCompetenceDate) {
    return (
      <div className="text-center text-sm text-muted-foreground py-8">
        Preencha os campos acima para ver o preview das parcelas
      </div>
    );
  }

  const installments = calculateInstallments(
    totalAmount,
    installmentCount,
    firstCompetenceDate,
    firstDueDate
  );

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="font-semibold">Preview das Parcelas</h4>
        <span className="text-sm text-muted-foreground">
          Total: {formatCurrency(totalAmount)}
        </span>
      </div>

      <div className="max-h-64 overflow-y-auto rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Parcela</TableHead>
              <TableHead>Data Competência</TableHead>
              <TableHead>Data Vencimento</TableHead>
              <TableHead className="text-right">Valor</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {installments.map((installment) => (
              <TableRow key={installment.number}>
                <TableCell>
                  {installment.number}/{installmentCount}
                </TableCell>
                <TableCell>{formatDate(installment.competenceDate)}</TableCell>
                <TableCell>
                  {installment.dueDate ? formatDate(installment.dueDate) : '--'}
                </TableCell>
                <TableCell className="text-right font-medium">
                  {formatCurrency(installment.amount)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
