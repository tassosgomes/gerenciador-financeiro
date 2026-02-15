using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.Application;

public class CategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IOperationLogRepository> _operationLogRepository = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateCategoryCommandHandler>> _loggerCreate = new();
    private readonly Mock<ILogger<UpdateCategoryCommandHandler>> _loggerUpdate = new();

    private readonly CreateCategoryCommandHandler _sutCreate;
    private readonly UpdateCategoryCommandHandler _sutUpdate;

    public CategoryCommandHandlerTests()
    {
        _auditService.Setup(mock => mock.LogAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _categoryRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category category, CancellationToken _) => category);

        _sutCreate = new CreateCategoryCommandHandler(
            _categoryRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _loggerCreate.Object);

        _sutUpdate = new UpdateCategoryCommandHandler(
            _categoryRepository.Object,
            _operationLogRepository.Object,
            _auditService.Object,
            _unitOfWork.Object,
            _loggerUpdate.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaCategoriaComSucesso()
    {
        var command = new CreateCategoryCommand("Alimentacao", CategoryType.Despesa, "user-1");

        _categoryRepository
            .Setup(mock => mock.ExistsByNameAndTypeAsync(command.Name, command.Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _sutCreate.HandleAsync(command, CancellationToken.None);

        response.Name.Should().Be("Alimentacao");
        response.Type.Should().Be(CategoryType.Despesa);
        _categoryRepository.Verify(mock => mock.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NomeDuplicado_LancaCategoryNameAlreadyExistsException()
    {
        var command = new CreateCategoryCommand("Alimentacao", CategoryType.Despesa, "user-1");

        _categoryRepository
            .Setup(mock => mock.ExistsByNameAndTypeAsync(command.Name, command.Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => _sutCreate.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<CategoryNameAlreadyExistsException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ThrowsValidationException()
    {
        var command = new CreateCategoryCommand(string.Empty, CategoryType.Despesa, "user-1");

        var action = () => _sutCreate.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        _categoryRepository.Verify(mock => mock.ExistsByNameAndTypeAsync(It.IsAny<string>(), It.IsAny<CategoryType>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryHandler_ComandoValido_AtualizaCategoriaComSucesso()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = Category.Create("Lazer", CategoryType.Despesa, "user-1");
        var command = new UpdateCategoryCommand(categoryId, "Entretenimento", "user-2");

        _categoryRepository
            .Setup(mock => mock.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var response = await _sutUpdate.HandleAsync(command, CancellationToken.None);

        // Assert
        response.Name.Should().Be("Entretenimento");
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryHandler_CategoriaDoSistema_LancaSystemCategoryCannotBeChangedException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var systemCategory = Category.Restore(
            categoryId,
            "Alimentação",
            CategoryType.Despesa,
            isActive: true,
            isSystem: true,
            "system",
            DateTime.UtcNow,
            null,
            null);

        var command = new UpdateCategoryCommand(categoryId, "Novo Nome", "user-1");

        _categoryRepository
            .Setup(mock => mock.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemCategory);

        // Act
        var action = () => _sutUpdate.HandleAsync(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<SystemCategoryCannotBeChangedException>();
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(mock => mock.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryHandler_CategoriaInexistente_LancaCategoryNotFoundException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new UpdateCategoryCommand(categoryId, "Novo Nome", "user-1");

        _categoryRepository
            .Setup(mock => mock.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var action = () => _sutUpdate.HandleAsync(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<CategoryNotFoundException>();
        _unitOfWork.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCategoryHandler_ComandoInvalido_LancaValidationException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new UpdateCategoryCommand(categoryId, string.Empty, "user-1");

        // Act
        var action = () => _sutUpdate.HandleAsync(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
        _categoryRepository.Verify(mock => mock.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
