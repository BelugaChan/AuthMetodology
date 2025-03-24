using Microsoft.AspNetCore.Mvc;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.DTO;
using AuthMetodology.API.Interfaces;
using Microsoft.Extensions.Options;
using AuthMetodology.Infrastructure.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Google.Apis.Auth.GoogleJsonWebSignature;
using Google.Apis.Auth;
namespace AuthMetodology.API.Controllers
{
    [Controller]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;
        private readonly ICookieCreator cookieCreator;
        private readonly JWTOptions options;
        public AuthController(IUserService userService, ICookieCreator cookieCreator, IOptions<JWTOptions> options) 
        {
            this.userService = userService;
            this.cookieCreator = cookieCreator;
            this.options = options.Value;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResponse = await userService.RegisterUserAsync(userDto);

            cookieCreator.CreateTokenCookie("tasty-cookies", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("tasty-cookies-fresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequestDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authResponse = await userService.LoginAsync(userDto);
            cookieCreator.CreateTokenCookie("tasty-cookies", authResponse.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("tasty-cookies-fresh", authResponse.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(authResponse);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto requestDto)
        {
            var principal = GetPrincipalFromExpiredToken(requestDto.AccessToken);
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var refreshTokenResponseDto = await userService.UpdateUserTokensAsync(userId, requestDto);

            cookieCreator.CreateTokenCookie("tasty-cookies", refreshTokenResponseDto.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("tasty-cookies-fresh", refreshTokenResponseDto.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

            return Ok(refreshTokenResponseDto);

        }

        //[HttpPost("googleAuth")]
        //public async Task<JsonResult> GoogleLogin(GoogleLoginUserRequestDto requestDto)
        //{

        //    Payload payload = await ValidateAsync(requestDto.IdToken, new ValidationSettings
        //    {
        //        Audience = new[] { "15020973482-sjt0uq7isblob38jakgl37rqf5n1lgi0.apps.googleusercontent.com" }
        //    });

        //}

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
