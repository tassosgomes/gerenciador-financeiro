namespace GestorFinanceiro.Financeiro.Application.Common;

public record PagedResult<T>(IEnumerable<T> Data, PaginationMetadata Pagination);
