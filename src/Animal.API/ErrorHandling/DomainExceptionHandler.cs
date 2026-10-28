using Animal.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Animal.API.ErrorHandling
{
    // Turns domain exceptions into ProblemDetails responses, so controllers don't need try/catch blocks.
    // Anything not listed here falls through to the default handler and becomes a 500.
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public DomainExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problem = exception switch
            {
                EntityNotFoundException ex => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not found.",
                    Detail = ex.Message
                },
                DomainValidationException ex => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more business rules were broken.",
                    Extensions = { ["errors"] = ex.Errors }
                },
                InvalidStatusTransitionException ex => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The request conflicts with the current status.",
                    Detail = ex.Message
                },
                // Usually a foreign key, e.g. deleting a pet that still has adoption applications.
                // The database message is not sent back because it describes the schema.
                DbUpdateException => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The change conflicts with existing data."
                },
                _ => null
            };

            if (problem is null)
            {
                return false;
            }

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}
