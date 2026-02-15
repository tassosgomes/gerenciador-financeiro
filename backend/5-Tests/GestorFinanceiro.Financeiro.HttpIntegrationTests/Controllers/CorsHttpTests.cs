using System.Net;
using System.Net.Http.Headers;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public sealed class CorsHttpTests : IntegrationTestBase
{
    public CorsHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task PreflightRequest_WithValidOrigin_ShouldReturnCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/accounts");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization,Content-Type");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var allowOriginHeader = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        allowOriginHeader.Should().Be("http://localhost:5173");
        
        var allowMethodsHeader = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault();
        allowMethodsHeader.Should().NotBeNullOrEmpty();
        
        var allowHeadersHeader = response.Headers.GetValues("Access-Control-Allow-Headers").FirstOrDefault();
        allowHeadersHeader.Should().Contain("Authorization");
        allowHeadersHeader.Should().Contain("Content-Type");
        
        var allowCredentialsHeader = response.Headers.GetValues("Access-Control-Allow-Credentials").FirstOrDefault();
        allowCredentialsHeader.Should().Be("true");
    }

    [DockerAvailableFact]
    public async Task SimpleRequest_WithValidOrigin_ShouldReturnCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Content = new StringContent(
            """{"email":"admin@test.com","password":"Admin@123"}""",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // Response pode ser 400 (validacao) ou 401 (credenciais), mas deve ter headers CORS
        var allowOriginHeader = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        allowOriginHeader.Should().Be("http://localhost:5173");
        
        var allowCredentialsHeader = response.Headers.GetValues("Access-Control-Allow-Credentials").FirstOrDefault();
        allowCredentialsHeader.Should().Be("true");
    }

    [DockerAvailableFact]
    public async Task PreflightRequest_WithInvalidOrigin_ShouldNotReturnAllowOriginHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/accounts");
        request.Headers.Add("Origin", "http://malicious-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // CORS bloqueado - nao deve retornar Access-Control-Allow-Origin
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out _).Should().BeFalse();
    }

    [DockerAvailableFact]
    public async Task GetRequest_WithValidOrigin_ShouldAllowConfiguredMethods()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/accounts");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var allowMethodsHeader = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault();
        allowMethodsHeader.Should().Contain("GET");
        allowMethodsHeader.Should().Contain("POST");
        allowMethodsHeader.Should().Contain("PUT");
        allowMethodsHeader.Should().Contain("PATCH");
        allowMethodsHeader.Should().Contain("DELETE");
    }
}
