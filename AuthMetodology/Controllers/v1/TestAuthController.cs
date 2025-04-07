using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [Route("api/v{version:apiVersion}/testAuth")]
    public class TestAuthController : ControllerBase
    {
        [Authorize]
        [MapToApiVersion(1)]
        [HttpGet]
        public IActionResult GetTestData()
        {
            var data = new {Name = "Test", Surname = "Testovich" };
            return Ok(data);
        }
    }
}
