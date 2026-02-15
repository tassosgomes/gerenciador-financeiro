using FluentValidation;
using FluentValidation.Results;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos.Backup;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Mapster;
using DomainAccount = GestorFinanceiro.Financeiro.Domain.Entity.Account;
using DomainCategory = GestorFinanceiro.Financeiro.Domain.Entity.Category;
using DomainRecurrenceTemplate = GestorFinanceiro.Financeiro.Domain.Entity.RecurrenceTemplate;
using DomainTransaction = GestorFinanceiro.Financeiro.Domain.Entity.Transaction;
using DomainUser = GestorFinanceiro.Financeiro.Domain.Entity.User;

namespace GestorFinanceiro.Financeiro.Application.Commands.Backup;

public class ImportBackupCommandHandler : ICommandHandler<ImportBackupCommand, BackupImportSummaryDto>
{
    private readonly IBackupRepository _backupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<ImportBackupCommand> _validator;
    private readonly IBackupIntegrityValidator _backupIntegrityValidator;

    public ImportBackupCommandHandler(
        IBackupRepository backupRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IValidator<ImportBackupCommand> validator,
        IBackupIntegrityValidator backupIntegrityValidator)
    {
        _backupRepository = backupRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _validator = validator;
        _backupIntegrityValidator = backupIntegrityValidator;
    }

    public async Task<BackupImportSummaryDto> HandleAsync(ImportBackupCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var integrityErrors = _backupIntegrityValidator.Validate(command.Data);
        if (integrityErrors.Count > 0)
        {
            var failures = integrityErrors
                .Select(error => new ValidationFailure(nameof(command.Data), error))
                .ToList();

            throw new ValidationException(failures);
        }

        var users = command.Data.Users.Adapt<List<DomainUser>>();
        var accounts = command.Data.Accounts.Adapt<List<DomainAccount>>();
        var categories = command.Data.Categories.Adapt<List<DomainCategory>>();
        var recurrenceTemplates = command.Data.RecurrenceTemplates.Adapt<List<DomainRecurrenceTemplate>>();
        var transactions = command.Data.Transactions.Adapt<List<DomainTransaction>>();

        foreach (var user in users)
        {
            var temporaryPassword = $"Temp-{Guid.NewGuid():N}";
            user.ApplyImportedPasswordHash(_passwordHasher.Hash(temporaryPassword));
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _backupRepository.TruncateAllAsync(cancellationToken);
            await _backupRepository.ImportAsync(users, accounts, categories, recurrenceTemplates, transactions, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new BackupImportSummaryDto(
                users.Count,
                accounts.Count,
                categories.Count,
                recurrenceTemplates.Count,
                transactions.Count);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
