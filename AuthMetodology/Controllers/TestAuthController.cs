using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthMetodology.API.Controllers
{
    [ApiController]
    [Route("api/testAuth")]
    public class TestAuthController : ControllerBase
    {
        [Authorize]
        [HttpGet]
        public IActionResult GetTestData()
        {
            var data = new {Name = "Test", Surname = "Testovich" };
            return Ok(data);
        }
    }
}
