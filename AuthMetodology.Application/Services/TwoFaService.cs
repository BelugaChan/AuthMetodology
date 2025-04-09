using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Persistence.Interfaces;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Logic.Models.v1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Infrastructure.Models;
using Microsoft.Extensions.Options;

namespace AuthMetodology.Application.Services
{
    public class TwoFaService : ITwoFaService
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly ITwoFaProvider twoFaProvider;
        private readonly IRabbitMqService rabbitMqService;
        private readonly IRedisService redisService;
        private readonly IJWTProvider jWtProvider;
        private readonly JWTOptions options;
        private readonly string TwoFaQueueName = "TwoFaQueue";
        public TwoFaService(IUserRepository userRepository,IMapper mapper, ITwoFaProvider twoFaProvider,IRabbitMqService rabbitMqService, IRedisService redisService, IJWTProvider jWtProvider, IOptions<JWTOptions> options)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.redisService = redisService;
            this.twoFaProvider = twoFaProvider;
            this.rabbitMqService = rabbitMqService;
            this.jWtProvider = jWtProvider;
            this.options = options.Value;
        }

        public async Task EnableTwoFaStatusAsync(Guid id)
        {
            var userEntity = await userRepository.GetByIdAsync(id);
            
            if (userEntity is not null) 
            {
                if (userEntity.Is2FaEnabled)
                    throw new InvalidTwoFaStatusException("2FA is already enabled...");

                var user = mapper.Map<UserV1>(userEntity);

                var isOk = await userRepository.UpdateUserAsync(id, u =>
                {
                    u.Is2FaEnabled = true;
                });
                if (!isOk)
                    throw new DbUpdateException();
            }
            else
            {
                throw new UserNotFoundException();
            }
        }

        public async Task DisableTwoFaStatusAsync(Guid id)
        {
            var userEntity = await userRepository.GetByIdAsync(id);
            
            if (userEntity is not null)
            {
                if(!userEntity.Is2FaEnabled)
                    throw new InvalidTwoFaStatusException("2FA is already disabled...");

                var user = mapper.Map<UserV1>(userEntity);
                var isOk = await userRepository.UpdateUserAsync(id, u =>
                {
                    u.Is2FaEnabled = false;
                });
                if (!isOk)
                    throw new DbUpdateException();
            }
            else
            {
                throw new UserNotFoundException();
            }
        }
        
        public async Task SendTwoFaAsync(SendTwoFaRequestDtoV1 requestDto)
        {
            var code = twoFaProvider.GenerateTwoFaCode();
            string key = $"twoFa:{requestDto.Id}";

            await SendAndSaveData(key, requestDto.Id, code, requestDto.Mail);
            //var publishDtoForRabbit = RabbitMqTwoFaPublish.Create(requestDto.Id, code, requestDto.Mail);
            //var twoFaModelForRedis = RedisTwoFa.Create(requestDto.Id, code, DateTime.UtcNow.AddMinutes(5));
            
            //await redisService.SetStringToCacheAsync(key, twoFaModelForRedis);

            //rabbitMqService.SendMessage(publishDtoForRabbit);
        }

        public async Task SendTwoFaAsync(ReSendTwoFaRequestDtoV1 requestDto)
        {
            var userEntity = await userRepository.GetByEmailAsync(requestDto.Email);
            if (userEntity is null)
                throw new UserNotFoundException();
            var code = twoFaProvider.GenerateTwoFaCode();
            string key = $"twoFa:{userEntity.Id}";
            await redisService.RemoveStringFromCacheAsync(key);

            await SendAndSaveData(key, userEntity.Id, code, requestDto.Email);
            //var publishDtoForRabbit = RabbitMqTwoFaPublish.Create(userEntity.Id, code, requestDto.Email);
            //var twoFaModelForRedis = RedisTwoFa.Create(userEntity.Id, code, DateTime.UtcNow.AddMinutes(5));

            //await redisService.SetStringToCacheAsync(key, twoFaModelForRedis);

            //rabbitMqService.SendMessage(publishDtoForRabbit);
        }
        
        private async Task SendAndSaveData(string key, Guid id, string code, string mail)
        {
            var publishDtoForRabbit = RabbitMqTwoFaPublish.Create(id, code, mail);
            var twoFaModelForRedis = RedisTwoFa.Create(id, code, DateTime.UtcNow.AddMinutes(5));

            await redisService.SetStringToCacheAsync(key, twoFaModelForRedis);

            await rabbitMqService.SendMessageAsync(publishDtoForRabbit, TwoFaQueueName);
        }

        public async Task<AuthResponseDtoV1> VerifyTwoFaCodeAsync(TwoFaRequestDtoV1 requestDto)
        {
            var key = $"twoFa:{requestDto.Id}";
            var redisData = await redisService.GetStringFromCacheAsync<RedisTwoFa>(key);
            if (redisData is null)
                throw new CacheNotFoundException();
            if (redisData.CodeExpire > DateTime.Now)
                throw new TwoFaCodeExpireException();

            await redisService.RemoveStringFromCacheAsync(key);

            var userEntity = await userRepository.GetByIdAsync(requestDto.Id);
            if (userEntity is not null)
            {
                var user = mapper.Map<UserV1>(userEntity);

                var refreshToken = jWtProvider.GenerateRefreshToken();

                var isOk = await userRepository.UpdateUserAsync(user.Id, u =>
                {
                    u.RefreshToken = refreshToken;
                    u.RefreshTokenExpiry = DateTime.UtcNow.AddDays(options.RefreshTokenExpiryDays);
                });
                if (isOk)
                {
                    var token = jWtProvider.GenerateToken(user);

                    return new AuthResponseDtoV1() { UserId = user.Id, AccessToken = token, RefreshToken = refreshToken, RequiresTwoFa = user.Is2FaEnabled };
                }
                throw new DbUpdateException();
            }
            throw new UserNotFoundException();
        }
    }
}
