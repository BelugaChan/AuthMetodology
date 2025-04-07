namespace AuthMetodology.Infrastructure.Models;

public class RabbitMqTwoFaPublish
{
    public required Guid Id { get; set; }

    public required string Code { get; set; }

    public required string Email { get; set; }

    public static RabbitMqTwoFaPublish Create(Guid id, string code, string email)
        => new RabbitMqTwoFaPublish()
        {
            Id = id,
            Code = code,
            Email = email
        };
}