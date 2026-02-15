using FluentValidation;
using GestorFinanceiro.Financeiro.Domain.Exception;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestorFinanceiro.Financeiro.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (problemDetails, isUnexpectedError) = MapException(httpContext, exception);

        if (isUnexpectedError)
        {
            _logger.LogError(exception, "Unhandled exception processing request {RequestPath}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception processing request {RequestPath}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        if (problemDetails is ValidationProblemDetails validationProblemDetails)
        {
            await httpContext.Response.WriteAsJsonAsync(validationProblemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        }

        return true;
    }

    private (ProblemDetails ProblemDetails, bool IsUnexpectedError) MapException(HttpContext httpContext, Exception exception)
    {
        return exception switch
        {
            ValidationException validationException =>
                (CreateValidationProblemDetails(httpContext, validationException), false),

            InvalidCredentialsException invalidCredentialsException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status401Unauthorized, "Credenciais inválidas", invalidCredentialsException.Message, "https://httpstatuses.com/401"), false),

            InactiveUserException inactiveUserException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status401Unauthorized, "Usuário inativo", inactiveUserException.Message, "https://httpstatuses.com/401"), false),

            InvalidRefreshTokenException invalidRefreshTokenException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status401Unauthorized, "Token inválido", invalidRefreshTokenException.Message, "https://httpstatuses.com/401"), false),

            UnauthorizedAccessException unauthorizedAccessException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status403Forbidden, "Acesso negado", unauthorizedAccessException.Message, "https://httpstatuses.com/403"), false),

            AccountNotFoundException accountNotFoundException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Conta não encontrada", accountNotFoundException.Message, "https://httpstatuses.com/404"), false),

            CategoryNotFoundException categoryNotFoundException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Categoria não encontrada", categoryNotFoundException.Message, "https://httpstatuses.com/404"), false),

            TransactionNotFoundException transactionNotFoundException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Transação não encontrada", transactionNotFoundException.Message, "https://httpstatuses.com/404"), false),

            UserNotFoundException userNotFoundException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Usuário não encontrado", userNotFoundException.Message, "https://httpstatuses.com/404"), false),

            RecurrenceTemplateNotFoundException recurrenceTemplateNotFoundException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status404NotFound, "Template de recorrência não encontrado", recurrenceTemplateNotFoundException.Message, "https://httpstatuses.com/404"), false),

            AccountNameAlreadyExistsException accountNameAlreadyExistsException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Nome de conta já existe", accountNameAlreadyExistsException.Message, "https://httpstatuses.com/400"), false),

            CategoryNameAlreadyExistsException categoryNameAlreadyExistsException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Nome de categoria já existe", categoryNameAlreadyExistsException.Message, "https://httpstatuses.com/400"), false),

            InsufficientBalanceException insufficientBalanceException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Saldo insuficiente", insufficientBalanceException.Message, "https://httpstatuses.com/400"), false),

            InvalidTransactionAmountException invalidTransactionAmountException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Valor inválido", invalidTransactionAmountException.Message, "https://httpstatuses.com/400"), false),

            UserEmailAlreadyExistsException userEmailAlreadyExistsException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Email já cadastrado", userEmailAlreadyExistsException.Message, "https://httpstatuses.com/400"), false),

            DomainException domainException =>
                (CreateProblemDetails(httpContext, StatusCodes.Status400BadRequest, "Erro de validação", domainException.Message, "https://httpstatuses.com/400"), false),

            _ =>
                (CreateProblemDetails(
                    httpContext,
                    StatusCodes.Status500InternalServerError,
                    "Erro interno",
                    _hostEnvironment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    "https://httpstatuses.com/500"), true)
        };
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, int statusCode, string title, string detail, string type)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path
        };
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/400",
            Instance = httpContext.Request.Path
        };
    }
}
