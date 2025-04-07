namespace AuthMetodology.Infrastructure.Interfaces;

public interface IRabbitMqService
{
    void SendMessage(object obj);
    void SendMessage(string message);
}