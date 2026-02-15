using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class UsersControllerHttpTests : IntegrationTestBase
{
    public UsersControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task Admin_CreateUser_ReturnsCreatedWithLocation()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        var response = await adminClient.PostAsJsonAsync("/api/v1/users", new
        {
            name = "Novo Usuario",
            email = "new-user@test.com",
            password = "StrongPass123!",
            role = "Member"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var payload = await response.Content.ReadFromJsonAsync<UserResponse>(JsonSerializerOptions);
        payload.Should().NotBeNull();
        payload!.Email.Should().Be("new-user@test.com");
    }

    [DockerAvailableFact]
    public async Task Admin_CreateDuplicateEmail_ReturnsBadRequestProblemDetails()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        var response = await adminClient.PostAsJsonAsync("/api/v1/users", new
        {
            name = "Usuario Duplicado",
            email = "member@test.com",
            password = "StrongPass123!",
            role = "Member"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task Member_CreateUser_ReturnsForbidden()
    {
        var memberClient = await AuthenticateAsMemberAsync();
        var response = await memberClient.PostAsJsonAsync("/api/v1/users", new
        {
            name = "Sem Permissao",
            email = "sem-permissao@test.com",
            password = "StrongPass123!",
            role = "Member"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [DockerAvailableFact]
    public async Task Anonymous_CreateUser_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/users", new
        {
            name = "Anon",
            email = "anon@test.com",
            password = "StrongPass123!",
            role = "Member"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [DockerAvailableFact]
    public async Task Admin_ListUsers_ReturnsSeededUsers()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var response = await adminClient.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<UserResponse>>(JsonSerializerOptions);

        payload.Should().NotBeNull();
        payload!.Should().Contain(user => user.Email == "admin@test.com");
        payload.Should().Contain(user => user.Email == "member@test.com");
    }

    [DockerAvailableFact]
    public async Task Admin_GetMissingUser_ReturnsNotFoundProblemDetails()
    {
        var adminClient = await AuthenticateAsAdminAsync();

        var response = await adminClient.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [DockerAvailableFact]
    public async Task Member_ListUsers_ReturnsForbiddenProblemDetails()
    {
        var memberClient = await AuthenticateAsMemberAsync();

        var response = await memberClient.GetAsync("/api/v1/users");
        await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden);
    }

    [DockerAvailableFact]
    public async Task Admin_GetExistingUser_ReturnsOk()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var listResponse = await adminClient.GetAsync("/api/v1/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserResponse>>(JsonSerializerOptions);

        users.Should().NotBeNull();
        var member = users!.Single(user => user.Email == "member@test.com");

        var detailResponse = await adminClient.GetAsync($"/api/v1/users/{member.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerAvailableFact]
    public async Task Admin_DeactivateAndActivateUser_ReturnsNoContent()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var listResponse = await adminClient.GetAsync("/api/v1/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserResponse>>(JsonSerializerOptions);
        var member = users!.Single(user => user.Email == "member@test.com");

        var deactivateResponse = await adminClient.PatchAsJsonAsync($"/api/v1/users/{member.Id}/status", new { isActive = false });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activateResponse = await adminClient.PatchAsJsonAsync($"/api/v1/users/{member.Id}/status", new { isActive = true });
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [DockerAvailableFact]
    public async Task Member_UpdateUserStatus_ReturnsForbiddenProblemDetails()
    {
        var adminClient = await AuthenticateAsAdminAsync();
        var users = await (await adminClient.GetAsync("/api/v1/users")).Content.ReadFromJsonAsync<List<UserResponse>>(JsonSerializerOptions);
        var memberId = users!.Single(user => user.Email == "member@test.com").Id;

        var memberClient = await AuthenticateAsMemberAsync();
        var response = await memberClient.PatchAsJsonAsync($"/api/v1/users/{memberId}/status", new { isActive = false });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden);
    }
}
