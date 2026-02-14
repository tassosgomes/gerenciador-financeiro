using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.User;

public class ToggleUserStatusCommandHandler : ICommandHandler<ToggleUserStatusCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleUserStatusCommandHandler> _logger;

    public ToggleUserStatusCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<ToggleUserStatusCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ToggleUserStatusCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating status for user {UserId} to active={IsActive}", command.UserId, command.IsActive);

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var previousData = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.MustChangePassword,
            };

            if (command.IsActive)
            {
                user.Activate(command.UpdatedByUserId);
            }
            else
            {
                user.Deactivate(command.UpdatedByUserId);
                await _refreshTokenRepository.RevokeByUserIdAsync(command.UserId, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var action = command.IsActive ? "Updated" : "Deactivated";
            await _auditService.LogAsync("User", user.Id, action, command.UpdatedByUserId, previousData, cancellationToken);
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
