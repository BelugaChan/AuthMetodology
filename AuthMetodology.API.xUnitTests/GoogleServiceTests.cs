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
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace AuthMetodology.Tests.Services
{
    public class GoogleServiceTests
    {
        private readonly Mock<IJWTProvider> _jwtProviderMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IOptions<GoogleOptions>> _googleOptionsMock;
        private readonly Mock<IOptions<JWTOptions>> _jwtOptionsMock;
        private readonly GoogleService _sut;

        private readonly JWTOptions _jwtOptions    = new() { RefreshTokenExpiryDays = 7, AccessTokenExpiryMinutes = 15 };
        private readonly GoogleOptions _googleOpts = new() { ClientId = "test-client-id" };

        public GoogleServiceTests()
        {
            _jwtProviderMock    = new Mock<IJWTProvider>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _mapperMock         = new Mock<IMapper>();
            _googleOptionsMock  = new Mock<IOptions<GoogleOptions>>();
            _jwtOptionsMock     = new Mock<IOptions<JWTOptions>>();

            _googleOptionsMock.SetupGet(o => o.Value).Returns(_googleOpts);
            _jwtOptionsMock.SetupGet(o => o.Value).Returns(_jwtOptions);

            _sut = new GoogleService(
                _jwtProviderMock.Object,
                _userRepositoryMock.Object,
                _googleOptionsMock.Object,
                _jwtOptionsMock.Object,
                _mapperMock.Object
            );
        }

        // ─── Хелперы ─────────────────────────────────────────────────────────────

        private static GoogleJsonWebSignature.Payload BuildPayload(
            string email   = "google@test.com",
            string subject = "google_subject_123",
            string name    = "Google User")
            => new GoogleJsonWebSignature.Payload { Email = email, Subject = subject, Name = name };

        private static UserV1 MakeUser(Guid? id = null, string email = "google@test.com", string idGoogle = "google_subject_123")
            => UserV1.Create(
                id ?? Guid.NewGuid(),
                string.Empty,
                "username",
                email,
                "refresh",
                DateTime.UtcNow.AddDays(7),
                idGoogle,
                false,
                true,
                UserRole.User,
                string.Empty,
                default);

        // ─── CreateGoogleUserAsync ────────────────────────────────────────────────

        [Fact]
        public async Task CreateGoogleUserAsync_NewEmail_CreatesUserAndReturnsAuthResponse()
        {
            // Arrange
            var payload = BuildPayload();

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
            _jwtProviderMock.Setup(j => j.GenerateToken(It.IsAny<UserV1>())).Returns("access_token");
            _mapperMock
                .Setup(m => m.Map<UserEntityV1>(It.IsAny<UserV1>()))
                .Returns(new UserEntityV1 { Email = payload.Email });
            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserEntityV1>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.CreateGoogleUserAsync(payload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("access_token", result.AccessToken);
            Assert.False(result.RequiresTwoFa);
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserEntityV1>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateGoogleUserAsync_ExistingEmail_ThrowsExistMailException()
        {
            // Arrange
            var payload = BuildPayload();
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Email = payload.Email });

            // Act & Assert
            await Assert.ThrowsAsync<ExistMailException>(() => _sut.CreateGoogleUserAsync(payload));
        }

        [Fact]
        public async Task CreateGoogleUserAsync_NewUser_GoogleSubjectSetCorrectly()
        {
            // Arrange
            var payload = BuildPayload(subject: "unique_google_sub");
            UserV1? capturedUser = null;

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock
                .Setup(j => j.GenerateToken(It.IsAny<UserV1>()))
                .Callback<UserV1>(u => capturedUser = u)
                .Returns("access");
            _mapperMock
                .Setup(m => m.Map<UserEntityV1>(It.IsAny<UserV1>()))
                .Returns(new UserEntityV1 { Email = payload.Email });
            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserEntityV1>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.CreateGoogleUserAsync(payload);

            // Assert
            Assert.NotNull(capturedUser);
            Assert.Equal("unique_google_sub", capturedUser!.IdGoogle);
            Assert.Equal(payload.Email, capturedUser.Email);
        }

        [Fact]
        public async Task CreateGoogleUserAsync_NewUser_UserNotAddedToRepositoryOnMapperFailure()
        {
            // Arrange
            var payload = BuildPayload();
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(It.IsAny<UserV1>())).Returns("access");
            _mapperMock
                .Setup(m => m.Map<UserEntityV1>(It.IsAny<UserV1>()))
                .Throws(new Exception("Mapper failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.CreateGoogleUserAsync(payload));
            _userRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<UserEntityV1>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ─── LoginGoogleUserAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task LoginGoogleUserAsync_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var payload    = BuildPayload();
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = payload.Email };
            var userModel  = MakeUser(userEntity.Id, payload.Email, payload.Subject);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.LoginGoogleUserAsync(payload);

            // Assert
            Assert.Equal("access", result.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.False(result.RequiresTwoFa);
            Assert.Equal(userModel.Id, result.UserId);
        }

        [Fact]
        public async Task LoginGoogleUserAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            var payload = BuildPayload();
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.LoginGoogleUserAsync(payload));
        }

        [Fact]
        public async Task LoginGoogleUserAsync_WrongGoogleSubject_ThrowsIncorrectGoogleCredentialsException()
        {
            // Arrange
            var payload    = BuildPayload(subject: "real_subject");
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = payload.Email };
            var userModel  = MakeUser(userEntity.Id, payload.Email, idGoogle: "different_subject");

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);

            // Act & Assert
            await Assert.ThrowsAsync<IncorrectGoogleCredentialsException>(() => _sut.LoginGoogleUserAsync(payload));
        }

        [Fact]
        public async Task LoginGoogleUserAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var payload    = BuildPayload();
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = payload.Email };
            var userModel  = MakeUser(userEntity.Id, payload.Email, payload.Subject);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.LoginGoogleUserAsync(payload));
        }

        [Fact]
        public async Task LoginGoogleUserAsync_Success_UpdatesRefreshTokenInRepository()
        {
            // Arrange
            var payload    = BuildPayload();
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = payload.Email };
            var userModel  = MakeUser(userEntity.Id, payload.Email, payload.Subject);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(payload.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("new_access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _sut.LoginGoogleUserAsync(payload);

            // Assert
            _userRepositoryMock.Verify(
                r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
