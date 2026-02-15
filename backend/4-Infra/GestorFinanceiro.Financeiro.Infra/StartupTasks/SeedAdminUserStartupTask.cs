using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestorFinanceiro.Financeiro.Infra.StartupTasks;

public sealed class SeedAdminUserStartupTask : IStartupTask
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedAdminUserStartupTask> _logger;

    public SeedAdminUserStartupTask(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<SeedAdminUserStartupTask> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking if admin user seeding is required");

        var existingUsers = await _userRepository.GetAllAsync(cancellationToken);
        if (existingUsers.Any())
        {
            _logger.LogInformation("Users already exist in database. Skipping admin seed");
            return;
        }

        var adminName = _configuration["AdminSeed:Name"] ?? "Administrador";
        var adminEmail = _configuration["AdminSeed:Email"] ?? "admin@GestorFinanceiro.local";
        var adminPassword = _configuration["AdminSeed:Password"] ?? "mudar123";

        _logger.LogInformation("Seeding default admin user with email {AdminEmail}", adminEmail);
        _logger.LogWarning(
            "Default admin credentials are being used. Please change the password after first login. " +
            "Configure AdminSeed:Email and AdminSeed:Password environment variables for production");

        try
        {
            var adminUser = User.Create(
                adminName,
                adminEmail,
                _passwordHasher.Hash(adminPassword),
                UserRole.Admin,
                "system");

            await _userRepository.AddAsync(adminUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default admin user seeded successfully with email {AdminEmail}", adminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed default admin user");
            throw;
        }
    }
}
