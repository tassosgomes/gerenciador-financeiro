using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Infra.Auth;

namespace GestorFinanceiro.Financeiro.UnitTests.Infra.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_SenhaValida_GeraHashNaoVazio()
    {
        var hash = _sut.Hash("password123");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_MesmaSenha_GeraHashesDiferentes()
    {
        var hash1 = _sut.Hash("password123");
        var hash2 = _sut.Hash("password123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_SenhaCorreta_RetornaTrue()
    {
        var hash = _sut.Hash("password123");

        var result = _sut.Verify("password123", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_SenhaIncorreta_RetornaFalse()
    {
        var hash = _sut.Hash("password123");

        var result = _sut.Verify("senha-errada", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_SenhaNula_LancaArgumentException()
    {
        var action = () => _sut.Hash(null!);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Hash_SenhaVazia_LancaArgumentException()
    {
        var action = () => _sut.Hash("");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_SenhaNula_LancaArgumentException()
    {
        var action = () => _sut.Verify(null!, "hash");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_HashNulo_LancaArgumentException()
    {
        var action = () => _sut.Verify("password", null!);

        action.Should().Throw<ArgumentException>();
    }
}
