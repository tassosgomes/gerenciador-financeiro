using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Domain.Entity;
using Mapster;

namespace GestorFinanceiro.Financeiro.Application.Mapping;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        TypeAdapterConfig<Account, AccountResponse>.NewConfig();
        TypeAdapterConfig<Transaction, TransactionResponse>.NewConfig();
        TypeAdapterConfig<Category, CategoryResponse>.NewConfig();
        TypeAdapterConfig<RecurrenceTemplate, RecurrenceTemplateResponse>.NewConfig();
        TypeAdapterConfig<AuditLog, AuditLogDto>.NewConfig();
        TypeAdapterConfig<User, UserResponse>
            .NewConfig()
            .Map(destination => destination.Role, source => source.Role.ToString());

        TypeAdapterConfig<User, UserBackupDto>.NewConfig();
        TypeAdapterConfig<Account, AccountBackupDto>.NewConfig();
        TypeAdapterConfig<Category, CategoryBackupDto>.NewConfig();
        TypeAdapterConfig<Transaction, TransactionBackupDto>.NewConfig();
        TypeAdapterConfig<RecurrenceTemplate, RecurrenceTemplateBackupDto>.NewConfig();

        TypeAdapterConfig<UserBackupDto, User>
            .NewConfig()
            .ConstructUsing(source => User.Restore(
                source.Id,
                source.Name,
                source.Email,
                source.Role,
                source.IsActive,
                true,
                string.Empty,
                source.CreatedBy,
                source.CreatedAt,
                source.UpdatedBy,
                source.UpdatedAt));

        TypeAdapterConfig<AccountBackupDto, Account>
            .NewConfig()
            .ConstructUsing(source => Account.Restore(
                source.Id,
                source.Name,
                source.Type,
                source.Balance,
                source.AllowNegativeBalance,
                source.IsActive,
                source.CreatedBy,
                source.CreatedAt,
                source.UpdatedBy,
                source.UpdatedAt));

        TypeAdapterConfig<CategoryBackupDto, Category>
            .NewConfig()
            .ConstructUsing(source => Category.Restore(
                source.Id,
                source.Name,
                source.Type,
                source.IsActive,
                source.CreatedBy,
                source.CreatedAt,
                source.UpdatedBy,
                source.UpdatedAt));

        TypeAdapterConfig<RecurrenceTemplateBackupDto, RecurrenceTemplate>
            .NewConfig()
            .ConstructUsing(source => RecurrenceTemplate.Restore(
                source.Id,
                source.AccountId,
                source.CategoryId,
                source.Type,
                source.Amount,
                source.Description,
                source.DayOfMonth,
                source.IsActive,
                source.LastGeneratedDate,
                source.DefaultStatus,
                source.CreatedBy,
                source.CreatedAt,
                source.UpdatedBy,
                source.UpdatedAt));

        TypeAdapterConfig<TransactionBackupDto, Transaction>
            .NewConfig()
            .ConstructUsing(source => Transaction.Restore(
                source.Id,
                source.AccountId,
                source.CategoryId,
                source.Type,
                source.Amount,
                source.Description,
                source.CompetenceDate,
                source.DueDate,
                source.Status,
                source.IsAdjustment,
                source.OriginalTransactionId,
                source.HasAdjustment,
                source.InstallmentGroupId,
                source.InstallmentNumber,
                source.TotalInstallments,
                source.IsRecurrent,
                source.RecurrenceTemplateId,
                source.TransferGroupId,
                source.CancellationReason,
                source.CancelledBy,
                source.CancelledAt,
                source.OperationId,
                source.CreatedBy,
                source.CreatedAt,
                source.UpdatedBy,
                source.UpdatedAt));
    }
}
