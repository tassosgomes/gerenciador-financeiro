using AwesomeAssertions;
using FluentValidation;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.Application.Commands.Backup;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Application.Queries.Backup;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class BackupControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly BackupController _controller;

    public BackupControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new BackupController(_dispatcherMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task ExportAsync_ShouldReturnOkWithContentDispositionHeader()
    {
        var response = new BackupExportDto(DateTime.UtcNow, "1.0", BuildValidData());
        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ExportBackupQuery, BackupExportDto>(
                It.IsAny<ExportBackupQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ExportAsync(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        _controller.Response.Headers.ContentDisposition.ToString().Should().Contain("attachment; filename=");
    }

    [Fact]
    public async Task ImportAsync_WithValidData_ShouldReturnOk()
    {
        var summary = new BackupImportSummaryDto(1, 1, 1, 1, 1);
        var data = BuildValidData();
        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<ImportBackupCommand, BackupImportSummaryDto>(
                It.IsAny<ImportBackupCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.ImportAsync(data, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result.Result!).Value.Should().BeEquivalentTo(summary);
    }

    [Fact]
    public async Task ImportAsync_WithInvalidData_ShouldPropagateValidationException()
    {
        var data = BuildValidData();
        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<ImportBackupCommand, BackupImportSummaryDto>(
                It.IsAny<ImportBackupCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("invalid backup"));

        var action = () => _controller.ImportAsync(data, CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    private static BackupDataDto BuildValidData()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        return new BackupDataDto(
            [new UserBackupDto(userId, "Admin", "admin@test.com", UserRole.Admin, true, true, "system", now, null, null)],
            [new AccountBackupDto(accountId, "Conta", AccountType.Corrente, 10m, true, true, "system", now, null, null)],
            [new CategoryBackupDto(categoryId, "Categoria", CategoryType.Receita, true, false, "system", now, null, null)],
            [
                new TransactionBackupDto(
                    Guid.NewGuid(),
                    accountId,
                    categoryId,
                    TransactionType.Credit,
                    10m,
                    "Movimento",
                    now.Date,
                    null,
                    TransactionStatus.Pending,
                    false,
                    null,
                    false,
                    null,
                    null,
                    null,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "system",
                    now,
                    null,
                    null)
            ],
            [
                new RecurrenceTemplateBackupDto(
                    Guid.NewGuid(),
                    accountId,
                    categoryId,
                    TransactionType.Credit,
                    10m,
                    "Template",
                    1,
                    true,
                    null,
                    TransactionStatus.Pending,
                    "system",
                    now,
                    null,
                    null)
            ]);
    }
}
