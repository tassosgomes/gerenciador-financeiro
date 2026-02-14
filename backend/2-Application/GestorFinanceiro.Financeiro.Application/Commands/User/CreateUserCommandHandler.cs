using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Commands.User;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IAuditService auditService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<CreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserResponse> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validationResult = new CreateUserCommandValidator().Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(error => error.ErrorMessage))}");
        }

        _logger.LogInformation("Creating user with email {Email}", command.Email);

        var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(command.Email, cancellationToken);
        if (emailAlreadyExists)
        {
            throw new UserEmailAlreadyExistsException(command.Email);
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var role = Enum.Parse<UserRole>(command.Role, true);

        var user = GestorFinanceiro.Financeiro.Domain.Entity.User.Create(
            command.Name,
            command.Email,
            passwordHash,
            role,
            command.CreatedByUserId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync("User", user.Id, "Created", command.CreatedByUserId, null, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return ToUserResponse(user);
    }

    private static UserResponse ToUserResponse(GestorFinanceiro.Financeiro.Domain.Entity.User user)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString(),
            user.IsActive,
            user.MustChangePassword,
            user.CreatedAt);
    }
}
