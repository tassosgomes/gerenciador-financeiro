using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Category;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.Category;

public class ListCategoriesQueryHandler : IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ListCategoriesQueryHandler> _logger;

    public ListCategoriesQueryHandler(
        ICategoryRepository categoryRepository,
        ILogger<ListCategoriesQueryHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryResponse>> HandleAsync(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing categories with type filter: {Type}", query.Type);

        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        if (query.Type.HasValue)
        {
            categories = categories
                .Where(category => category.Type == query.Type.Value)
                .ToList();
        }

        return categories.Adapt<IReadOnlyList<CategoryResponse>>();
    }
}
