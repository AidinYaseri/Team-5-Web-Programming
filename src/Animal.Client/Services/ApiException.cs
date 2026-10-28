using System.Net;

namespace Animal.Client.Services
{
    // Thrown when the API answers with an error. Errors holds readable messages taken from the ProblemDetails body.
    public class ApiException : Exception
    {
        public ApiException(HttpStatusCode statusCode, IReadOnlyList<string> errors)
            : base(errors.Count > 0 ? string.Join(" ", errors) : $"The API returned {(int)statusCode}.")
        {
            StatusCode = statusCode;
            Errors = errors;
        }

        public HttpStatusCode StatusCode { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
