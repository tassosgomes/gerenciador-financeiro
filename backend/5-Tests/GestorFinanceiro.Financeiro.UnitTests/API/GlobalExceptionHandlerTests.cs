using System.Text.Json;
using AwesomeAssertions;
using FluentValidation;
using FluentValidation.Results;
using GestorFinanceiro.Financeiro.API.Middleware;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock = new();

    [Fact]
    public async Task TryHandleAsync_WhenAccountNameAlreadyExistsException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new AccountNameAlreadyExistsException("Conta Principal"));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Nome de conta já existe");
        response.Json.RootElement.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WhenCategoryNameAlreadyExistsException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new CategoryNameAlreadyExistsException("Alimentação", CategoryType.Despesa));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Nome de categoria já existe");
    }

    [Fact]
    public async Task TryHandleAsync_WhenInsufficientBalanceException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new InsufficientBalanceException(Guid.NewGuid(), 100m, 200m));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Saldo insuficiente");
    }

    [Fact]
    public async Task TryHandleAsync_WhenInvalidTransactionAmountException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new InvalidTransactionAmountException(-100m));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Valor inválido");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUserEmailAlreadyExistsException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new UserEmailAlreadyExistsException("test@test.com"));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Email já cadastrado");
    }

    [Fact]
    public async Task TryHandleAsync_WhenGenericDomainException_ShouldReturn400()
    {
        var response = await ExecuteAsync(new TestDomainException("Generic domain error"));

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Erro de validação");
    }

    [Fact]
    public async Task TryHandleAsync_WhenInvalidCredentialsException_ShouldReturn401()
    {
        var response = await ExecuteAsync(new InvalidCredentialsException());

        response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Credenciais inválidas");
    }

    [Fact]
    public async Task TryHandleAsync_WhenInactiveUserException_ShouldReturn401()
    {
        var response = await ExecuteAsync(new InactiveUserException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Usuário inativo");
    }

    [Fact]
    public async Task TryHandleAsync_WhenInvalidRefreshTokenException_ShouldReturn401()
    {
        var response = await ExecuteAsync(new InvalidRefreshTokenException());

        response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Token inválido");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUnauthorizedAccessException_ShouldReturn403()
    {
        var response = await ExecuteAsync(new UnauthorizedAccessException("Access denied"));

        response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Acesso negado");
    }

    [Fact]
    public async Task TryHandleAsync_WhenAccountNotFoundException_ShouldReturn404()
    {
        var response = await ExecuteAsync(new AccountNotFoundException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Conta não encontrada");
    }

    [Fact]
    public async Task TryHandleAsync_WhenCategoryNotFoundException_ShouldReturn404()
    {
        var response = await ExecuteAsync(new CategoryNotFoundException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Categoria não encontrada");
    }

    [Fact]
    public async Task TryHandleAsync_WhenTransactionNotFoundException_ShouldReturn404()
    {
        var response = await ExecuteAsync(new TransactionNotFoundException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Transação não encontrada");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUserNotFoundException_ShouldReturn404()
    {
        var response = await ExecuteAsync(new UserNotFoundException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Usuário não encontrado");
    }

    [Fact]
    public async Task TryHandleAsync_WhenRecurrenceTemplateNotFoundException_ShouldReturn404()
    {
        var response = await ExecuteAsync(new RecurrenceTemplateNotFoundException(Guid.NewGuid()));

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Template de recorrência não encontrado");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUnexpectedException_ShouldReturn500WithSafeMessage()
    {
        var response = await ExecuteAsync(new Exception("sensitive details"), Environments.Production);

        response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Erro interno");
        response.Json.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUnexpectedExceptionInDev_ShouldReturn500WithExceptionMessage()
    {
        var response = await ExecuteAsync(new Exception("sensitive details"), Environments.Development);

        response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        response.Json.RootElement.GetProperty("detail").GetString().Should().Be("sensitive details");
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationException_ShouldReturnValidationErrors()
    {
        var exception = new ValidationException(
            [
                new ValidationFailure("Email", "Email is required"),
                new ValidationFailure("Email", "Email format is invalid"),
                new ValidationFailure("Name", "Name is required")
            ]);

        var response = await ExecuteAsync(exception);

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        response.Json.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        response.Json.RootElement.GetProperty("errors").GetProperty("Email").GetArrayLength().Should().Be(2);
        response.Json.RootElement.GetProperty("errors").GetProperty("Name").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task TryHandleAsync_ProblemDetailsIncludesInstancePath()
    {
        var response = await ExecuteAsync(new TestDomainException("test"));

        response.Json.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/test");
    }

    private async Task<(int StatusCode, JsonDocument Json)> ExecuteAsync(Exception exception, string environment = "Development")
    {
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = environment };
        var handler = new GlobalExceptionHandler(_loggerMock.Object, hostEnvironment);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/test";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(httpContext.Response.Body);

        return (httpContext.Response.StatusCode, json);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "GestorFinanceiro.Financeiro.API";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string message) : base(message) { }
    }
}
