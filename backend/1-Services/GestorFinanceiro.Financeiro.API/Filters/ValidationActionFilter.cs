using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestorFinanceiro.Financeiro.API.Filters;

public sealed class ValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(state => state.Value?.Errors.Count > 0)
            .ToDictionary(
                state => state.Key,
                state => state.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Type = "https://httpstatuses.com/400",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        context.Result = new BadRequestObjectResult(problemDetails);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
