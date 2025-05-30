﻿using Asp.Versioning;
using AuthMetodology.Application.Metrics;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RabbitMqPublisher.Interface;
using System.Threading;

namespace AuthMetodology.API.Controllers.v1
{
    [ApiVersion(1)]
    [ApiController]
    [EnableCors("AllowFrontend")]
    [Route("api/v{version:apiVersion}/testAuth")]
    public class TestAuthController : ControllerBase
    {
        private readonly MetricsRegistry metrics;
        private readonly IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService;
        public TestAuthController(MetricsRegistry metrics, IRabbitMqPublisherBase<RabbitMqLogPublish> logQueueService)
        {
            this.metrics = metrics;
            this.logQueueService = logQueueService;
        }
        /// <summary>
        /// Тестовый эндпоинт.
        /// </summary>
        /// <remarks>
        /// ### Пример запроса:
        /// GET /api/v1/testAuth
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
        [Authorize(Policy = "BearerOnly")]
        [MapToApiVersion(1)]
        [HttpGet]
        public IActionResult GetTestData(CancellationToken cancellationToken)
        {
            _ = logQueueService.SendEventAsync(//запуск логирования параллельно (не дожидаясь завершения)
                    new RabbitMqLogPublish
                    {
                        Message = "GET /api/v1/testAuth was called",
                        LogLevel = Serilog.Events.LogEventLevel.Information,
                        ServiceName = "AuthMetodology",
                        TimeStamp = DateTime.UtcNow
                    }, cancellationToken
                );

            var startTime = DateTime.UtcNow;
            var data = new {Name = "Test", Surname = "Testovich" };
            metrics.HttpRequestCounter.Add(1, 
                new KeyValuePair<string, object?>("endpoint", "/api/v1/testAuth"),
                new KeyValuePair<string, object?>("method", "GET"),
                new KeyValuePair<string, object?>("status", 200)
                );

            var ellapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            metrics.HttpResponseTimeMs.Record(ellapsed,
                new KeyValuePair<string, object?>("endpoint", "/api/v1/testAuth"),
                new KeyValuePair<string, object?>("method", "GET"),
                new KeyValuePair<string, object?>("status", 200)
                );

            return Ok(data);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [MapToApiVersion(1)]
        [HttpGet("admin-data")]
        public IActionResult GetAdminData()
        {
            return Ok("Secret data for admins!");
        }
    }
}
