using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Mapping;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Application.Queries.Category;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GestorFinanceiro.Financeiro.Application.Common;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();

        // Configure mappings
        MappingConfig.ConfigureMappings();

        // Register all command handlers
        services.AddScoped<ICommandHandler<CreateAccountCommand, AccountResponse>, CreateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountCommand, AccountResponse>, UpdateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateAccountCommand, Unit>, DeactivateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateAccountCommand, Unit>, ActivateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCategoryCommand, CategoryResponse>, CreateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateCategoryCommand, CategoryResponse>, UpdateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTransactionCommand, TransactionResponse>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<AdjustTransactionCommand, TransactionResponse>, AdjustTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<CancelTransactionCommand, TransactionResponse>, CancelTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<CreateInstallmentCommand, IReadOnlyList<TransactionResponse>>, CreateInstallmentCommandHandler>();
        services.AddScoped<ICommandHandler<AdjustInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>, AdjustInstallmentGroupCommandHandler>();
        services.AddScoped<ICommandHandler<CancelInstallmentCommand, Unit>, CancelInstallmentCommandHandler>();
        services.AddScoped<ICommandHandler<CancelInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>, CancelInstallmentGroupCommandHandler>();
        services.AddScoped<ICommandHandler<CreateRecurrenceCommand, RecurrenceTemplateResponse>, CreateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateRecurrenceCommand, Unit>, DeactivateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<GenerateRecurrenceCommand, Unit>, GenerateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTransferCommand, IReadOnlyList<TransactionResponse>>, CreateTransferCommandHandler>();
        services.AddScoped<ICommandHandler<CancelTransferCommand, Unit>, CancelTransferCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, Unit>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, Unit>, ChangePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<CreateUserCommand, UserResponse>, CreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<ToggleUserStatusCommand, Unit>, ToggleUserStatusCommandHandler>();

        services.AddScoped<CreateTransactionValidator>();

        // Register all query handlers
        services.AddScoped<IQueryHandler<GetAccountByIdQuery, AccountResponse>, GetAccountByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountResponse>>, ListAccountsQueryHandler>();
        services.AddScoped<IQueryHandler<GetTransactionByIdQuery, TransactionResponse>, GetTransactionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsByAccountQuery, IReadOnlyList<TransactionResponse>>, ListTransactionsByAccountQueryHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsQuery, PagedResult<TransactionResponse>>, ListTransactionsQueryHandler>();
        services.AddScoped<IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>, ListCategoriesQueryHandler>();
        services.AddScoped<IQueryHandler<GetAllUsersQuery, IEnumerable<UserResponse>>, GetAllUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByIdQuery, UserResponse>, GetUserByIdQueryHandler>();

        // Register validators
        services.AddScoped<LoginCommandValidator>();
        services.AddScoped<RefreshTokenCommandValidator>();
        services.AddScoped<ChangePasswordCommandValidator>();
        services.AddScoped<CreateUserCommandValidator>();
        services.AddScoped<IValidator<ListTransactionsQuery>, ListTransactionsQueryValidator>();

        return services;
    }
}
