export function formatCnpj(cnpj: string): string {
  const digits = cnpj.replace(/\D/g, '');

  if (digits.length !== 14) {
    return cnpj;
  }

  return digits.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
}

export function formatAccessKey(accessKey: string): string {
  const digits = accessKey.replace(/\D/g, '');
  if (digits.length === 0) {
    return accessKey;
  }

  return digits.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
}

export function formatDateTime(date: string): string {
  const parsedDate = new Date(date);

  if (Number.isNaN(parsedDate.getTime())) {
    return '--';
  }

  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(parsedDate);
}
