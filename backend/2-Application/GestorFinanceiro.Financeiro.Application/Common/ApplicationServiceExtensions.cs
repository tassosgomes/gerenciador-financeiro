using GestorFinanceiro.Financeiro.Application.Commands.Account;
using GestorFinanceiro.Financeiro.Application.Commands.Auth;
using GestorFinanceiro.Financeiro.Application.Commands.Backup;
using GestorFinanceiro.Financeiro.Application.Commands.Budget;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Commands.Installment;
using GestorFinanceiro.Financeiro.Application.Commands.Invoice;
using GestorFinanceiro.Financeiro.Application.Commands.Recurrence;
using GestorFinanceiro.Financeiro.Application.Commands.Receipt;
using GestorFinanceiro.Financeiro.Application.Commands.System;
using GestorFinanceiro.Financeiro.Application.Commands.Transaction;
using GestorFinanceiro.Financeiro.Application.Commands.Transfer;
using GestorFinanceiro.Financeiro.Application.Commands.User;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Application.Mapping;
using GestorFinanceiro.Financeiro.Application.Queries.Account;
using GestorFinanceiro.Financeiro.Application.Queries.Audit;
using GestorFinanceiro.Financeiro.Application.Queries.Backup;
using GestorFinanceiro.Financeiro.Application.Queries.Budget;
using GestorFinanceiro.Financeiro.Application.Queries.Category;
using GestorFinanceiro.Financeiro.Application.Queries.Dashboard;
using GestorFinanceiro.Financeiro.Application.Queries.Invoice;
using GestorFinanceiro.Financeiro.Application.Queries.Receipt;
using GestorFinanceiro.Financeiro.Application.Queries.Transaction;
using GestorFinanceiro.Financeiro.Application.Queries.User;
using GestorFinanceiro.Financeiro.Domain.Service;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GestorFinanceiro.Financeiro.Application.Common;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<IBackupIntegrityValidator, BackupIntegrityValidator>();
        services.AddScoped<CreditCardDomainService>();
        services.AddScoped<TransactionDomainService>();
        services.AddScoped<InstallmentDomainService>();
        services.AddScoped<RecurrenceDomainService>();
        services.AddScoped<TransferDomainService>();
        services.AddScoped<BudgetDomainService>();

        // Configure mappings
        MappingConfig.ConfigureMappings();

        // Register all command handlers
        services.AddScoped<ICommandHandler<CreateAccountCommand, AccountResponse>, CreateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountCommand, AccountResponse>, UpdateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateAccountCommand, Unit>, DeactivateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateAccountCommand, Unit>, ActivateAccountCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCategoryCommand, CategoryResponse>, CreateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateCategoryCommand, CategoryResponse>, UpdateCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCategoryCommand, Unit>, DeleteCategoryCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTransactionCommand, TransactionResponse>, CreateTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<AdjustTransactionCommand, TransactionResponse>, AdjustTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<CancelTransactionCommand, TransactionResponse>, CancelTransactionCommandHandler>();
        services.AddScoped<ICommandHandler<ImportNfceCommand, ImportNfceResponse>, ImportNfceCommandHandler>();
        services.AddScoped<ICommandHandler<MarkTransactionAsPaidCommand, TransactionResponse>, MarkTransactionAsPaidCommandHandler>();
        services.AddScoped<ICommandHandler<CreateInstallmentCommand, IReadOnlyList<TransactionResponse>>, CreateInstallmentCommandHandler>();
        services.AddScoped<ICommandHandler<AdjustInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>, AdjustInstallmentGroupCommandHandler>();
        services.AddScoped<ICommandHandler<CancelInstallmentCommand, Unit>, CancelInstallmentCommandHandler>();
        services.AddScoped<ICommandHandler<CancelInstallmentGroupCommand, IReadOnlyList<TransactionResponse>>, CancelInstallmentGroupCommandHandler>();
        services.AddScoped<ICommandHandler<CreateRecurrenceCommand, RecurrenceTemplateResponse>, CreateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateRecurrenceCommand, Unit>, DeactivateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<GenerateRecurrenceCommand, Unit>, GenerateRecurrenceCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTransferCommand, IReadOnlyList<TransactionResponse>>, CreateTransferCommandHandler>();
        services.AddScoped<ICommandHandler<CancelTransferCommand, Unit>, CancelTransferCommandHandler>();
        services.AddScoped<ICommandHandler<PayInvoiceCommand, IReadOnlyList<TransactionResponse>>, PayInvoiceCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, Unit>, LogoutCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, Unit>, ChangePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<CreateUserCommand, UserResponse>, CreateUserCommandHandler>();
        services.AddScoped<ICommandHandler<ToggleUserStatusCommand, Unit>, ToggleUserStatusCommandHandler>();
        services.AddScoped<ICommandHandler<ImportBackupCommand, BackupImportSummaryDto>, ImportBackupCommandHandler>();
        services.AddScoped<ICommandHandler<ResetSystemCommand, Unit>, ResetSystemCommandHandler>();
        services.AddScoped<ICommandHandler<CreateBudgetCommand, BudgetResponse>, CreateBudgetCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateBudgetCommand, BudgetResponse>, UpdateBudgetCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteBudgetCommand, Unit>, DeleteBudgetCommandHandler>();

        services.AddScoped<CreateTransactionValidator>();

        // Register all query handlers
        services.AddScoped<IQueryHandler<GetAccountByIdQuery, AccountResponse>, GetAccountByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListAccountsQuery, IReadOnlyList<AccountResponse>>, ListAccountsQueryHandler>();
        services.AddScoped<IQueryHandler<GetTransactionByIdQuery, TransactionResponse>, GetTransactionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetTransactionHistoryQuery, TransactionHistoryResponse>, GetTransactionHistoryQueryHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsByAccountQuery, IReadOnlyList<TransactionResponse>>, ListTransactionsByAccountQueryHandler>();
        services.AddScoped<IQueryHandler<ListTransactionsQuery, PagedResult<TransactionResponse>>, ListTransactionsQueryHandler>();
        services.AddScoped<IQueryHandler<ListAuditLogsQuery, PagedResult<AuditLogDto>>, ListAuditLogsQueryHandler>();
        services.AddScoped<IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>, ListCategoriesQueryHandler>();
        services.AddScoped<IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>, GetDashboardSummaryQueryHandler>();
        services.AddScoped<IQueryHandler<GetDashboardChartsQuery, DashboardChartsResponse>, GetDashboardChartsQueryHandler>();
        services.AddScoped<IQueryHandler<GetInvoiceQuery, InvoiceResponse>, GetInvoiceQueryHandler>();
        services.AddScoped<IQueryHandler<LookupNfceQuery, NfceLookupResponse>, LookupNfceQueryHandler>();
        services.AddScoped<IQueryHandler<GetReceiptItemsByTransactionIdQuery, TransactionReceiptResponse>, GetReceiptItemsByTransactionIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetAllUsersQuery, IEnumerable<UserResponse>>, GetAllUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByIdQuery, UserResponse>, GetUserByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ExportBackupQuery, BackupExportDto>, ExportBackupQueryHandler>();
        services.AddScoped<IQueryHandler<ListBudgetsQuery, IReadOnlyList<BudgetResponse>>, ListBudgetsQueryHandler>();
        services.AddScoped<IQueryHandler<GetBudgetByIdQuery, BudgetResponse>, GetBudgetByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetBudgetSummaryQuery, BudgetSummaryResponse>, GetBudgetSummaryQueryHandler>();
        services.AddScoped<IQueryHandler<GetAvailablePercentageQuery, AvailablePercentageResponse>, GetAvailablePercentageQueryHandler>();

        // Register validators
        services.AddScoped<LoginCommandValidator>();
        services.AddScoped<RefreshTokenCommandValidator>();
        services.AddScoped<ChangePasswordCommandValidator>();
        services.AddScoped<CreateUserCommandValidator>();
        services.AddScoped<PayInvoiceCommandValidator>();
        services.AddScoped<IValidator<ImportBackupCommand>, ImportBackupValidator>();
        services.AddScoped<IValidator<LookupNfceQuery>, LookupNfceQueryValidator>();
        services.AddScoped<IValidator<ImportNfceCommand>, ImportNfceValidator>();
        services.AddScoped<IValidator<ListTransactionsQuery>, ListTransactionsQueryValidator>();
        services.AddScoped<IValidator<GetInvoiceQuery>, GetInvoiceQueryValidator>();
        services.AddScoped<CreateBudgetValidator>();
        services.AddScoped<UpdateBudgetValidator>();

        return services;
    }
}
