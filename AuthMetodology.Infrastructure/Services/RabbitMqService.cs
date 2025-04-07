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
    public void SendMessage(object obj)
    {
        var message = JsonSerializer.Serialize(obj);
        SendMessage(message);
    }

    public void SendMessage(string message)
    {
        var factory = new ConnectionFactory(){HostName = options.Host};
        using var connection = factory.CreateConnection();
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(
                queue: options.Queue,
                durable: false,
                exclusive: false,
                autoDelete:  false,
                arguments: null
                );
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "", routingKey: options.Queue, body: body);
        };

    }
}