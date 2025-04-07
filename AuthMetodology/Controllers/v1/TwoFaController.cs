using Asp.Versioning;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Logic.Models.v1;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [Route("api/v{version:apiVersion}/twofa")]
    public class TwoFaController : ControllerBase
    {
        private readonly ITwoFaService twoFaService;
        public TwoFaController(ITwoFaService twoFaService)
        {
            this.twoFaService = twoFaService;
        }

        /// <summary>
        /// Эндпоинт для включения двухфакторной аутентификации пользователя
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Уведомление о том, что двухфакторная аутентификация активирована</returns>
        [MapToApiVersion(1)]
        [HttpPatch("enable-2fa")]
        public async Task<IActionResult> EnableTwoFa([FromRoute] Guid id)
        {
            await twoFaService.EnableTwoFaStatusAsync(id);
            return Ok("2FA enabled");
        }

        /// <summary>
        /// Эндпоинт для отключения двухфакторной аутентификации пользователя
        /// </summary>
        /// <param name="id">Идентификатор пользователя</param>
        /// <returns>Уведомление о том, что двухфакторная аутентификация дизактивирована</returns>
        [MapToApiVersion(1)]
        [HttpPatch("disable-2fa")]
        public async Task<IActionResult> DisableTwoFa([FromRoute] Guid id)
        {
            await twoFaService.DisableTwoFaStatusAsync(id);  
            return Ok("2FA disabled");
        }

        /// <summary>
        /// Эндпоинт для подтверждения двухфакторной аутентификации пользователя
        /// </summary>
        /// <param name="id">Dto, содержащее Id пользователя и шестизначный код</param>
        /// <returns>Экземпляр класса AuthResponseDtoV1</returns>
        [MapToApiVersion(1)]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFa([FromBody] TwoFaRequestDto requestDto)
        {
            var authDto = await twoFaService.VerifyTwoFaCodeAsync(requestDto);
            return Ok(authDto);
        }

        /// <summary>
        /// Эндпоинт для повторной отправки кода двухфакторной аутентификации пользователя
        /// </summary>
        /// <param name="id">Dto, содержащее Email пользователя</param>
        /// <returns>Уведомление о том, что код двухфакторной аутентификации отправлен повторно</returns>
        [MapToApiVersion(1)]
        [HttpPost("resend-2fa")]
        public async Task<IActionResult> ResendTwoFa([FromBody] ReSendTwoFaRequestDto requestDto)
        {
            //var userEntity = await userRepository.GetByIdAsync(id);
            //var user = mapper.Map<UserV1>(userEntity);
            //if (user.Is2FaEnabled is false)
            //    return BadRequest("2FA не активирована");

            await twoFaService.SendTwoFaAsync(requestDto);
            return Ok("Код отправлен повторно");
        }
    }
}
