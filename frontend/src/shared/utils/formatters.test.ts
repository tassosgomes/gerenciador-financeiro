import { formatCompetenceMonth, formatCurrency, formatDate } from './formatters';

describe('formatters', () => {
  it('formats currency in BRL format', () => {
    expect(formatCurrency(1234.56)).toBe('R$\u00a01.234,56');
  });

  it('formats date in pt-BR format', () => {
    expect(formatDate('2026-01-15')).toBe('15/01/2026');
  });

  it('formats competence month in pt-BR', () => {
    expect(formatCompetenceMonth(10, 2026)).toBe('outubro de 2026');
  });
});
