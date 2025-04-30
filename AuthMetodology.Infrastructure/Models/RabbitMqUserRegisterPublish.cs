namespace AuthMetodology.Infrastructure.Models
{
    public class RabbitMqUserRegisterPublish
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }
    }
}
