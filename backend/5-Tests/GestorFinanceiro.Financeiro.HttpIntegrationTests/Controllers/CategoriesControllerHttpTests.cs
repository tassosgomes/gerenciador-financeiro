using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using GestorFinanceiro.Financeiro.HttpIntegrationTests.Base;

namespace GestorFinanceiro.Financeiro.HttpIntegrationTests.Controllers;

public class CategoriesControllerHttpTests : IntegrationTestBase
{
    public CategoriesControllerHttpTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [DockerAvailableFact]
    public async Task CreateListFilterAndUpdateCategory_ReturnsExpectedData()
    {
        var client = await AuthenticateAsAdminAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "Lazer",
            type = "Despesa"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonObjectAsync(createResponse);
        var categoryId = created["id"]!.GetValue<Guid>();

        var filterResponse = await client.GetAsync("/api/v1/categories?type=Receita");
        filterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filtered = await filterResponse.Content.ReadFromJsonAsync<JsonArray>(JsonSerializerOptions);
        filtered.Should().NotBeNull();
        filtered!
            .Select(item => item?["type"]?.GetValue<string>())
            .Should()
            .Contain(type => type == "Receita");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/categories/{categoryId}", new
        {
            name = "Lazer Atualizado"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [DockerAvailableFact]
    public async Task CreateCategory_WithDuplicateName_ReturnsBadRequestProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "Salario",
            type = "Receita"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task CreateCategory_WithoutName_ReturnsBadRequestProblemDetails()
    {
        var client = await AuthenticateAsAdminAsync();

        var response = await client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "",
            type = "Despesa"
        });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [DockerAvailableFact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/categories", new
        {
            name = "Sem Token",
            type = "Despesa"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
