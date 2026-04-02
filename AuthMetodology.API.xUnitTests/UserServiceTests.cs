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
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IJWTProvider> _jwtProviderMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITwoFaService> _twoFaServiceMock;
        private readonly Mock<IRabbitMqPublisherBase<RabbitMqUserRegisterPublish>> _rabbitMqMock;
        private readonly Mock<IOptions<JWTOptions>> _optionsMock;
        private readonly UserService _sut;

        private readonly JWTOptions _jwtOptions = new() { RefreshTokenExpiryDays = 7, AccessTokenExpiryMinutes = 15 };

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtProviderMock    = new Mock<IJWTProvider>();
            _mapperMock         = new Mock<IMapper>();
            _twoFaServiceMock   = new Mock<ITwoFaService>();
            _rabbitMqMock       = new Mock<IRabbitMqPublisherBase<RabbitMqUserRegisterPublish>>();
            _optionsMock        = new Mock<IOptions<JWTOptions>>();
            _optionsMock.SetupGet(o => o.Value).Returns(_jwtOptions);

            _sut = new UserService(
                _rabbitMqMock.Object,
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtProviderMock.Object,
                _optionsMock.Object,
                _mapperMock.Object,
                _twoFaServiceMock.Object
            );
        }

        // ─── Хелпер: создаёт UserV1 через фабричный метод ────────────────────────

        private static UserV1 MakeUser(
            Guid? id = null,
            string email = "user@test.com",
            bool is2FaEnabled = false,
            bool isEmailConfirmed = true,
            UserRole role = UserRole.User)
            => UserV1.Create(
                id ?? Guid.NewGuid(),
                "hashed_pw",
                "username",
                email,
                "old_refresh",
                DateTime.UtcNow.AddDays(7),
                string.Empty,
                is2FaEnabled,
                isEmailConfirmed,
                role,
                string.Empty,
                default);

        // ─── RegisterUserAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterUserAsync_NewEmail_ReturnsAuthResponse()
        {
            // Arrange
            var request = new RegisterUserRequestDtoV1
            {
                Email = "new@test.com",
                UserName = "newuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);
            _passwordHasherMock.Setup(p => p.Generate(request.Password)).Returns("hashed_pw");
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
            _jwtProviderMock.Setup(j => j.GenerateToken(It.IsAny<UserV1>())).Returns("access_token");
            _mapperMock
                .Setup(m => m.Map<UserEntityV1>(It.IsAny<UserV1>()))
                .Returns(new UserEntityV1 { Email = request.Email });
            _rabbitMqMock
                .Setup(r => r.SendEventAsync(It.IsAny<RabbitMqUserRegisterPublish>(), It.IsAny<CancellationToken>()));
            _userRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserEntityV1>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.RegisterUserAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("access_token", result.AccessToken);
            Assert.Equal("refresh_token", result.RefreshToken);
            Assert.Equal(UserRole.User, result.UserRole);
        }

        [Fact]
        public async Task RegisterUserAsync_ExistingEmail_ThrowsExistMailException()
        {
            // Arrange
            var request = new RegisterUserRequestDtoV1
            {
                Email = "exists@test.com",
                UserName = "user",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Email = request.Email });

            // Act & Assert
            await Assert.ThrowsAsync<ExistMailException>(() => _sut.RegisterUserAsync(request));
        }

        // ─── LoginAsync ───────────────────────────────────────────────────────────

        [Fact]
        public async Task LoginAsync_ValidCredentials_NoTwoFa_ReturnsAuthResponse()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "user@test.com", Password = "Password123!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, is2FaEnabled: false, isEmailConfirmed: true);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _passwordHasherMock.Setup(p => p.Verify(request.Password, userModel.PasswordHash)).Returns(true);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.Equal("access", result.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
            Assert.False(result.RequiresTwoFa);
            Assert.False(result.RequiresConfirmEmail);
        }

        [Fact]
        public async Task LoginAsync_EmailNotFound_ThrowsIncorrectMailException()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "ghost@test.com", Password = "Password123!" };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<IncorrectMailException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsIncorrectPasswordException()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "user@test.com", Password = "WrongPass1!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, isEmailConfirmed: true);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _passwordHasherMock.Setup(p => p.Verify(request.Password, userModel.PasswordHash)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<IncorrectPasswordException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_EmailNotConfirmed_ReturnsDtoWithRequiresConfirmEmail()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "unconfirmed@test.com", Password = "Password123!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, isEmailConfirmed: false);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _twoFaServiceMock
                .Setup(t => t.SendVerificationCodeAsync(It.IsAny<SendVerificationCodeRequestDtoV1>(), "confirm"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.True(result.RequiresConfirmEmail);
            Assert.Empty(result.AccessToken);
            _twoFaServiceMock.Verify(
                t => t.SendVerificationCodeAsync(
                    It.Is<SendVerificationCodeRequestDtoV1>(d => d.Id == userModel.Id), "confirm"),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_TwoFaEnabled_ReturnsDtoWithRequiresTwoFa()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "twofa@test.com", Password = "Password123!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, is2FaEnabled: true, isEmailConfirmed: true);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _passwordHasherMock.Setup(p => p.Verify(request.Password, userModel.PasswordHash)).Returns(true);
            _twoFaServiceMock
                .Setup(t => t.SendVerificationCodeAsync(It.IsAny<SendVerificationCodeRequestDtoV1>(), "2fa"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.True(result.RequiresTwoFa);
            Assert.Empty(result.AccessToken);
        }

        [Fact]
        public async Task LoginAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var request = new LoginUserRequestDtoV1 { Email = "user@test.com", Password = "Password123!" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email, is2FaEnabled: false, isEmailConfirmed: true);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _passwordHasherMock.Setup(p => p.Verify(request.Password, userModel.PasswordHash)).Returns(true);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.LoginAsync(request));
        }

        // ─── UpdateUserTokensAsync ────────────────────────────────────────────────

        [Fact]
        public async Task UpdateUserTokensAsync_ValidToken_ReturnsRefreshResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new RefreshTokenRequestDtoV1 { AccessToken = "old_access", RefreshToken = "valid_refresh" };
            var userEntity = new UserEntityV1
            {
                Id = userId,
                Email = "user@test.com",
                RefreshToken = "valid_refresh",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            var userModel = MakeUser(userId);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("new_access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userId, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.UpdateUserTokensAsync(userId, request);

            // Assert
            Assert.Equal("new_access", result.AccessToken);
            Assert.Equal("new_refresh", result.RefreshToken);
        }

        [Fact]
        public async Task UpdateUserTokensAsync_UserNotFound_ThrowsInvalidRefreshTokenException()
        {
            // Arrange
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => _sut.UpdateUserTokensAsync(Guid.NewGuid(),
                    new RefreshTokenRequestDtoV1 { AccessToken = "a", RefreshToken = "b" }));
        }

        [Fact]
        public async Task UpdateUserTokensAsync_TokenMismatch_ThrowsInvalidRefreshTokenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userEntity = new UserEntityV1
            {
                Id = userId,
                Email = "user@test.com",
                RefreshToken = "correct_token",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(userEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => _sut.UpdateUserTokensAsync(userId,
                    new RefreshTokenRequestDtoV1 { AccessToken = "a", RefreshToken = "wrong_token" }));
        }

        [Fact]
        public async Task UpdateUserTokensAsync_ExpiredToken_ThrowsInvalidRefreshTokenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userEntity = new UserEntityV1
            {
                Id = userId,
                Email = "user@test.com",
                RefreshToken = "valid_refresh",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) // истёк
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(userEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(
                () => _sut.UpdateUserTokensAsync(userId,
                    new RefreshTokenRequestDtoV1 { AccessToken = "a", RefreshToken = "valid_refresh" }));
        }

        // ─── ConfirmRegisterUserAsync ─────────────────────────────────────────────

        [Fact]
        public async Task ConfirmRegisterUserAsync_ExistingUser_ReturnsAuthResponse()
        {
            // Arrange
            var request = new ConfirmRegistrationRequestDtoV1 { Email = "user@test.com", RegistrationCode = "123456" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _jwtProviderMock.Setup(j => j.GenerateToken(userModel)).Returns("access");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.ConfirmRegisterUserAsync(request);

            // Assert
            Assert.Equal("access", result.AccessToken);
            Assert.Equal("refresh", result.RefreshToken);
        }

        [Fact]
        public async Task ConfirmRegisterUserAsync_UserNotFound_ThrowsUserNotFoundException()
        {
            // Arrange
            var request = new ConfirmRegistrationRequestDtoV1 { Email = "ghost@test.com", RegistrationCode = "123456" };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.ConfirmRegisterUserAsync(request));
        }

        [Fact]
        public async Task ConfirmRegisterUserAsync_DbUpdateFails_ThrowsDbUpdateException()
        {
            // Arrange
            var request = new ConfirmRegistrationRequestDtoV1 { Email = "user@test.com", RegistrationCode = "123456" };
            var userEntity = new UserEntityV1 { Id = Guid.NewGuid(), Email = request.Email };
            var userModel  = MakeUser(userEntity.Id, request.Email);

            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userEntity);
            _mapperMock.Setup(m => m.Map<UserV1>(userEntity)).Returns(userModel);
            _jwtProviderMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
            _userRepositoryMock
                .Setup(r => r.UpdateUserAsync(userModel.Id, It.IsAny<Action<UserEntityV1>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.ConfirmRegisterUserAsync(request));
        }

        // ─── InitiateRegisterUserAsync ────────────────────────────────────────────

        [Fact]
        public async Task InitiateRegisterUserAsync_ExistingEmail_ThrowsExistMailException()
        {
            // Arrange
            var request = new RegisterUserRequestDtoV1
            {
                Email = "exists@test.com", UserName = "user",
                Password = "P@ss1234", ConfirmPassword = "P@ss1234"
            };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserEntityV1 { Email = request.Email });

            // Act & Assert
            await Assert.ThrowsAsync<ExistMailException>(() => _sut.InitiateRegisterUserAsync(request));
        }

        [Fact]
        public async Task InitiateRegisterUserAsync_UsernameExists_ThrowsUsernameExistsException()
        {
            // Arrange
            var request = new RegisterUserRequestDtoV1
            {
                Email = "new@test.com", UserName = "takenuser",
                Password = "P@ss1234", ConfirmPassword = "P@ss1234"
            };
            _userRepositoryMock
                .Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserEntityV1?)null);
            _userRepositoryMock
                .Setup(r => r.IsUsernameExists(request.UserName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<UsernameExistsException>(() => _sut.InitiateRegisterUserAsync(request));
        }
    }
}
