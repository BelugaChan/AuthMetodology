using Microsoft.AspNetCore.Mvc;
using AuthMetodology.API.Interfaces;
using Microsoft.Extensions.Options;
using AuthMetodology.Infrastructure.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly IGoogleService googleService;
        private readonly ICookieCreator cookieCreator;
        private readonly IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService;
        private readonly IResetPasswordService resetPasswordService;
        private readonly ITwoFaService twoFaService;
        private readonly JWTOptions options;

        public AuthController(IUserService userService, IGoogleService googleService,ICookieCreator cookieCreator, IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService,IOptions<JWTOptions> options, IResetPasswordService resetPasswordService, ITwoFaService twoFaService) 
        {
            this.googleService = googleService;
            this.userService = userService;
            this.cookieCreator = cookieCreator;
            this.logQueueService = logQueueService;
            this.options = options.Value;
            this.resetPasswordService = resetPasswordService;
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
        ///     "email": "user@example.com",
        ///     "password": "SecurePassword123!",
        ///     "confirmPassword": "SecurePassword123!"
        /// }
        /// ```
        /// 
        /// ### Требования:
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
        [MapToApiVersion(1)]
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
        [MapToApiVersion(1)]
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

        [MapToApiVersion(1)]
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

        /// <summary>
        /// Аутентификация существующего пользователя в системе.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/login
        /// ```json
        /// {
        ///     "email": "user@example.com",
        ///     "password": "SecurePassword123!"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - Email: 5–30 символов, валидный формат.
        /// - Пароль: 8–20 символов, минимум 1 цифра, 1 спецсимвол, буквы в верхнем и нижнем регистре. Отсутствие кирилицы.
        /// 
        /// ### Возвращает:
        /// - JWT-токены (access и refresh) в куках.
        /// - Данные пользователя в теле ответа.
        /// ```json
        /// {
        ///     "userId": "7999070a-39bf-44b7-82b2-3de0de12b107",
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I",
        ///     "requiresTwoFa": false/true
        /// }
        /// ```
        /// </remarks>
        /// <param name="userDto">Данные для логина.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешный логин. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Некорректный пароль </response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestDtoV1 userDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = $"ModelState is invalid in POST /api/v1/auth/login \n{ModelState}",
                        LogLevel = Serilog.Events.LogEventLevel.Error,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
                );
                return BadRequest(ModelState);
            }

            _ = logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = "POST /api/v1/auth/login was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
            );

            var authResponse = await userService.LoginAsync(userDto, cancellationToken);
            if(!string.IsNullOrEmpty(authResponse.AccessToken))
                cookieCreator.CreateTokenCookie("access", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            if(!string.IsNullOrEmpty(authResponse.RefreshToken))
                cookieCreator.CreateTokenCookie("refresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }

        /// <summary>
        /// Обновление пары access - refresh токенов.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/refresh
        /// ```json
        /// {
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - accessToken: обязательное поле.
        /// - refreshToken: обязательное поле. 
        /// 
        /// ### Возвращает:
        /// - JWT-токены (access и refresh) в куках.
        /// - Данные о токенах в теле ответа.
        /// ```json
        /// {
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I"
        /// }
        /// ```
        /// </remarks>
        /// <param name="requestDto">Данные для обновления токенов.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешное обновление токенов. Возвращает RefreshResponseDtoV1.</response>
        /// <response code="401">Пользователь не найден в бд, либо refreshToken некорректен или у него закончилось время жизни.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                new RabbitMqLogPublish 
                    { 
                        Message = "POST /api/v1/auth/refresh was called", 
                        LogLevel = Serilog.Events.LogEventLevel.Information, 
                        ServiceName = "AuthMetodology", 
                        TimeStamp = DateTime.UtcNow 
                    }, cancellationToken
                );

            var principal = GetPrincipalFromExpiredToken(requestDto.AccessToken);
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var refreshTokenResponseDto = await userService.UpdateUserTokensAsync(userId, requestDto, cancellationToken);

            cookieCreator.CreateTokenCookie("access", refreshTokenResponseDto.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", refreshTokenResponseDto.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(refreshTokenResponseDto);

        }

        /// <summary>
        /// Регистрация пользователя в системе с помощью платформы Google.
        /// </summary>
        /// <remarks>
        /// Данный эндпоинт не проходил тестирование. Логика сохранения кук не реализована.
        /// ### Пример запроса:
        /// POST /api/v1/auth/googleRegister
        /// ```json
        /// {
        ///     "idToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - idToken: обязательное поле.
        /// 
        /// ### Возвращает:
        /// - Данные пользователя в теле ответа.
        /// ```json
        /// {
        ///     "userId": "7999070a-39bf-44b7-82b2-3de0de12b107",
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I",
        ///     "requiresTwoFa": false/true
        /// }
        /// ```
        /// </remarks>
        /// <param name="requestDto">Данные для регистрации.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная регистрация. Возвращает AuthResponseDtoV1.</response>
        /// <response code="409">Пользователь с таким email уже существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("googleRegister")]
        public async Task<IActionResult> GoogleRegister(GoogleLoginUserRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                new RabbitMqLogPublish
                {
                    Message = "POST /api/v1/auth/googleRegister was called",
                    LogLevel = Serilog.Events.LogEventLevel.Information,
                    ServiceName = "AuthMetodology",
                    TimeStamp = DateTime.UtcNow
                }, cancellationToken
            );

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.CreateGoogleUserAsync(payload);
            return Ok(response);
        }

        /// <summary>
        /// Изменение пароля пользователя.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/reset-password
        /// ```json
        /// {
        ///     "token": "qwjnckjndslksjdnpqwkmpoqkmd",
        ///     "password": "SecurePassword123!",
        ///     "confirmPassword": "SecurePassword123!"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - Пароль: 8–20 символов, минимум 1 цифра, 1 спецсимвол, буквы в верхнем и нижнем регистре. Отсутствие кирилицы.
        /// - Пароль и подтверждение должны совпадать.
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что пароль был успешно изменён
        /// ```
        /// </remarks>
        /// <param name="requestDto">Данные для логина.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешное изменение пароля. Возвращает следующее сообщение: "Пароль успешно изменён! Вы будете перемещены на форму логина"</response>
        /// <response code="409">Время жизни токена для смены пароля истекло, либо же токен некорректен</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = $"ModelState is invalid in POST /api/v1/auth/reset \n{ModelState}",
                        LogLevel = Serilog.Events.LogEventLevel.Error,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
                );
                return BadRequest(ModelState);
            }

            _ = logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = "POST /api/v1/auth/reset was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
            );

            await resetPasswordService.ResetPasswordAsync(requestDto, cancellationToken);

            return Ok("Пароль успешно изменён! Вы будете перемещены на форму логина");
        }

        /// <summary>
        /// Запрос на замену пароля пользователя.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/reset-password
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
        /// - Уведомление о том, что письмо с ссылкой для замены пароля было отправлено на почту, вне зависимости от того, был ли найден пользователь в системе или не был.
        /// ```
        /// </remarks>
        /// <param name="requestDto">Данные для отправки письма.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Отправка письма. Внимание! ответ 200 ОК будет даже в случае отсутствия пользователя в системе. Возвращает следующее сообщение: "Если почта существует, письмо с сылкой для изменения пароля было отправлено"</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                    new RabbitMqLogPublish
                    {
                        Message = "POST /api/v1/auth/forgot was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
            );

            await resetPasswordService.ForgotPasswordAsync(requestDto, Request.Scheme, Request.Host.ToString(), cancellationToken);

            return Ok("Если почта существует, письмо с сылкой для изменения пароля было отправлено");
        }

        /// <summary>
        /// Логин пользователя в системе с помощью платформы Google.
        /// </summary>
        /// <remarks>
        /// Данный эндпоинт не проходил тестирование. Логика сохранения кук не реализована.
        /// ### Пример запроса:
        /// POST /api/v1/auth/googleLogin
        /// ```json
        /// {
        ///     "idToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - idToken: обязательное поле.
        /// 
        /// ### Возвращает:
        /// - Данные пользователя в теле ответа.
        /// ```json
        /// {
        ///     "userId": "7999070a-39bf-44b7-82b2-3de0de12b107",
        ///     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30",
        ///     "refreshToken": "7YbHhgwhiQMqWdnw0czF1Wnp4yrHyGG0zWmgYWIL5pMNErt4FKkuNU6lZKjQjt6I",
        ///     "requiresTwoFa": false/true
        /// }
        /// ```
        /// </remarks>
        /// <param name="requestDto">Данные для логина.</param>
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешный логин. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Пользователь с таким email не найден или idToken некорректен</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("googleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginUserRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                new RabbitMqLogPublish
                {
                    Message = "POST /api/v1/auth/googleLogin was called",
                    LogLevel = Serilog.Events.LogEventLevel.Information,
                    ServiceName = "AuthMetodology",
                    TimeStamp = DateTime.UtcNow
                }, cancellationToken
            );

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.LoginGoogleUserAsync(payload, cancellationToken);
            return Ok(response);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
    }
}
