using AwesomeAssertions;
using GestorFinanceiro.Financeiro.API.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace GestorFinanceiro.Financeiro.UnitTests.API;

public class ValidationActionFilterTests
{
    [Fact]
    public void OnActionExecuting_WhenModelStateIsInvalid_ShouldReturnBadRequestWithProblemDetails()
    {
        var filter = new ValidationActionFilter();
        var context = CreateContext();
        context.ModelState.AddModelError("email", "Email is required");

        filter.OnActionExecuting(context);

        context.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)context.Result!;

        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        badRequest.Value.Should().BeOfType<ValidationProblemDetails>();

        var problemDetails = (ValidationProblemDetails)badRequest.Value!;
        problemDetails.Title.Should().Be("Validation Error");
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Errors.Should().ContainKey("email");
        problemDetails.Errors["email"][0].Should().Be("Email is required");
    }

    [Fact]
    public void OnActionExecuting_WhenModelStateIsValid_ShouldNotSetResult()
    {
        var filter = new ValidationActionFilter();
        var context = CreateContext();

        filter.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    private static ActionExecutingContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/users";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }
}
