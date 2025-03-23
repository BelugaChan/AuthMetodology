using System.Net;

namespace AuthMetodology.API.Middleware
{
    public record ExceptionResponse(HttpStatusCode StatusCode, string Description);
}
