import type { AxiosError } from 'axios';

interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}

export const ERROR_MESSAGES: Record<string, string> = {
  // Contas
  'AccountNameAlreadyExists': 'Já existe uma conta com este nome.',
  'AccountNotFound': 'Conta não encontrada.',
  'AccountInactive': 'Conta inativa. Ative a conta para realizar operações.',
  'InsufficientBalance': 'Saldo insuficiente para esta operação.',

  // Categorias
  'CategoryNameAlreadyExists': 'Já existe uma categoria com este nome.',
  'CategoryNotFound': 'Categoria não encontrada.',

  // Transações
  'TransactionNotFound': 'Transação não encontrada.',
  'TransactionAlreadyCancelled': 'Esta transação já foi cancelada.',
  'CannotCancelPaidTransaction': 'Não é possível cancelar uma transação já paga.',
  'InvalidTransactionStatus': 'Status de transação inválido.',

  // Autenticação
  'InvalidCredentials': 'Credenciais inválidas. Verifique seu e-mail e senha.',
  'TokenExpired': 'Sua sessão expirou. Faça login novamente.',
  'Unauthorized': 'Você não tem permissão para realizar esta ação.',

  // Usuários
  'UserEmailAlreadyExists': 'Já existe um usuário com este e-mail.',
  'UserNotFound': 'Usuário não encontrado.',

  // Backup
  'InvalidBackupFile': 'Arquivo de backup inválido ou corrompido.',
  'BackupImportFailed': 'Falha ao importar backup. Verifique o arquivo e tente novamente.',
};

export function getErrorMessage(error: unknown): string {
  // Tratamento de erros Axios
  if (error && typeof error === 'object' && 'isAxiosError' in error) {
    const axiosError = error as AxiosError<ProblemDetails>;

    // Problem Details (RFC 9457)
    const problem = axiosError.response?.data;
    if (problem?.type) {
      const key = problem.type.split('/').pop() ?? '';
      const message = ERROR_MESSAGES[key];
      if (message) return message;

      // Fallback para detail do Problem Details
      if (problem.detail) return problem.detail;
    }

    // Erros de rede específicos
    if (axiosError.code === 'ECONNABORTED') {
      return 'Tempo de conexão esgotado. Verifique sua internet e tente novamente.';
    }

    if (axiosError.code === 'ERR_NETWORK') {
      return 'Erro de conexão. Verifique sua internet e tente novamente.';
    }

    // Erros HTTP sem Problem Details
    if (axiosError.response) {
      const status = axiosError.response.status;
      if (status === 401) return ERROR_MESSAGES.Unauthorized;
      if (status === 403) return 'Você não tem permissão para acessar este recurso.';
      if (status === 404) return 'Recurso não encontrado.';
      if (status === 500) return 'Erro no servidor. Tente novamente mais tarde.';
      if (status >= 500) return 'Erro no servidor. Tente novamente mais tarde.';
    }

    // Sem resposta do servidor
    if (!axiosError.response) {
      return 'Erro de conexão. Verifique sua internet e tente novamente.';
    }
  }

  // Erros genéricos do JavaScript
  if (error instanceof Error) {
    return error.message || 'Erro inesperado. Tente novamente.';
  }

  // Fallback final
  return 'Erro inesperado. Tente novamente.';
}
