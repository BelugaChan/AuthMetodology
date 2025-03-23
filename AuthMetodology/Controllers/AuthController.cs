using Microsoft.AspNetCore.Mvc;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.DTO;
namespace AuthMetodology.API.Controllers
{
    [Controller]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService userService;
        public AuthController(IUserService userService) => this.userService = userService;


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await userService.RegisterUserAsync(userDto);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto userDto, HttpContext context)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = await userService.LoginAsync(userDto);

            context.Response.Cookies.Append("tast-cookies", token);

            return Ok();

        }
    }
}
