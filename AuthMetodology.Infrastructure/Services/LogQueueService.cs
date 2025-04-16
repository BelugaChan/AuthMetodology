using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;
using RabbitMqModel.Models;
using RabbitMqPublisher.Abstract;

namespace AuthMetodology.Infrastructure.Services
{
    public class LogQueueService : RabbitMqPublisherBase<RabbitMqLogPublish>
    {
        public override string QueueName => "LogQueue";

        public LogQueueService(IOptions<RabbitMqOptions> options)
            :base(options)
        {
        }

        public async override Task SendEventAsync(RabbitMqLogPublish message, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(message, QueueName, cancellationToken);
        }
    }
}
