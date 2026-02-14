using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Application.Queries.User;

public class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IEnumerable<UserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(
        IUserRepository userRepository,
        ILogger<GetAllUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<UserResponse>> HandleAsync(GetAllUsersQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing all users");

        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(ToUserResponse);
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
