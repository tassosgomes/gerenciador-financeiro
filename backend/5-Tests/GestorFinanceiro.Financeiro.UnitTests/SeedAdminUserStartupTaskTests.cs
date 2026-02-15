using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Services;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;
using GestorFinanceiro.Financeiro.Domain.Interface;
using GestorFinanceiro.Financeiro.Infra.StartupTasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestorFinanceiro.Financeiro.UnitTests;

public class SeedAdminUserStartupTaskTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SeedAdminUserStartupTask>> _loggerMock;

    public SeedAdminUserStartupTaskTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<SeedAdminUserStartupTask>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersExist_ShouldCreateAdminWithDefaultValues()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Email"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Password"]).Returns((string?)null);

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u =>
                    u.Name == "Administrador" &&
                    u.Email == "admin@GestorFinanceiro.local" &&
                    u.Role == UserRole.Admin &&
                    u.MustChangePassword == true),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersExist_ShouldHashPasswordCorrectly()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedPassword = "mudar123";
        var expectedHash = "hashed_mudar123";

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Email"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Password"]).Returns((string?)null);

        _passwordHasherMock
            .Setup(p => p.Hash(expectedPassword))
            .Returns(expectedHash);

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _passwordHasherMock.Verify(p => p.Hash(expectedPassword), Times.Once);
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.PasswordHash == expectedHash),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigurationProvided_ShouldUseConfiguredValues()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var configuredName = "Custom Admin";
        var configuredEmail = "custom@example.com";
        var configuredPassword = "CustomPass123!";

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"]).Returns(configuredName);
        _configurationMock.Setup(c => c["AdminSeed:Email"]).Returns(configuredEmail);
        _configurationMock.Setup(c => c["AdminSeed:Password"]).Returns(configuredPassword);

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u =>
                    u.Name == configuredName &&
                    u.Email == configuredEmail),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _passwordHasherMock.Verify(p => p.Hash(configuredPassword), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsersExist_ShouldNotCreateAdmin()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var existingUser = User.Create(
            "Existing User",
            "existing@example.com",
            "hashed_password",
            UserRole.Member,
            "system");

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleUsersExist_ShouldNotCreateAdmin()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var user1 = User.Create("User 1", "user1@example.com", "hash1", UserRole.Member, "system");
        var user2 = User.Create("User 2", "user2@example.com", "hash2", UserRole.Admin, "system");

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user1, user2 });

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database error");

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Email"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Password"]).Returns((string?)null);

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var sut = CreateSut();

        // Act & Assert
        var action = () => sut.ExecuteAsync(cancellationToken);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldHonorCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut();

        // Act & Assert
        var action = () => sut.ExecuteAsync(cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData("", "admin@GestorFinanceiro.local", "mudar123")]
    [InlineData("Administrador", "", "mudar123")]
    [InlineData("Administrador", "admin@GestorFinanceiro.local", "")]
    public async Task ExecuteAsync_WhenConfigurationIsEmptyString_ShouldUseDefaultValues(
        string configName,
        string configEmail,
        string configPassword)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"])
            .Returns(string.IsNullOrEmpty(configName) ? null : configName);
        _configurationMock.Setup(c => c["AdminSeed:Email"])
            .Returns(string.IsNullOrEmpty(configEmail) ? null : configEmail);
        _configurationMock.Setup(c => c["AdminSeed:Password"])
            .Returns(string.IsNullOrEmpty(configPassword) ? null : configPassword);

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u =>
                    u.Name == "Administrador" &&
                    u.Email == "admin@GestorFinanceiro.local"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _passwordHasherMock.Verify(p => p.Hash("mudar123"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSeedingAdmin_ShouldCreateWithSystemAsCreatedBy()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _userRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _configurationMock.Setup(c => c["AdminSeed:Name"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Email"]).Returns((string?)null);
        _configurationMock.Setup(c => c["AdminSeed:Password"]).Returns((string?)null);

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        var sut = CreateSut();

        // Act
        await sut.ExecuteAsync(cancellationToken);

        // Assert
        _userRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<User>(u => u.CreatedBy == "system"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private SeedAdminUserStartupTask CreateSut()
    {
        return new SeedAdminUserStartupTask(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }
}
