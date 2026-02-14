using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Exception;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;

namespace GestorFinanceiro.Financeiro.Application.Commands.Auth;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var validationResult = new RefreshTokenCommandValidator().Validate(command);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(error => error.ErrorMessage))}");
        }

        _logger.LogInformation("Refreshing access token");

        var tokenHash = GestorFinanceiro.Financeiro.Domain.Entity.RefreshToken.ComputeHash(command.RefreshToken);
        var currentRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (currentRefreshToken is null || !currentRefreshToken.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(currentRefreshToken.UserId);
        }

        if (!user.IsActive)
        {
            throw new InactiveUserException(user.Id);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            currentRefreshToken.Revoke();

            var accessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new AuthResponse(
                accessToken,
                newRefreshToken.Token,
                ExtractExpiresInSeconds(accessToken),
                ToUserResponse(user));
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
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

    private static int ExtractExpiresInSeconds(string accessToken)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var expiresAt = token.ValidTo == DateTime.MinValue
            ? DateTime.UtcNow
            : token.ValidTo;

        return Math.Max(0, (int)(expiresAt - DateTime.UtcNow).TotalSeconds);
    }
}
