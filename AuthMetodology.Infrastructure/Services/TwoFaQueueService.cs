using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;
using RabbitMqModel.Models;
using RabbitMqPublisher.Abstract;

namespace AuthMetodology.Infrastructure.Services
{
    public class TwoFaQueueService : RabbitMqPublisherBase<RabbitMqTwoFaPublish>
    {
        public override string QueueName => "ResetPasswordQueue";

        public TwoFaQueueService(IOptions<RabbitMqOptions> options)
            : base(options)
        {
        }

        public async override Task SendEventAsync(RabbitMqTwoFaPublish message, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(message, QueueName, cancellationToken);
        }
    }
}
