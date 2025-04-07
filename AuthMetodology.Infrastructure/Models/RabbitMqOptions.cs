namespace AuthMetodology.Infrastructure.Models;

public class RabbitMqOptions
{
    public string Host { get; set; } = string.Empty;

    public string Queue { get; set; } = string.Empty;
}