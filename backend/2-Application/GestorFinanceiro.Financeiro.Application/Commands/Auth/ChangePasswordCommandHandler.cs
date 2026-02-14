using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.Auth;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditService auditService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var validationResult = new ChangePasswordCommandValidator().Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(error => error.ErrorMessage))}");
        }

        _logger.LogInformation("Changing password for user {UserId}", command.UserId);

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(command.UserId);
        }

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var previousData = new
            {
                user.Id,
                user.Email,
                user.MustChangePassword,
            };

            var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
            user.ChangePassword(newPasswordHash, command.UserId.ToString());

            await _refreshTokenRepository.RevokeByUserIdAsync(command.UserId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("User", user.Id, "Updated", command.UserId.ToString(), previousData, cancellationToken);
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
