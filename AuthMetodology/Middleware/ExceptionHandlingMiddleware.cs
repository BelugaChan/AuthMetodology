using AuthMetodology.Application.Exceptions;
using System.Net;

namespace AuthMetodology.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);                
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            ExceptionResponse response = exception switch
            {
                ExistMailException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Пользователь с такой почтой уже существует"),
                IncorrectPasswordException _ => new ExceptionResponse(HttpStatusCode.Unauthorized, "Неверный пароль"),
                IncorrectMailException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Пользователь с такой почтой не найден в системе"),
                _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Internal server error. Please retry later")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)response.StatusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
