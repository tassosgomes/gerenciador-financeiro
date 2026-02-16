using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

public sealed class SeedInvoicePaymentCategoryStartupTask : IStartupTask
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SeedInvoicePaymentCategoryStartupTask> _logger;

    public SeedInvoicePaymentCategoryStartupTask(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<SeedInvoicePaymentCategoryStartupTask> logger)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking if invoice payment category seeding is required");

        const string categoryName = "Pagamento de Fatura";
        var categoryType = CategoryType.Despesa;

        var existingCategories = await _categoryRepository.GetAllAsync(cancellationToken);
        var categoryExists = existingCategories.Any(c =>
            c.Name == categoryName
            && c.Type == categoryType
            && c.IsSystem);

        if (categoryExists)
        {
            _logger.LogInformation("Invoice payment category already exists. Skipping seed");
            return;
        }

        _logger.LogInformation("Seeding invoice payment system category");

        try
        {
            var category = Category.Restore(
                id: Guid.NewGuid(),
                name: categoryName,
                type: categoryType,
                isActive: true,
                isSystem: true,
                createdBy: "system",
                createdAt: DateTime.UtcNow,
                updatedBy: null,
                updatedAt: null);

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice payment system category seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed invoice payment system category");
            throw;
        }
    }
}
