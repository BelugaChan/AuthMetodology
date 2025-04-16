using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Models.v1;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.Application.Services
{
    public class ResetPasswordService : IResetPasswordService
    {
        private readonly IMapper mapper;
        private readonly IUserRepository userRepository;
        private readonly ResetPasswordOptions options;
        private readonly IJWTProvider jWTProvider;
        private readonly IRabbitMqPublisherBase<RabbitMqResetPasswordPublish> resetPasswordQueueService;
        public ResetPasswordService(IMapper mapper,IUserRepository userRepository, IOptions<ResetPasswordOptions> options, IJWTProvider jWTProvider, IRabbitMqPublisherBase<RabbitMqResetPasswordPublish> resetPasswordQueueService)
        {
            this.mapper = mapper;
            this.userRepository = userRepository;
            this.options = options.Value;
            this.jWTProvider = jWTProvider;
            this.resetPasswordQueueService = resetPasswordQueueService;
        }
        public async Task ForgotPasswordAsync(ForgotPasswordRequestDtoV1 requestDto, string scheme, string host, CancellationToken cancellationToken = default)
        {
            var userEntity = await userRepository.GetByEmailAsync(requestDto.Email, cancellationToken);
            if (userEntity is not null)
            {
                var user = mapper.Map<UserV1>(userEntity);

                var resetToken = jWTProvider.GenerateResetToken();
                var resetTokenExpiry = DateTime.UtcNow.AddHours(options.ResetTokenExpiryHours);

                var isOk = await userRepository.UpdateUserAsync(user.Id, u =>
                {
                    u.ResetPasswordToken = resetToken;
                    u.ResetPasswordTokenExpiry = resetTokenExpiry;
                }, cancellationToken);
                if(isOk) 
                {
                    var resetPasswordLink = $"{scheme}://{host}/api/account/reset-password?token={user.ResetPasswordToken}";
                    var rabbitMqResetPasswordModel = RabbitMqResetPasswordPublish.Create(resetPasswordLink, user.Email);
                    _ = resetPasswordQueueService.SendEventAsync(rabbitMqResetPasswordModel, cancellationToken);
                }
                else
                {
                    throw new DbUpdateException();
                }
            }
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDtoV1 requestDto, CancellationToken cancellationToken = default)
        {
            var userEntity = await userRepository.GetByResetPasswordTokenAsync(requestDto.Token, cancellationToken);
            if (userEntity is not null) 
            {
                var user = mapper.Map<UserV1>(userEntity);

                if (user.ResetPasswordTokenExpiry >= DateTime.UtcNow)
                    throw new ExpiredResetPasswordTokenException();

                if (user.ResetPasswordToken != requestDto.Token)
                    throw new IncorrectResetPasswordTokenException();

                var isOk = await userRepository.UpdateUserAsync(userEntity.Id, u =>
                {
                    u.ResetPasswordToken = string.Empty;
                    u.ResetPasswordTokenExpiry = default;
                }, cancellationToken);
                if (!isOk)
                {
                    throw new DbUpdateException();
                }
                
            }
            else
            {
                throw new UserNotFoundException();
            }
        }
    }
}
