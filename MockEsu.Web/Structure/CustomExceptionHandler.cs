using MockEsu.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using MockEsu.Application.Common.BaseRequests;
using static System.Net.WebRequestMethods;

namespace MockEsu.Web.Structure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(ValidationException), HandleValidationException },
                //{ typeof(NotFoundException), HandleNotFoundException },
                //{ typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                //{ typeof(ForbiddenAccessException), HandleForbiddenAccessException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                Title = "Unhandled exception occurred.",
                Detail = exception.Message
            });
        }
        return false;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        if (ex as ValidationException != null)
        {
            var exception = (ValidationException)ex;
            await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
            {
                Status = httpContext.Response.StatusCode,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            });
        }
        //else if (ex as System.ComponentModel.DataAnnotations.ValidationException != null)
        //{
        //    var exception = (System.ComponentModel.DataAnnotations.ValidationException)ex;
        //    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        //    {
        //        Status = httpContext.Response.StatusCode,
        //        Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
        //        Title = "One or more validation errors occurred.",
        //        Detail = exception.Message
        //    });
        //}


    }

    //private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    //{
    //    var exception = (NotFoundException)ex;

    //    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

    //    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
    //    {
    //        Status = StatusCodes.Status404NotFound,
    //        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    //        Title = "The specified resource was not found.",
    //        Detail = exception.Message
    //    });
    //}

    //private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    //{
    //    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

    //    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    //    {
    //        Status = StatusCodes.Status401Unauthorized,
    //        Title = "Unauthorized",
    //        Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
    //    });
    //}

    //private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    //{
    //    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

    //    await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
    //    {
    //        Status = StatusCodes.Status403Forbidden,
    //        Title = "Forbidden",
    //        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
    //    });
    //}
}
