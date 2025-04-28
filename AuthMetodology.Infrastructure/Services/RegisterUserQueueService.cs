using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;
using RabbitMqModel.Models;
using RabbitMqPublisher.Abstract;

namespace AuthMetodology.Infrastructure.Services
{
    public class RegisterUserQueueService : RabbitMqPublisherBase<RabbitMqUserRegisterPublish>
    {
        public RegisterUserQueueService(IOptions<RabbitMqOptions> options) : base(options)
        {
        }
        public override string QueueName => "RegisterUserQueue";

        public override async Task SendEventAsync(RabbitMqUserRegisterPublish message, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(message, QueueName, cancellationToken);
        }
    }
}
