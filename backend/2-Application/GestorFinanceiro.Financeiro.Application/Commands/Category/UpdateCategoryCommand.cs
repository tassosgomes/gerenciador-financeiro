using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string UserId,
    string? OperationId = null
) : ICommand<CategoryResponse>;