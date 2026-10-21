using Animal.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.ErrorHandling
{
    // Turns domain exceptions into ProblemDetails so controllers don't need try/catch.
    // EntityNotFoundException -> 404, BusinessRuleException -> 409. Anything else falls through to a 500.
    public class DomainExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetails;

        public DomainExceptionHandler(IProblemDetailsService problemDetails)
        {
            _problemDetails = problemDetails;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int? status = exception switch
            {
                EntityNotFoundException => StatusCodes.Status404NotFound,
                BusinessRuleException => StatusCodes.Status409Conflict,
                _ => null
            };

            if (status is null)
            {
                return false;
            }

            httpContext.Response.StatusCode = status.Value;
            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = status == StatusCodes.Status404NotFound ? "Not found" : "Business rule violated",
                    Detail = exception.Message
                }
            });
        }
    }
}
