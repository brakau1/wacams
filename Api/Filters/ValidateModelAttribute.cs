using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Wacam.Api.Filters;

/// <summary>
/// Action filter that validates ModelState and returns a consistent error response.
/// Can be applied globally or per-controller.
/// </summary>
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new { Field = e.Key, Errors = e.Value.Errors.Select(er => er.ErrorMessage).ToArray() })
                .ToArray();

            context.Result = new BadRequestObjectResult(new
            {
                success = false,
                message = "Validation failed.",
                errors = errors
            });
        }

        base.OnActionExecuting(context);
    }
}
