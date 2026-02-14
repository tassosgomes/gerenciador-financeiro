using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Controllers;
using GestorFinanceiro.Financeiro.API.Controllers.Requests;
using GestorFinanceiro.Financeiro.Application.Commands.Category;
using GestorFinanceiro.Financeiro.Application.Common;
using GestorFinanceiro.Financeiro.Application.Dtos;
using GestorFinanceiro.Financeiro.Application.Queries.Category;
using GestorFinanceiro.Financeiro.Domain.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class CategoriesControllerTests
{
    private readonly Mock<IDispatcher> _dispatcherMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _dispatcherMock = new Mock<IDispatcher>();
        _controller = new CategoriesController(_dispatcherMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedWithLocation()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var request = new CreateCategoryRequest
        {
            Name = "Alimentacao",
            Type = CategoryType.Despesa
        };

        var response = CreateCategoryResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<CreateCategoryCommand, CategoryResponse>(
                It.IsAny<CreateCategoryCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.CreateAsync(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedResult>();
        var createdResult = (CreatedResult)result.Result!;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.Location.Should().Be($"/api/v1/categories/{response.Id}");
        createdResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task ListAsync_WithTypeFilter_ShouldReturnOkWithResponse()
    {
        var response = new List<CategoryResponse> { CreateCategoryResponse() };

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchQueryAsync<ListCategoriesQuery, IReadOnlyList<CategoryResponse>>(
                It.Is<ListCategoriesQuery>(query => query.Type == CategoryType.Despesa),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.ListAsync(CategoryType.Despesa, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldReturnOkWithResponse()
    {
        ConfigureAuthenticatedUser(Guid.NewGuid());

        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest { Name = "Moradia" };
        var response = CreateCategoryResponse();

        _dispatcherMock
            .Setup(dispatcher => dispatcher.DispatchCommandAsync<UpdateCategoryCommand, CategoryResponse>(
                It.IsAny<UpdateCategoryCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.UpdateAsync(categoryId, request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(response);
    }

    private void ConfigureAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private static CategoryResponse CreateCategoryResponse()
    {
        return new CategoryResponse(
            Guid.NewGuid(),
            "Alimentacao",
            CategoryType.Despesa,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
