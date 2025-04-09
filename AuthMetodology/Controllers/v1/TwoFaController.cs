using Asp.Versioning;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [Route("api/v{version:apiVersion}/twofa")]
    public class TwoFaController : ControllerBase
    {
        private readonly ILogger<TwoFaController> logger;
        private readonly ITwoFaService twoFaService;
        public TwoFaController(ILogger<TwoFaController> logger, ITwoFaService twoFaService)
        {
            this.logger = logger;
            this.twoFaService = twoFaService;
        }

        /// <summary>
        /// Активация двухфакторной аутентификации пользователя.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// PATCH /api/v1/auth/enable-2fa/7999070a-39bf-44b7-82b2-3de0de12b107
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что двухакторка активирована.
        /// ```
        /// </remarks>
        /// <param name="id">Guid пользователя.</param>
        /// <response code="200">Успешная активация. Возвращает "2FA enabled".</response>
        /// <response code="401">Пользователь с таким id не найден</response>
        /// <response code="409">Двухфакторка уже активирована.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPatch("enable-2fa/{id}")]
        public async Task<IActionResult> EnableTwoFa([FromRoute] Guid id)
        {
            logger.LogInformation("PATCH /api/v1/twofa/enable-2fa/id was called");

            await twoFaService.EnableTwoFaStatusAsync(id);
            return Ok("2FA enabled");
        }

        /// <summary>
        /// Деактивация двухфакторной аутентификации пользователя.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// PATCH /api/v1/auth/disable-2fa/7999070a-39bf-44b7-82b2-3de0de12b107
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что двухакторка деактивирована.
        /// ```
        /// </remarks>
        /// <param name="id">Guid пользователя.</param>
        /// <response code="200">Успешная деактивация. Возвращает "2FA disabled".</response>
        /// <response code="401">Пользователь с таким id не найден</response>
        /// <response code="409">Двухфакторка уже деактивирована.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [HttpPatch("disable-2fa/{id}")]
        public async Task<IActionResult> DisableTwoFa([FromRoute] Guid id)
        {
            logger.LogInformation("PATCH /api/v1/twofa/disable-2fa/id was called");

            await twoFaService.DisableTwoFaStatusAsync(id);  
            return Ok("2FA disabled");
        }

        /// <summary>
        /// Проверяет код двухфакторной аутентификации.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/verify-2fa
        /// ```json
        /// {
        ///     "id": "7999070a-39bf-44b7-82b2-3de0de12b107",
        ///     "code": "123456"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - id: обязательное поле.
        /// - code: обязательное поле.
        /// 
        /// ### Возвращает:
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
        /// <param name="requestDto">Данные для проверки кода двухфакторной аутентификации.</param>
        /// <response code="200">Успешная проверка. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Пользователь с таким id не найден в системе</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(1)]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFa([FromBody] TwoFaRequestDtoV1 requestDto)
        {
            logger.LogInformation("POST /api/v1/twofa/verify-2fa was called");

            var authDto = await twoFaService.VerifyTwoFaCodeAsync(requestDto);
            return Ok(authDto);
        }

        /// <summary>
        /// Повторная отправка кода двухфакторной аутентификации.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// POST /api/v1/auth/resend-2fa
        /// ```json
        /// {
        ///     "email": "abc@mail.com"
        /// }
        /// ```
        /// 
        /// ### Требования:
        /// - email: обязательное поле.
        /// 
        /// ### Возвращает:
        /// - Уведомление о том, что код был успешно отправлен.
        /// </remarks>
        /// <param name="requestDto">Данные для проверки кода двухфакторной аутентификации.</param>
        /// <response code="200">Успешная отправка кода. Возвращает строку "Код отправлен повторно".</response>
        /// <response code="401">Пользователь с таким email не найден в системе</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(1)]
        [HttpPost("resend-2fa")]
        public async Task<IActionResult> ResendTwoFa([FromBody] ReSendTwoFaRequestDtoV1 requestDto)
        {
            //var userEntity = await userRepository.GetByIdAsync(id);
            //var user = mapper.Map<UserV1>(userEntity);
            //if (user.Is2FaEnabled is false)
            //    return BadRequest("2FA не активирована");
            logger.LogInformation("POST /api/v1/twofa/resend-2fa was called");

            await twoFaService.SendTwoFaAsync(requestDto);
            return Ok("Код отправлен повторно");
        }
    }
}
