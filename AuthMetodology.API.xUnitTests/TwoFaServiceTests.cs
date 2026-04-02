using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Exceptions;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Application.Services;
using AuthMetodology.Infrastructure.Interfaces;
using AuthMetodology.Infrastructure.Models;
using AuthMetodology.Logic.Entities.v1;
using AuthMetodology.Logic.Enums;
using AuthMetodology.Logic.Models.v1;
using AuthMetodology.Persistence.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.Tests.Services
{
    public class TwoFaServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITwoFaProvider> _twoFaProviderMock;
        private readonly Mock<IRabbitMqPublisherBase<RabbitMqTwoFaPublish>> _rabbitMqMock;
        private readonly Mock<IRedisService> _redisServiceMock;
        private readonly Mock<IJWTProvider> _jwtProviderMock;
        private readonly Mock<IOptions<JWTOptions>> _optionsMock;
        private readonly TwoFaService _sut;

        private readonly JWTOptions _jwtOptions = new() { RefreshTokenExpiryDays = 7, AccessTokenExpiryMinutes = 15 };

        public TwoFaServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _mapperMock         = new Mock<IMapper>();
            _twoFaProviderMock  = new Mock<ITwoFaProvider>();
            _rabbitMqMock       = new Mock<IRabbitMqPublisherBase<RabbitMqTwoFaPublish>>();
            _redisServiceMock   = new Mock<IRedisService>();
            _jwtProviderMock    = new Mock<IJWTProvider>();
            _optionsMock        = new Mock<IOptions<JWTOptions>>();
            _optionsMock.SetupGet(o => o.Value).Returns(_jwtOptions);

            _sut = new TwoFaService(
                _userRepositoryMock.Object,
                _mapperMock.Object,
                _twoFaProviderMock.Object,
                _rabbitMqMock.Object,
                _redisServiceMock.Object,
                _jwtProviderMock.Object,
                _optionsMock.Object
            );
        }

        // ─── Хелпер ──────────────────────────────────────────────────────────────

        private static UserV1 MakeUser(Guid? id = null, bool is2FaEnabled = false, UserRole role = UserRole.User)
            => UserV1.Create(
                id ?? Guid.NewGuid(),
                "hashed_pw",
                "username",
                "user@test.com",
                "refresh",
                DateTime.UtcNow.AddDays(7),
                string.Empty,
                is2FaEnabled,
                true,
                role,
                string.Empty,
                default);

        // ─── EnableTwoFaStatusAsync ───────────────────────────────────────────────

        [Fact]
        public async Task EnableTwoFaStatusAsync_UserFound_TwoFaDisabled_EnablesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Id = userId, Email = "u@t.com", Is2FaEnabled = false });
            _mapperMock
                .Setup(m => m.Map<UserV1>(It.IsAny<UserEntityV1>()))
                .Returns(MakeUser(userId, is2FaEnabled: false));
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _sut.EnableTwoFaStatusAsync(userId);

            // Assert
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EnableTwoFaStatusAsync_TwoFaAlreadyEnabled_ThrowsInvalidTwoFaStatusException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Id = userId, Email = "u@t.com", Is2FaEnabled = true });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidTwoFaStatusException>(() => _sut.EnableTwoFaStatusAsync(userId));
        }

        [Fact]
        public async Task EnableTwoFaStatusAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.EnableTwoFaStatusAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task EnableTwoFaStatusAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Id = userId, Email = "u@t.com", Is2FaEnabled = false });
            _mapperMock
                .Setup(m => m.Map<UserV1>(It.IsAny<UserEntityV1>()))
                .Returns(MakeUser(userId));
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.EnableTwoFaStatusAsync(userId));
        }

        // ─── DisableTwoFaStatusAsync ──────────────────────────────────────────────

        [Fact]
        public async Task DisableTwoFaStatusAsync_UserFound_TwoFaEnabled_DisablesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Id = userId, Email = "u@t.com", Is2FaEnabled = true });
            _mapperMock
                .Setup(m => m.Map<UserV1>(It.IsAny<UserEntityV1>()))
                .Returns(MakeUser(userId, is2FaEnabled: true));
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _sut.DisableTwoFaStatusAsync(userId);

            // Assert
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DisableTwoFaStatusAsync_TwoFaAlreadyDisabled_ThrowsInvalidTwoFaStatusException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Id = userId, Email = "u@t.com", Is2FaEnabled = false });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidTwoFaStatusException>(() => _sut.DisableTwoFaStatusAsync(userId));
        }

        [Fact]
        public async Task DisableTwoFaStatusAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.DisableTwoFaStatusAsync(Guid.NewGuid()));
        }

        // ─── SendVerificationCodeAsync ────────────────────────────────────────────

        [Fact]
        public async Task SendVerificationCodeAsync_SavesToRedisAndPublishesToRabbit()
        {
            // Arrange
            var request = new SendVerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Mail = "user@test.com" };
            _twoFaProviderMock.Setup(t => t.GenerateTwoFaCode()).Returns("123456");
            _redisServiceMock
                .Setup(r => r.SetStringToCacheAsync(It.IsAny<string>(), It.IsAny<RedisTwoFa>()))
                .Returns(Task.CompletedTask);
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqTwoFaPublish>(), It.IsAny<CancellationToken>()));

            // Act
            await _sut.SendVerificationCodeAsync(request, "2fa");

            // Assert
            _redisServiceMock.Verify(
                r => r.SetStringToCacheAsync(
                    It.Is<string>(k => k.Contains(request.Id.ToString())),
                    It.IsAny<RedisTwoFa>()),
                Times.Once);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_KeyContainsContext()
        {
            // Arrange
            var request = new SendVerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Mail = "user@test.com" };
            _twoFaProviderMock.Setup(t => t.GenerateTwoFaCode()).Returns("000000");
            _redisServiceMock
                .Setup(r => r.SetStringToCacheAsync(It.IsAny<string>(), It.IsAny<RedisTwoFa>()))
                .Returns(Task.CompletedTask);
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqTwoFaPublish>(), It.IsAny<CancellationToken>()));

            // Act
            await _sut.SendVerificationCodeAsync(request, "confirm");

            // Assert — ключ должен содержать контекст "confirm"
            _redisServiceMock.Verify(
                r => r.SetStringToCacheAsync(
                    It.Is<string>(k => k.StartsWith("confirm:")),
                    It.IsAny<RedisTwoFa>()),
                Times.Once);
        }

        // ─── ResendVerificationCodeAsync ──────────────────────────────────────────

        [Fact]
        public async Task ResendVerificationCodeAsync_UserExists_RemovesOldAndSendsNew()
        {
            // Arrange
            var request = new ReSendVerificationCodeRequestDtoV1 { Email = "user@test.com" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _twoFaProviderMock.Setup(t => t.GenerateTwoFaCode()).Returns("654321");
            _redisServiceMock.Setup(r => r.RemoveStringFromCacheAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _redisServiceMock
                .Setup(r => r.SetStringToCacheAsync(It.IsAny<string>(), It.IsAny<RedisTwoFa>()))
                .Returns(Task.CompletedTask);
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqTwoFaPublish>(), It.IsAny<CancellationToken>()));

            // Act
            await _sut.ResendVerificationCodeAsync(request);

            // Assert
            _redisServiceMock.Verify(r => r.RemoveStringFromCacheAsync(It.IsAny<string>()), Times.Once);
            _redisServiceMock.Verify(r => r.SetStringToCacheAsync(It.IsAny<string>(), It.IsAny<RedisTwoFa>()), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationCodeAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(
                () => _sut.ResendVerificationCodeAsync(new ReSendVerificationCodeRequestDtoV1 { Email = "ghost@test.com" }));
        }

        // ─── VerifyCodeAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task VerifyCodeAsync_ValidCode_ReturnsAuthResponse()
        {
            // Arrange
            var request = new VerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Code = "123456" };
            // CodeExpire < DateTime.Now → код считается валидным (логика: CodeExpire > Now → expired)
            var redisData = new RedisTwoFa { Id = request.Id, Code = request.Code, CodeExpire = DateTime.Now.AddMinutes(-1) };
            var userEntity = new UserEntityV1 { Id = request.Id, Email = "u@t.com" };
            var userModel  = MakeUser(request.Id, is2FaEnabled: true);

            _redisServiceMock
                .Setup(r => r.GetStringFromCacheAsync<RedisTwoFa>(It.IsAny<string>()))
                .ReturnsAsync(redisData);
            _redisServiceMock.Setup(r => r.RemoveStringFromCacheAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(request.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.VerifyCodeAsync(request);

            // Assert
            Assert.Equal("access", result.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.Equal(request.Id, result.UserId);
        }

        [Fact]
        public async Task VerifyCodeAsync_CacheNotFound_ThrowsCacheNotFoundException()
        {
            // Arrange
            _redisServiceMock
                .Setup(r => r.GetStringFromCacheAsync<RedisTwoFa>(It.IsAny<string>()))
                .ReturnsAsync((RedisTwoFa?)null);

            // Act & Assert
            await Assert.ThrowsAsync<CacheNotFoundException>(
                () => _sut.VerifyCodeAsync(new VerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Code = "000000" }));
        }

        [Fact]
        public async Task VerifyCodeAsync_CodeNotYetExpired_ThrowsTwoFaCodeExpireException()
        {
            // Arrange — CodeExpire > DateTime.Now → сервис считает код просроченным и бросает исключение
            var request = new VerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Code = "123456" };
            var redisData = new RedisTwoFa
            {
                Id = request.Id,
                Code = request.Code,
                CodeExpire = DateTime.Now.AddMinutes(10)
            };
            _redisServiceMock
                .Setup(r => r.GetStringFromCacheAsync<RedisTwoFa>(It.IsAny<string>()))
                .ReturnsAsync(redisData);

            // Act & Assert
            await Assert.ThrowsAsync<TwoFaCodeExpireException>(() => _sut.VerifyCodeAsync(request));
        }

        [Fact]
        public async Task VerifyCodeAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            var request = new VerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Code = "123456" };
            var redisData = new RedisTwoFa { Id = request.Id, Code = request.Code, CodeExpire = DateTime.Now.AddMinutes(-1) };

            _redisServiceMock
                .Setup(r => r.GetStringFromCacheAsync<RedisTwoFa>(It.IsAny<string>()))
                .ReturnsAsync(redisData);
            _redisServiceMock.Setup(r => r.RemoveStringFromCacheAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.VerifyCodeAsync(request));
        }

        [Fact]
        public async Task VerifyCodeAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var request = new VerificationCodeRequestDtoV1 { Id = Guid.NewGuid(), Code = "123456" };
            var redisData = new RedisTwoFa { Id = request.Id, Code = request.Code, CodeExpire = DateTime.Now.AddMinutes(-1) };
            var userEntity = new UserEntityV1 { Id = request.Id, Email = "u@t.com" };
            var userModel  = MakeUser(request.Id);

            _redisServiceMock
                .Setup(r => r.GetStringFromCacheAsync<RedisTwoFa>(It.IsAny<string>()))
                .ReturnsAsync(redisData);
            _redisServiceMock.Setup(r => r.RemoveStringFromCacheAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(request.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.VerifyCodeAsync(request));
        }
    }
}
