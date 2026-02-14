using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Queries.Category;

public record ListCategoriesQuery(
    CategoryType? Type = null
) : IQuery<IReadOnlyList<CategoryResponse>>;
