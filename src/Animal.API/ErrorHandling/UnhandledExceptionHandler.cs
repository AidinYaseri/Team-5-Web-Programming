using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Animal.API.ErrorHandling
{
    public sealed class UnhandledExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IHostEnvironment _hostEnvironment;

        public UnhandledExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IHostEnvironment hostEnvironment)
        {
            _problemDetailsService = problemDetailsService;
            _hostEnvironment = hostEnvironment;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var status = exception is TimeoutException
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError;

            var title = status == StatusCodes.Status503ServiceUnavailable
                ? "A required dependency is temporarily unavailable."
                : "An unexpected error occurred.";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title
            };

            if (_hostEnvironment.IsDevelopment())
            {
                problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            }

            httpContext.Response.StatusCode = status;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}
