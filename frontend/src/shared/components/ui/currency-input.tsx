import * as React from 'react';
import { Input } from '@/shared/components/ui/input';
import { cn } from '@/shared/lib/utils';

export interface CurrencyInputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> {
  value?: number;
  onChange?: (value: number) => void;
}

const CurrencyInput = React.forwardRef<HTMLInputElement, CurrencyInputProps>(
  ({ className, value, onChange, ...props }, ref) => {
    const [displayValue, setDisplayValue] = React.useState('');

    // Formata o valor numérico para exibição
    const formatCurrency = (num: number) => {
      return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL',
      }).format(num);
    };

    // Atualiza display quando value muda externamente
    React.useEffect(() => {
      if (value !== undefined) {
        setDisplayValue(formatCurrency(value));
      }
    }, [value]);

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const input = e.target.value;

      // Remove tudo exceto dígitos
      const digitsOnly = input.replace(/\D/g, '');

      if (digitsOnly === '') {
        setDisplayValue('R$ 0,00');
        onChange?.(0);
        return;
      }

      // Converte para centavos e depois para reais
      const numericValue = parseInt(digitsOnly, 10) / 100;
      setDisplayValue(formatCurrency(numericValue));
      onChange?.(numericValue);
    };

    const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
      // Seleciona tudo ao focar para facilitar a edição
      e.target.select();
    };

    return (
      <Input
        ref={ref}
        type="text"
        inputMode="numeric"
        value={displayValue}
        onChange={handleChange}
        onFocus={handleFocus}
        className={cn('text-center text-2xl font-bold', className)}
        {...props}
      />
    );
  }
);

CurrencyInput.displayName = 'CurrencyInput';

export { CurrencyInput };
