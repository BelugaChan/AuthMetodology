using Asp.Versioning;
using AuthMetodology.API.Interfaces;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.Metrics;
using AuthMetodology.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.API.Controllers.v2
{
    [ApiVersion(2)]
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly ICookieCreator cookieCreator;
        private readonly IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService;
        private readonly ITwoFaService twoFaService;
        private readonly JWTOptions options;
        private readonly MetricsRegistry metrics;
        public AuthController(IUserService userService, ICookieCreator cookieCreator, IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService, IOptions<JWTOptions> options, MetricsRegistry metrics, ITwoFaService twoFaService)
        {
            this.userService = userService;
            this.cookieCreator = cookieCreator;
            this.logQueueService = logQueueService;
            this.metrics = metrics;
            this.options = options.Value;
            this.twoFaService = twoFaService;
        }

        /// <summary>
        /// Регистрирует нового пользователя в системе.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/register-initiate
        /// ```json
        /// {
        ///     "userName": "nickname",
        ///     "email": "user@example.com",
        ///     "password": "SecurePassword123!",
        ///     "confirmPassword": "SecurePassword123!"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - Username: 5–30 символов, валидный формат.
        /// - Email: 5–30 символов, валидный формат.
        /// - Пароль: 8–20 символов, минимум 1 цифра, 1 спецсимвол, буквы в верхнем и нижнем регистре. Отсутствие кирилицы.
        /// - Пароль и подтверждение должны совпадать.
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что код подтверждения при регситрации был отправлен на почту
        /// </remarks>
        /// <param name="userDto">Данные для регистрации.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная первичная регистрация. Возвращает строку: "Код для подтверждения почты был отправлен".</response>
        /// <response code="400">Невалидные данные (например, пароли не совпадают или не соблюдаются прочие условия).</response>
        /// <response code="409">Пользователь с таким email уже существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(2)]
        [HttpPost("register-initiate")]
        public async Task<IActionResult> RegisterInitiate([FromBody] RegisterUserRequestDtoV1 userDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = $"ModelState is invalid in POST /api/v1/auth/register-initiate \n{ModelState}",
                        LogLevel = Serilog.Events.LogEventLevel.Error,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken

                );
                metrics.HttpRequestCounter.Add(1,
                    new KeyValuePair<string, object?>("endpoint", "/api/v1/auth/register-initiate"),
                    new KeyValuePair<string, object?>("method", "POST"),
                    new KeyValuePair<string, object?>("status", 409));

                return BadRequest(ModelState);
            }

            _ = logQueueService.SendEventAsync(//запуск логирования параллельно (не дожидаясь завершения)
                    new RabbitMqLogPublish
                    {
                        Message = "POST /api/v1/auth/register-initiate was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
                );

            await userService.InitiateRegisterUserAsync(userDto, cancellationToken);

            metrics.HttpRequestCounter.Add(1,
                new KeyValuePair<string, object?>("endpoint", "/api/v1/auth/register-initiate"),
                new KeyValuePair<string, object?>("method", "POST"),
                new KeyValuePair<string, object?>("status", 200));

            return Ok("Код для подтверждения почты был отправлен");
        }

        /// <summary>
        /// Подтверждает регистрацию нового пользователя в системе.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/register-confirm
        /// ```json
        /// {
        ///     "email": "user@example.com",
        ///     "registrationCode": "123456"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - Email: 5–30 символов, валидный формат.
        /// - Код подтверждения: шестизначный код
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что код подтверждения при регситрации был отправлен на почту
        /// </remarks>
        /// <param name="userDto">Данные для отправки кода</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешное подтверждение регистрации. Возвращает AuthResponseDtoV1.</response>
        /// <response code="409">Пользователь с таким email не существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(2)]
        [HttpPost("register-confirm")]
        public async Task<IActionResult> RegisterConfirm([FromBody] ConfirmRegistrationRequestDtoV1 userDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = $"ModelState is invalid in POST /api/v1/auth/register-confirm \n{ModelState}",
                        LogLevel = Serilog.Events.LogEventLevel.Error,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken

                );
                return BadRequest(ModelState);
            }

            _ = logQueueService.SendEventAsync(//запуск логирования параллельно (не дожидаясь завершения)
                    new RabbitMqLogPublish
                    {
                        Message = "POST /api/v1/auth/register-confirm was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
            );
            var authResponse = await userService.ConfirmRegisterUserAsync(userDto, cancellationToken);

            cookieCreator.CreateTokenCookie("access", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }


        /// <summary>
        /// Отправляет код для верификации почты при регистрации повторно.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/register-confirm
        /// ```json
        /// {
        ///     "email": "user@example.com"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - Email: 5–30 символов, валидный формат.
        /// 
        /// ### Возвращает:
        /// - JWT-токены (access и refresh) в куках.
        /// - Данные пользователя в теле ответа.
        /// ```json
        /// {
        ///     "userId": "7999070a-39bf-44b7-82b2-3de0de12b107",
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I",
        ///     "requiresTwoFa": false
        /// }
        /// ```
        /// </remarks>
        /// <param name="userDto">Данные для подтверждения регистрации.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешное подтверждение регистрации. Возвращает AuthResponseDtoV1.</response>
        /// <response code="409">Пользователь с таким email не существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(2)]
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmationCode([FromBody] ReSendVerificationCodeRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
               new RabbitMqLogPublish
               {
                   Message = "POST /api/v1/twofa/resend-confirmation was called",
                   LogLevel = Serilog.Events.LogEventLevel.Information,
                   ServiceName = "AuthMetodology",
                   TimeStamp = DateTime.UtcNow
               }, cancellationToken
            );

            await twoFaService.ResendVerificationCodeAsync(requestDto, "confirm", cancellationToken);
            return Ok("Код отправлен повторно");
        }
    }
}
