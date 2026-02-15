import { getErrorMessage } from '@/shared/utils/errorMessages';
import type { AxiosError } from 'axios';

describe('getErrorMessage', () => {
  it('returns mapped error message for known Problem Details type', () => {
    const error = {
      isAxiosError: true,
      response: {
        data: {
          type: 'https://example.com/errors/AccountNameAlreadyExists',
          title: 'Account Name Already Exists',
          status: 400,
          detail: 'An account with this name already exists',
        },
        status: 400,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Já existe uma conta com este nome.');
  });

  it('returns detail from Problem Details when type is unknown', () => {
    const error = {
      isAxiosError: true,
      response: {
        data: {
          type: 'https://example.com/errors/UnknownError',
          title: 'Unknown Error',
          status: 400,
          detail: 'Something went wrong',
        },
        status: 400,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Something went wrong');
  });

  it('returns connection timeout message for ECONNABORTED', () => {
    const error = {
      isAxiosError: true,
      code: 'ECONNABORTED',
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Tempo de conexão esgotado. Verifique sua internet e tente novamente.');
  });

  it('returns network error message for ERR_NETWORK', () => {
    const error = {
      isAxiosError: true,
      code: 'ERR_NETWORK',
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Erro de conexão. Verifique sua internet e tente novamente.');
  });

  it('returns unauthorized message for 401 status', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 401,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Você não tem permissão para realizar esta ação.');
  });

  it('returns forbidden message for 403 status', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 403,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Você não tem permissão para acessar este recurso.');
  });

  it('returns not found message for 404 status', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 404,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Recurso não encontrado.');
  });

  it('returns server error message for 500 status', () => {
    const error = {
      isAxiosError: true,
      response: {
        status: 500,
      },
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Erro no servidor. Tente novamente mais tarde.');
  });

  it('returns connection error message when no response', () => {
    const error = {
      isAxiosError: true,
    } as unknown as AxiosError;

    expect(getErrorMessage(error)).toBe('Erro de conexão. Verifique sua internet e tente novamente.');
  });

  it('returns error message for generic Error instance', () => {
    const error = new Error('Generic error message');

    expect(getErrorMessage(error)).toBe('Generic error message');
  });

  it('returns fallback message for unknown error types', () => {
    const error = { something: 'unknown' };

    expect(getErrorMessage(error)).toBe('Erro inesperado. Tente novamente.');
  });
});
