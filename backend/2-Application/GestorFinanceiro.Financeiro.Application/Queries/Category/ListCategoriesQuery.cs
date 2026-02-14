using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Queries.Category;

public record ListCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;