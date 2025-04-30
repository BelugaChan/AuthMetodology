using AuthMetodology.Application.Exceptions;
using Google.Apis.Auth;
using System.Net;
using Microsoft.EntityFrameworkCore;
using AuthMetodology.Infrastructure.Models;
using RabbitMQ.Client.Exceptions;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService;

        public ExceptionHandlingMiddleware(RequestDelegate next, IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService) 
        {
            _next = next;
            this.logQueueService = logQueueService;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _ = logQueueService.SendEventAsync(
                   new RabbitMqLogPublish
                   {
                           Message = $"Exception was thrown.\nMessage: {ex.Message}\nSource: {ex.Source}",
                           LogLevel = Serilog.Events.LogEventLevel.Error,
                           ServiceName = "AuthMetodology",
                           TimeStamp = DateTime.UtcNow
                   }
                );
                await HandleExceptionAsync(context, ex);                
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            ExceptionResponse response = exception switch
            {
                ExistMailException _ => new ExceptionResponse(HttpStatusCode.Conflict, "Пользователь с такой почтой уже существует"),
                IncorrectPasswordException _ => new ExceptionResponse(HttpStatusCode.Unauthorized, "Неверный пароль"),
                IncorrectMailException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Пользователь с такой почтой не найден в системе"),
                UserNotFoundException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Пользователь с таким id не существует"),
                InvalidRefreshTokenException _ => new ExceptionResponse(HttpStatusCode.Unauthorized, "Invalid refresh token"),
                InvalidJwtException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Invalid token (called from GoogleLogin)"),
                IncorrectGoogleCredentialsException _ => new ExceptionResponse(HttpStatusCode.BadRequest, "Incorrect GoogleId or UserEmail"),
                DbUpdateException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "DB update wasn't successfull"),
                CacheNotFoundException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Data in cache wasn't found"),
                TwoFaCodeExpireException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Two fa code has been expired"),
                InvalidTwoFaStatusException _ => new ExceptionResponse(HttpStatusCode.Conflict, exception.Message),
                BrokerUnreachableException _ => new ExceptionResponse(HttpStatusCode.InternalServerError, exception.Message),
                ExpiredResetPasswordTokenException _ => new ExceptionResponse(HttpStatusCode.Conflict, "Время жизни токена для смены пароля истекло. Попробуйте восстановить пароль снова."),
                IncorrectResetPasswordTokenException _ => new ExceptionResponse(HttpStatusCode.Conflict, "Некорректный токен для смены пароля.."),
                UsernameExistsException _ => new ExceptionResponse(HttpStatusCode.Conflict, "Пользователь с таким никнеймом уже существует"),
                _ => new ExceptionResponse(HttpStatusCode.InternalServerError, $"Internal server error. Please retry later.\nEx trace: {exception}")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)response.StatusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
