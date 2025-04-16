namespace AuthMetodology.Infrastructure.Models
{
    public class RabbitMqResetPasswordPublish
    {
        public required string Link { get; set; }

        public required string Email { get; set; }

        public static RabbitMqResetPasswordPublish Create(string link, string email)
            => new RabbitMqResetPasswordPublish() 
            { 
                Link = link,
                Email = email
            };
    }
}
