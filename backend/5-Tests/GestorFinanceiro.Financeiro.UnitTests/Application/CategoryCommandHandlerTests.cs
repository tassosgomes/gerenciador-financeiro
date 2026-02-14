using AwesomeAssertions;
using FluentValidation;
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
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateCategoryCommandHandler>> _logger = new();

    private readonly CreateCategoryCommandHandler _sut;

    public CategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(mock => mock.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(mock => mock.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _categoryRepository
            .Setup(mock => mock.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category category, CancellationToken _) => category);

        _sut = new CreateCategoryCommandHandler(
            _categoryRepository.Object,
            _operationLogRepository.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_ComandoValido_CriaCategoriaComSucesso()
    {
        var command = new CreateCategoryCommand("Alimentacao", CategoryType.Despesa, "user-1");

        _categoryRepository
            .Setup(mock => mock.ExistsByNameAndTypeAsync(command.Name, command.Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _sut.HandleAsync(command, CancellationToken.None);

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

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<CategoryNameAlreadyExistsException>();
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ThrowsValidationException()
    {
        var command = new CreateCategoryCommand(string.Empty, CategoryType.Despesa, "user-1");

        var action = () => _sut.HandleAsync(command, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
        _categoryRepository.Verify(mock => mock.ExistsByNameAndTypeAsync(It.IsAny<string>(), It.IsAny<CategoryType>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(mock => mock.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
