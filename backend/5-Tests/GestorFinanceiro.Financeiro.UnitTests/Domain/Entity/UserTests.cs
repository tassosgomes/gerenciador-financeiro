using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.Domain.Enum;

namespace GestorFinanceiro.Financeiro.UnitTests.Domain.Entity;

public class UserTests
{
    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;

        var user = User.Create("Admin User", "admin@familia.com", "hashed-password", UserRole.Admin, "system");

        var after = DateTime.UtcNow;
        user.Name.Should().Be("Admin User");
        user.Email.Should().Be("admin@familia.com");
        user.PasswordHash.Should().Be("hashed-password");
        user.Role.Should().Be(UserRole.Admin);
        user.IsActive.Should().BeTrue();
        user.MustChangePassword.Should().BeTrue();
        user.CreatedBy.Should().Be("system");
        user.CreatedAt.Should().BeOnOrAfter(before);
        user.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithMemberRole_SetsRoleCorrectly()
    {
        var user = User.Create("Member User", "member@familia.com", "hashed-password", UserRole.Member, "admin-1");

        user.Role.Should().Be(UserRole.Member);
        user.CreatedBy.Should().Be("admin-1");
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var action = () => User.Create("", "user@test.com", "hash", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsArgumentException()
    {
        var action = () => User.Create("   ", "user@test.com", "hash", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithShortName_ThrowsArgumentException()
    {
        var action = () => User.Create("AB", "user@test.com", "hash", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ThrowsArgumentException()
    {
        var longName = new string('A', 151);

        var action = () => User.Create(longName, "user@test.com", "hash", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyEmail_ThrowsArgumentException()
    {
        var action = () => User.Create("Valid Name", "", "hash", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ThrowsArgumentException()
    {
        var action = () => User.Create("Valid Name", "user@test.com", "", UserRole.Member, "system");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameAtMinLength_Succeeds()
    {
        var user = User.Create("Abc", "user@test.com", "hash", UserRole.Member, "system");

        user.Name.Should().Be("Abc");
    }

    [Fact]
    public void Create_WithNameAtMaxLength_Succeeds()
    {
        var name = new string('A', 150);

        var user = User.Create(name, "user@test.com", "hash", UserRole.Member, "system");

        user.Name.Should().Be(name);
    }

    [Fact]
    public void Deactivate_ActiveUser_SetsIsActiveToFalse()
    {
        var user = User.Create("User Test", "user@test.com", "hash", UserRole.Member, "system");

        user.Deactivate("admin-1");

        user.IsActive.Should().BeFalse();
        user.UpdatedBy.Should().Be("admin-1");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_InactiveUser_SetsIsActiveToTrue()
    {
        var user = User.Create("User Test", "user@test.com", "hash", UserRole.Member, "system");
        user.Deactivate("admin-1");

        user.Activate("admin-2");

        user.IsActive.Should().BeTrue();
        user.UpdatedBy.Should().Be("admin-2");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangePassword_UpdatesPasswordHashAndSetsMustChangePasswordToFalse()
    {
        var user = User.Create("User Test", "user@test.com", "old-hash", UserRole.Member, "system");
        user.MustChangePassword.Should().BeTrue();

        user.ChangePassword("new-hash", "user-1");

        user.PasswordHash.Should().Be("new-hash");
        user.MustChangePassword.Should().BeFalse();
        user.UpdatedBy.Should().Be("user-1");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var user1 = User.Create("User One", "user1@test.com", "hash", UserRole.Member, "system");
        var user2 = User.Create("User Two", "user2@test.com", "hash", UserRole.Member, "system");

        user1.Id.Should().NotBe(user2.Id);
        user1.Id.Should().NotBe(Guid.Empty);
    }
}
