using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;
using RabbitMqModel.Models;
using RabbitMqPublisher.Abstract;

namespace AuthMetodology.Infrastructure.Services
{
    public class ResetPasswordQueueService : RabbitMqPublisherBase<RabbitMqResetPasswordPublish>
    {
        public override string QueueName => "ResetPasswordQueue";

        public ResetPasswordQueueService(IOptions<RabbitMqOptions> options)
            : base(options)
        {
        }

        public async override Task SendEventAsync(RabbitMqResetPasswordPublish message, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(message, QueueName, cancellationToken);
        }
    }
}
