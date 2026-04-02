using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Exceptions;
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
    public class ResetPasswordServiceTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IJWTProvider> _jwtProviderMock;
        private readonly Mock<IRabbitMqPublisherBase<RabbitMqResetPasswordPublish>> _rabbitMqMock;
        private readonly Mock<IOptions<ResetPasswordOptions>> _optionsMock;
        private readonly ResetPasswordService _sut;

        private readonly ResetPasswordOptions _options = new() { ResetTokenExpiryHours = 1 };

        public ResetPasswordServiceTests()
        {
            _mapperMock          = new Mock<IMapper>();
            _userRepositoryMock  = new Mock<IUserRepository>();
            _jwtProviderMock     = new Mock<IJWTProvider>();
            _rabbitMqMock        = new Mock<IRabbitMqPublisherBase<RabbitMqResetPasswordPublish>>();
            _optionsMock         = new Mock<IOptions<ResetPasswordOptions>>();
            _optionsMock.SetupGet(o => o.Value).Returns(_options);

            _sut = new ResetPasswordService(
                _mapperMock.Object,
                _userRepositoryMock.Object,
                _optionsMock.Object,
                _jwtProviderMock.Object,
                _rabbitMqMock.Object
            );
        }

        // ─── Хелпер ──────────────────────────────────────────────────────────────

        private static UserV1 MakeUser(
            Guid? id = null,
            string email = "user@test.com",
            string resetToken = "",
            DateTime resetTokenExpiry = default)
            => UserV1.Create(
                id ?? Guid.NewGuid(),
                "hashed_pw",
                "username",
                email,
                "refresh",
                DateTime.UtcNow.AddDays(7),
                string.Empty,
                false,
                true,
                UserRole.User,
                resetToken,
                resetTokenExpiry);

        // ─── ForgotPasswordAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task ForgotPasswordAsync_UserExists_UpdatesTokenAndPublishesMessage()
        {
            // Arrange
            var request = new ForgotPasswordRequestDtoV1 { Email = "user@test.com" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateResetToken()).Returns("reset_token_xyz");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqResetPasswordPublish>(), It.IsAny<CancellationToken>()));

            // Act
            await _sut.ForgotPasswordAsync(request, "https", "example.com");

            // Assert
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _rabbitMqMock.Verify(
                r => r.SendEventAsync(
                    It.Is<RabbitMqResetPasswordPublish>(m => m.Email == request.Email),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_DoesNothing()
        {
            // Arrange
            var request = new ForgotPasswordRequestDtoV1 { Email = "ghost@test.com" };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act
            await _sut.ForgotPasswordAsync(request, "https", "example.com");

            // Assert — никакие зависимости не вызываются
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _rabbitMqMock.Verify(
                r => r.SendEventAsync(It.IsAny<RabbitMqResetPasswordPublish>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var request = new ForgotPasswordRequestDtoV1 { Email = "user@test.com" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateResetToken()).Returns("token");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(
                () => _sut.ForgotPasswordAsync(request, "https", "example.com"));
        }

        [Fact]
        public async Task ForgotPasswordAsync_ResetLinkContainsCorrectToken()
        {
            // Arrange
            const string expectedToken = "my_reset_token";
            var request = new ForgotPasswordRequestDtoV1 { Email = "user@test.com" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, resetToken: expectedToken);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateResetToken()).Returns(expectedToken);
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RabbitMqResetPasswordPublish? captured = null;
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqResetPasswordPublish>(), It.IsAny<CancellationToken>()))
                .Callback<RabbitMqResetPasswordPublish, CancellationToken>((msg, _) => captured = msg);

            // Act
            await _sut.ForgotPasswordAsync(request, "https", "example.com");

            // Assert
            Assert.NotNull(captured);
            Assert.Contains(expectedToken, captured!.Link);
        }

        // ─── ResetPasswordAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ClearsTokenSuccessfully()
        {
            // Arrange
            const string token = "valid_token";
            var request = new ResetPasswordRequestDtoV1 { Token = token, Password = "NewPass1!", ConfirmPassword = "NewPass1!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = "u@t.com" };
            // ResetPasswordTokenExpiry < UtcNow → проверка пройдёт (сервис бросает при >= UtcNow)
            var userModel = MakeUser(userEntity.Id, resetToken: token, resetTokenExpiry: DateTime.UtcNow.AddHours(-1));

            _userRepositoryMock
                .Setup(r => r.GetByResetPasswordTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userEntity.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _sut.ResetPasswordAsync(request);

            // Assert
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(userEntity.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenStillValid_ThrowsExpiredResetPasswordTokenException()
        {
            // Arrange — ResetPasswordTokenExpiry >= UtcNow → сервис считает токен невалидным
            const string token = "valid_token";
            var request = new ResetPasswordRequestDtoV1 { Token = token, Password = "NewPass1!", ConfirmPassword = "NewPass1!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = "u@t.com" };
            var userModel  = MakeUser(userEntity.Id, resetToken: token, resetTokenExpiry: DateTime.UtcNow.AddHours(1));

            _userRepositoryMock
                .Setup(r => r.GetByResetPasswordTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);

            // Act & Assert
            await Assert.ThrowsAsync<ExpiredResetPasswordTokenException>(() => _sut.ResetPasswordAsync(request));
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenMismatch_ThrowsIncorrectResetPasswordTokenException()
        {
            // Arrange
            var request = new ResetPasswordRequestDtoV1 { Token = "wrong_token", Password = "NewPass1!", ConfirmPassword = "NewPass1!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = "u@t.com" };
            // Токен в модели другой → мисматч; expiry в прошлом чтобы миновать первую проверку
            var userModel  = MakeUser(userEntity.Id, resetToken: "correct_token", resetTokenExpiry: DateTime.UtcNow.AddHours(-1));

            _userRepositoryMock
                .Setup(r => r.GetByResetPasswordTokenAsync("wrong_token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);

            // Act & Assert
            await Assert.ThrowsAsync<IncorrectResetPasswordTokenException>(() => _sut.ResetPasswordAsync(request));
        }

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.GetByResetPasswordTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(
                () => _sut.ResetPasswordAsync(new ResetPasswordRequestDtoV1
                {
                    Token = "t", Password = "P@ss1234", ConfirmPassword = "P@ss1234"
                }));
        }

        [Fact]
        public async Task ResetPasswordAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            const string token = "valid_token";
            var request = new ResetPasswordRequestDtoV1 { Token = token, Password = "NewPass1!", ConfirmPassword = "NewPass1!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = "u@t.com" };
            var userModel  = MakeUser(userEntity.Id, resetToken: token, resetTokenExpiry: DateTime.UtcNow.AddHours(-1));

            _userRepositoryMock
                .Setup(r => r.GetByResetPasswordTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userEntity.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.ResetPasswordAsync(request));
        }
    }
}
