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
        /// <summary>
        /// Тестовый эндпоинт.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// Get /api/v1/testAuth
        /// ### Возвращает:
        /// - Данные пользователя в теле ответа.
        /// ```json
        /// {
        ///     "Name": "Test",
        ///     "SurName": "Testovich"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Успешный вызов эндпоинта. Access токен в куки корректен.</response>
        /// <response code="401">Пользователь не прошёл аутентификацию</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
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
