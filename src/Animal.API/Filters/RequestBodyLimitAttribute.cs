using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Animal.API.Filters
{
    // Hard limit on the whole request body, enforced before model binding reads the form.
    // It's a resource filter, so it runs after authorization (401/403 come first) but before
    // [FromForm] parameters are bound. An oversized request never gets parsed.
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequestBodyLimitAttribute : Attribute, IAsyncResourceFilter
    {
        public RequestBodyLimitAttribute(long maxBytes)
        {
            MaxBytes = maxBytes;
        }

        public long MaxBytes { get; }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            // The client said how big the body is: refuse it without reading a single byte.
            if (request.ContentLength > MaxBytes)
            {
                context.Result = PayloadTooLarge(httpContext);
                return;
            }

            // No Content-Length (a chunked body): the server counts the bytes as they arrive and
            // stops at the limit. Kestrel throws a 413 BadHttpRequestException once it's passed.
            var sizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is { IsReadOnly: false })
            {
                sizeFeature.MaxRequestBodySize = MaxBytes;
            }

            // Read the form here, inside the limit, so a body that turns out too big becomes a 413
            // instead of a binding error. Model binding then uses this already-read form.
            if (request.HasFormContentType)
            {
                try
                {
                    await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = MaxBytes }, httpContext.RequestAborted);
                }
                catch (Exception exception) when (IsBodyTooLarge(exception))
                {
                    context.Result = PayloadTooLarge(httpContext);
                    return;
                }
            }

            await next();
        }

        private static bool IsBodyTooLarge(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
                {
                    return true;
                }
            }

            return false;
        }

        private ObjectResult PayloadTooLarge(HttpContext httpContext)
        {
            var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateProblemDetails(
                httpContext,
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "The request is too large.",
                detail: $"The whole request must be {MaxBytes / (1024 * 1024)} MB or smaller.");

            return new ObjectResult(problem) { StatusCode = StatusCodes.Status413PayloadTooLarge };
        }
    }
}
