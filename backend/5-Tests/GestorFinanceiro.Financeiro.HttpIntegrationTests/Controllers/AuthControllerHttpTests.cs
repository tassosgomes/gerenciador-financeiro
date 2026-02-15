using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class AuthControllerHttpTests : IntegrationTestBase
{
    public AuthControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        payload.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [DockerAvailableFact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorizedProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "missing@test.com",
            password = "Admin123!"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Login_WithInvalidEmailFormat_ReturnsValidationProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "invalid-email",
            password = "Admin123!"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "WrongPassword123!"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorizedProblemDetails()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var usersResponse = await adminClient.GetAsync("/api/v1/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserResponse>>(JsonSerializerOptions);
        var member = users!.Single(user => user.Email == "member@test.com");

        var deactivateResponse = await adminClient.PatchAsJsonAsync($"/api/v1/users/{member.Id}/status", new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "member@test.com",
            password = "Member123!"
        });

        await AssertProblemDetailsAsync(loginResponse, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Login_WithoutBody_ReturnsBadRequestProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { });
        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "Admin123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);

        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = loginPayload!.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);
        refreshPayload.Should().NotBeNull();
        refreshPayload!.RefreshToken.Should().NotBe(loginPayload.RefreshToken);
    }

    [DockerAvailableFact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorizedProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorizedProblemDetails()
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "Admin123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);
        await ExpireRefreshTokenAsync(loginPayload!.RefreshToken);

        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });

        await AssertProblemDetailsAsync(refreshResponse, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync("/api/v1/auth/logout", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task AuthFlow_LoginRefreshLogout_RevokesRefreshToken()
    {
        var loginClient = await AuthenticateAsAdminAsync();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@test.com",
            password = "Admin123!"
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);

        var refreshResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = loginPayload!.RefreshToken
        });

        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);
        refreshPayload.Should().NotBeNull();

        var protectedResponse = await loginClient.GetAsync("/api/v1/accounts");
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutResponse = await loginClient.PostAsync("/api/v1/auth/logout", content: null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var revokedRefreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = refreshPayload!.RefreshToken
        });

        await AssertProblemDetailsAsync(revokedRefreshResponse, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task ChangePassword_WithValidData_ReturnsNoContent()
    {
        var memberClient = await AuthenticateAsMemberAsync();

        var response = await memberClient.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Member123!",
            newPassword = "Member123!Updated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginWithNewPasswordResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "member@test.com",
            password = "Member123!Updated"
        });

        loginWithNewPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerAvailableFact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsUnauthorizedProblemDetails()
    {
        var memberClient = await AuthenticateAsMemberAsync();

        var response = await memberClient.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "WrongCurrent123!",
            newPassword = "Another123!"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task ChangePassword_WithWeakNewPassword_ReturnsBadRequestProblemDetails()
    {
        var memberClient = await AuthenticateAsMemberAsync();

        var response = await memberClient.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Member123!",
            newPassword = "123"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Member123!",
            newPassword = "Member123!Updated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
