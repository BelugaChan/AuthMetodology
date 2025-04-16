using Serilog.Events;

namespace AuthMetodology.Infrastructure.Models
{
    public class RabbitMqLogPublish
    {
        public required string Message { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        public required LogEventLevel LogLevel { get; set; }

        public required string ServiceName { get; set; }
    }
}
