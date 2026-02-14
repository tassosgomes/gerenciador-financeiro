using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public record CreateCategoryCommand(
    string Name,
    CategoryType Type,
    string UserId,
    string? OperationId = null
) : ICommand<CategoryResponse>;