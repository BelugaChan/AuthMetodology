﻿using Asp.Versioning;
using AuthMetodology.API.Interfaces;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMqPublisher.Interface;
using System.Security.Claims;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [EnableCors("AllowFrontend")]
    [Route("api/v{version:apiVersion}/twofa")]
    public class TwoFaController : ControllerBase
    {
        private readonly ITwoFaService twoFaService;
        private readonly ICookieCreator cookieCreator;
        private readonly IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService;
        private readonly JWTOptions options;
        public TwoFaController(ITwoFaService twoFaService, ICookieCreator cookieCreator, IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService, IOptions<JWTOptions> options)
        {
            this.twoFaService = twoFaService;
            this.cookieCreator = cookieCreator;
            this.logQueueService = logQueueService;
            this.options = options.Value;
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
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная активация. Возвращает "2FA enabled".</response>
        /// <response code="401">Пользователь с таким id не найден или замечена попытка подмены id</response>
        /// <response code="409">Двухфакторка уже активирована.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [Authorize]
        [HttpPatch("enable-2fa/{id}")]
        public async Task<IActionResult> EnableTwoFa([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                new RabbitMqLogPublish
                {
                    Message = "PATCH /api/v1/twofa/enable-2fa/id was called",
                    LogLevel = Serilog.Events.LogEventLevel.Information,
                    ServiceName = "AuthMetodology",
                    TimeStamp = DateTime.UtcNow
                }, cancellationToken
            );

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if(userId != id)
            {
                return Unauthorized("You can only enable 2FA for your own account.");
            }

            await twoFaService.EnableTwoFaStatusAsync(id, cancellationToken);
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
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная деактивация. Возвращает "2FA disabled".</response>
        /// <response code="401">Пользователь с таким id не найден или замечена попытка подмены id</response>
        /// <response code="409">Двухфакторка уже деактивирована.</response>
        /// <response code="500">Прочие ошибки на стороне сервера или ошибка при обновлении данных в БД</response>
        [MapToApiVersion(1)]
        [Authorize]
        [HttpPatch("disable-2fa/{id}")]
        public async Task<IActionResult> DisableTwoFa([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
                new RabbitMqLogPublish
                {
                    Message = "PATCH /api/v1/twofa/disable-2fa/id was called",
                    LogLevel = Serilog.Events.LogEventLevel.Information,
                    ServiceName = "AuthMetodology",
                    TimeStamp = DateTime.UtcNow
                }, cancellationToken
            );

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (userId != id)
            {
                return Unauthorized("You can only enable 2FA for your own account.");
            }

            await twoFaService.DisableTwoFaStatusAsync(id, cancellationToken);  
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
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная проверка. Возвращает AuthResponseDtoV1.</response>
        /// <response code="401">Пользователь с таким id не найден в системе</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(1)]
        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFa([FromBody] VerificationCodeRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
               new RabbitMqLogPublish
               {
                   Message = "POST /api/v1/twofa/verify-2fa was called",
                   LogLevel = Serilog.Events.LogEventLevel.Information,
                   ServiceName = "AuthMetodology",
                   TimeStamp = DateTime.UtcNow
               }, cancellationToken
            );
            var authDto = await twoFaService.VerifyCodeAsync(requestDto, "2fa", cancellationToken);

            cookieCreator.CreateTokenCookie("access", authDto.AccessToken, DateTime.UtcNow.AddMinutes(options.AccessTokenExpiryMinutes));
            cookieCreator.CreateTokenCookie("refresh", authDto.RefreshToken, DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays));

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
        /// <param name="cancellationToken">Токен прерывания операции.</param>
        /// <response code="200">Успешная отправка кода. Возвращает строку "Код отправлен повторно".</response>
        /// <response code="401">Пользователь с таким email не найден в системе</response>
        /// <response code="500">Прочие ошибки на стороне сервера</response>
        [MapToApiVersion(1)]
        [HttpPost("resend-2fa")]
        public async Task<IActionResult> ResendTwoFaCode([FromBody] ReSendVerificationCodeRequestDtoV1 requestDto, CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(
               new RabbitMqLogPublish
               {
                   Message = "POST /api/v1/twofa/resend-2fa was called",
                   LogLevel = Serilog.Events.LogEventLevel.Information,
                   ServiceName = "AuthMetodology",
                   TimeStamp = DateTime.UtcNow
               }, cancellationToken
            );

            await twoFaService.ResendVerificationCodeAsync(requestDto, "2fa", cancellationToken);
            return Ok("Код отправлен повторно");
        }
    }
}
