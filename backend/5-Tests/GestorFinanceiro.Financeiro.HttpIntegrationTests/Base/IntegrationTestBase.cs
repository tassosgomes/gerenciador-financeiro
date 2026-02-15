using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Domain.Entity;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Seed;
using GestorFinanceiro.Financeiro.Infra.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected CustomWebApplicationFactory Factory { get; }

    protected HttpClient Client { get; private set; }

    public virtual async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        Client = Factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<HttpClient> AuthenticateAsAdminAsync()
    {
        return await AuthenticateAsync(TestDataSeeder.AdminEmail, TestDataSeeder.AdminPassword);
    }

    protected async Task<HttpClient> AuthenticateAsMemberAsync()
    {
        return await AuthenticateAsync(TestDataSeeder.MemberEmail, TestDataSeeder.MemberPassword);
    }

    protected static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        response.StatusCode.Should().Be(expectedStatusCode);

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var json = JsonNode.Parse(body) as JsonObject;
        json.Should().NotBeNull();
        json!["type"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        json["title"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        json["detail"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
        json["status"]!.GetValue<int>().Should().Be((int)expectedStatusCode);
    }

    protected static async Task<JsonObject> ReadJsonObjectAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(JsonSerializerOptions);
        json.Should().NotBeNull();
        return json!;
    }

    protected async Task ExpireRefreshTokenAsync(string refreshToken)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        var tokenHash = RefreshToken.ComputeHash(refreshToken);

        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(token => token.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
    }

    private async Task<HttpClient> AuthenticateAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonSerializerOptions);

        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
