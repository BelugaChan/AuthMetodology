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

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> logger;
        private readonly IUserService userService;
        private readonly IGoogleService googleService;
        private readonly ICookieCreator cookieCreator;
        private readonly JWTOptions options;

        public AuthController(ILogger<AuthController> logger, IUserService userService, IGoogleService googleService,ICookieCreator cookieCreator, IOptions<JWTOptions> options) 
        {
            this.logger = logger;
            this.googleService = googleService;
            this.userService = userService;
            this.cookieCreator = cookieCreator;
            this.options = options.Value;
        }

        /// <summary>
        /// Регистрирует нового пользователя в системе.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/register
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
        /// <param name="userDto">Данные для регистрации.</param>
        /// <response code="200">Успешная регистрация. Возвращает AuthResponseDtoV1.</response>
        /// <response code="400">Невалидные данные (например, пароли не совпадают или не соблюдаются прочие условия).</response>
        /// <response code="409">Пользователь с таким email уже существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(1)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestDtoV1 userDto)
        {
            if (!ModelState.IsValid)
            {
                logger.LogError("ModelState is invalid in POST /api/v1/auth/register \n{ModelState}",ModelState);
                return BadRequest(ModelState);
            }

            logger.LogInformation("POST /api/v1/auth/register was called");

            var authResponse = await userService.RegisterUserAsync(userDto);

            cookieCreator.CreateTokenCookie("access", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
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
        /// <response code="200">Успешный логин. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Некорректный пароль </response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestDtoV1 userDto)
        {
            if (!ModelState.IsValid)
            {
                logger.LogError("ModelState is invalid in POST /api/v1/auth/login \n{ModelState}",ModelState);
                return BadRequest(ModelState);
            }

            logger.LogInformation("POST /api/v1/auth/login was called");

            var authResponse = await userService.LoginAsync(userDto);
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
        /// <response code="200">Успешное обновление токенов. Возвращает RefreshResponseDtoV1.</response>
        /// <response code="401">Пользователь не найден в бд, либо refreshToken некорректен или у него закончилось время жизни.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDtoV1 requestDto)
        {
            logger.LogInformation("POST /api/v1/auth/refresh was called");

            var principal = GetPrincipalFromExpiredToken(requestDto.AccessToken);
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var refreshTokenResponseDto = await userService.UpdateUserTokensAsync(userId, requestDto);

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
        /// <response code="200">Успешная регистрация. Возвращает AuthResponseDtoV1.</response>
        /// <response code="409">Пользователь с таким email уже существует.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("googleRegister")]
        public async Task<IActionResult> GoogleRegister(GoogleLoginUserRequestDtoV1 requestDto)
        {
            logger.LogInformation("POST /api/v1/auth/googleRegister was called");

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.CreateGoogleUserAsync(payload);
            return Ok(response);
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
        /// <response code="200">Успешный логин. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Пользователь с таким email не найден или idToken некорректен</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPost("googleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginUserRequestDtoV1 requestDto)
        {
            logger.LogInformation("POST /api/v1/auth/googleLogin was called");

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.LoginGoogleUserAsync(payload);
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
