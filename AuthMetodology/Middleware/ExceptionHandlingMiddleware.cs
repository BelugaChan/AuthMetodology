using AuthMetodology.Application.Exceptions;
using Google.Apis.Auth;
using System.Net;
using Microsoft.EntityFrameworkCore;

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
                UserNotFoundException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Пользователь с таким id не существует"),
                InvalidRefreshTokenException _ => new ExceptionResponse(HttpStatusCode.Unauthorized, "Invalid refresh token"),
                InvalidJwtException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Invalid token (called from GoogleLogin)"),
                IncorrectGoogleCredentialsException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Incorrect GoogleId or UserEmail"),
                DbUpdateException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "DB update wasn't successfull"),
                CacheNotFoundException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Data in cache wasn't found"),
                TwoFaCodeExpireException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Two fa code has been expired"),
                _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Internal server error. Please retry later")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)response.StatusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
