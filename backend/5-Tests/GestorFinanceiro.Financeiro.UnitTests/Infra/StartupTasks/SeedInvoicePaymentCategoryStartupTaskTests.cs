using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.StartupTasks;

public class SeedInvoicePaymentCategoryStartupTaskTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<SeedInvoicePaymentCategoryStartupTask>> _loggerMock;

    public SeedInvoicePaymentCategoryStartupTaskTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<SeedInvoicePaymentCategoryStartupTask>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryDoesNotExist_ShouldCreateInvoicePaymentCategory()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Category>(c =>
                    c.Name == "Pagamento de Fatura" &&
                    c.Type == CategoryType.Despesa &&
                    c.IsSystem &&
                    c.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSystemCategoryAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var existingCategory = Category.Restore(
            id: Guid.NewGuid(),
            name: "Pagamento de Fatura",
            type: CategoryType.Despesa,
            isActive: true,
            isSystem: true,
            createdBy: "system",
            createdAt: DateTime.UtcNow,
            updatedBy: null,
            updatedAt: null);

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { existingCategory });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNonSystemCategoryWithSameNameExists_ShouldCreateSystemCategory()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var userCategory = Category.Create("Pagamento de Fatura", CategoryType.Despesa, "user");

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { userCategory });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Category>(c =>
                    c.Name == "Pagamento de Fatura" &&
                    c.Type == CategoryType.Despesa &&
                    c.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryWithDifferentTypeExists_ShouldCreateSystemCategory()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var existingCategory = Category.Restore(
            id: Guid.NewGuid(),
            name: "Pagamento de Fatura",
            type: CategoryType.Receita, // Tipo diferente
            isActive: true,
            isSystem: true,
            createdBy: "system",
            createdAt: DateTime.UtcNow,
            updatedBy: null,
            updatedAt: null);

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { existingCategory });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Category>(c =>
                    c.Name == "Pagamento de Fatura" &&
                    c.Type == CategoryType.Despesa &&
                    c.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleCategoriesExist_ButNoneMatchesCriteria_ShouldCreateSystemCategory()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var category1 = Category.Create("Categoria 1", CategoryType.Despesa, "user");
        var category2 = Category.Create("Categoria 2", CategoryType.Receita, "user");

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { category1, category2 });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Category>(c =>
                    c.Name == "Pagamento de Fatura" &&
                    c.Type == CategoryType.Despesa &&
                    c.IsSystem),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutedMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var categories = new List<Category>();

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => categories.ToList());

        _categoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Callback<Category, CancellationToken>((c, ct) => categories.Add(c))
            .ReturnsAsync((Category c, CancellationToken ct) => c);

        var sut = CreateSut();

        // Act - primeira execução
        await sut.ExecuteAsync(cancellationToken);

        // Act - segunda execução
        await sut.ExecuteAsync(cancellationToken);

        // Assert - deve ter adicionado apenas uma vez
        _categoryRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database error");

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = CreateSut();

        // Act & Assert
        var action = () => sut.ExecuteAsync(cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveChangesThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Save failed");

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = CreateSut();

        // Act & Assert
        var action = () => sut.ExecuteAsync(cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Save failed");
    }

    private SeedInvoicePaymentCategoryStartupTask CreateSut()
    {
        return new SeedInvoicePaymentCategoryStartupTask(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }
}
