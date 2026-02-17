using GestorFinanceiro.Financeiro.Application.Common;

namespace GestorFinanceiro.Financeiro.Application.Commands.Category;

public record DeleteCategoryCommand(
    Guid CategoryId,
    Guid? MigrateToCategoryId,
    string UserId
) : ICommand<Unit>;
