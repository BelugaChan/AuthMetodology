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
using AuthMetodology.Infrastructure.Interfaces;

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
        private readonly IRabbitMqService rabbitMqService;
        private readonly JWTOptions options;

        public AuthController(IUserService userService, IGoogleService googleService,ICookieCreator cookieCreator, IRabbitMqService rabbitMqService, IOptions<JWTOptions> options) 
        {
            this.googleService = googleService;
            this.userService = userService;
            this.cookieCreator = cookieCreator;
            this.rabbitMqService = rabbitMqService;
            this.options = options.Value;
        }

        [MapToApiVersion(1)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestDtoV1 userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResponse = await userService.RegisterUserAsync(userDto);

            cookieCreator.CreateTokenCookie("access", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }

        /// <summary>
        /// Эндпоинт для осуществления логина пользователя
        /// </summary>
        /// <param name="userDto">DTO для логина</param>
        /// <returns>Экземпляр класса AuthResponseDtoV1</returns>
        [MapToApiVersion(1)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestDtoV1 userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResponse = await userService.LoginAsync(userDto);
            cookieCreator.CreateTokenCookie("access", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }

        /// <summary>
        /// Эндпоинт для получения новой пары Access - Refresh токенов.
        /// </summary>
        /// <param name="requestDto">Dto c Access и Refresh токенами</param>
        /// <returns>Dto с Access и Refresh токенами</returns>
        [MapToApiVersion(1)]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDtoV1 requestDto)
        {
            var principal = GetPrincipalFromExpiredToken(requestDto.AccessToken);
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var refreshTokenResponseDto = await userService.UpdateUserTokensAsync(userId, requestDto);

            cookieCreator.CreateTokenCookie("tasty-cookies", refreshTokenResponseDto.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("tasty-cookies-fresh", refreshTokenResponseDto.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(refreshTokenResponseDto);

        }

        /// <summary>
        /// Эндпоинт для осуществления регистрации пользователя через Google
        /// </summary>
        /// <param name="requestDto">Dto с IdToken (получение от фронта)</param>
        /// <returns>Экземпляр класса AuthResponseDtoV1</returns>
        [MapToApiVersion(1)]
        [HttpPost("googleRegister")]
        public async Task<IActionResult> GoogleRegister(GoogleLoginUserRequestDtoV1 requestDto)
        {

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.CreateGoogleUserAsync(payload);
            return Ok(response);
        }

        /// <summary>
        /// Эндпоинт для осуществления логина пользователя через Google
        /// </summary>
        /// <param name="requestDto">Dto с IdToken (получение от фронта)</param>
        /// <returns>Экземпляр класса AuthResponseDtoV1</returns>
        [MapToApiVersion(1)]
        [HttpPost("googleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginUserRequestDtoV1 requestDto)
        {

            var payload = await googleService.VerifyGoogleTokenAsync(requestDto);
            var response = await googleService.LoginGoogleUserAsync(payload);
            return Ok(response);
        }


        /// <summary>
        /// Метод для получения полезной нагрузки из Access токена
        /// </summary>
        /// <param name="token">Access токен</param>
        /// <returns>Экземпляр класса ClaimsPrincipal (полезная нагрузка)</returns>
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
