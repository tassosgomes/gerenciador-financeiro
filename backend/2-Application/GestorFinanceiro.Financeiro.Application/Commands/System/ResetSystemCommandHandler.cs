using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Interface;

namespace GestorFinanceiro.Financeiro.Application.Commands.System;

public class ResetSystemCommandHandler : ICommandHandler<ResetSystemCommand, Unit>
{
    private readonly ISystemRepository _systemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetSystemCommandHandler(
        ISystemRepository systemRepository,
        IUnitOfWork unitOfWork)
    {
        _systemRepository = systemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(ResetSystemCommand command, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _systemRepository.ResetSystemDataAsync(cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Unit.Value;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
