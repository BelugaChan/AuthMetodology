using System.Text;
using System.Text.Json;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AuthMetodology.Infrastructure.Services;

public class RabbitMqService : IRabbitMqService
{
    private readonly RabbitMqOptions options;
    public RabbitMqService(IOptions<RabbitMqOptions> options) =>  this.options = options.Value;
    public async Task SendMessageAsync(object obj, string queueName)
    {
        var message = JsonSerializer.Serialize(obj);
        await SendMessageAsync(message, queueName);
    }

    public async Task SendMessageAsync(string message, string queueName)
    {
        var factory = new ConnectionFactory(){HostName = options.Host, Port=options.Port};
        using var connection = await factory.CreateConnectionAsync();
        using (var channel = await connection.CreateChannelAsync())
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete:  false,
                arguments: null
                );
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
        };

    }
}