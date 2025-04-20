using AuthMetodology.API.Controllers.v1;
using AuthMetodology.API.Interfaces;
using AuthMetodology.Application.DTO.v1;
using AuthMetodology.Application.Interfaces;
using AuthMetodology.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMqPublisher.Interface;

namespace AuthMetodology.API.xUnitTests
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserService> userServiceMock;
        private readonly Mock<IGoogleService> gogleServiceMock;
        private readonly Mock<ICookieCreator> cookieCreatorMock;
        private readonly Mock<IRabbitMqPublisherBase<RabbitMqLogPublish>> logQueueServiceMock;
        private readonly Mock<IResetPasswordService> resetPasswordServiceMock;
        private readonly Mock<ITwoFaService> twoFaServiceMock;
        private readonly Mock<IOptions<JWTOptions>> optionsMock;

        public AuthControllerTests()
        {
            userServiceMock = new Mock<IUserService>();
            gogleServiceMock = new Mock<IGoogleService>();
            cookieCreatorMock = new Mock<ICookieCreator>();
            logQueueServiceMock = new Mock<IRabbitMqPublisherBase<RabbitMqLogPublish>>();
            resetPasswordServiceMock = new Mock<IResetPasswordService>();
            optionsMock = new Mock<IOptions<JWTOptions>>();
            twoFaServiceMock = new Mock<ITwoFaService>();
        }

        //[Fact]
        //public async Task RegisterUser_ReturnsOk_WhenRegistrationIsSuccessful()
        //{
        //    //Arrange
        //    RegisterUserRequestDtoV1 fakeRequest = new() 
        //    {
        //        Email="test@testov.com",
        //        Password="Password123!",
        //        ConfirmPassword="Password123!"
        //    };

        //    AuthResponseDtoV1 fakeResponse = new() 
        //    { 
        //        UserId = Guid.NewGuid(),
        //        AccessToken = "123456789",
        //        RefreshToken = "987654321",
        //        RequiresTwoFa = false                
        //    };

        //    CancellationToken cancellationToken = new CancellationToken();

        //    optionsMock.SetupGet(x => x.Value)
        //        .Returns(new JWTOptions
        //        {
        //            AccessTokenExpiryMinutes = 15,
        //            RefreshTokenExpiryDays = 7
        //        });

        //    userServiceMock.Setup(s => s.RegisterUserAsync(fakeRequest, cancellationToken)).ReturnsAsync(fakeResponse);
        //    // Act
        //    var controller = new AuthController(
        //        userServiceMock.Object,
        //        gogleServiceMock.Object,
        //        cookieCreatorMock.Object,
        //        logQueueServiceMock.Object,
        //        optionsMock.Object,
        //        resetPasswordServiceMock.Object
        //    );
        //    var result = await controller.Register(fakeRequest, cancellationToken);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var returnedUserData = Assert.IsType<AuthResponseDtoV1>(okResult.Value);

        //    Assert.Equal(fakeResponse.AccessToken, returnedUserData.AccessToken);
        //    Assert.Equal(fakeResponse.UserId, returnedUserData.UserId);

        //    logQueueServiceMock.Verify(
        //            x => x.SendEventAsync(
        //                It.Is<RabbitMqLogPublish>(m =>
        //                    m.Message.Contains("POST /api/v1/auth/register was called")),
        //                cancellationToken),
        //            Times.Once
        //        );

        //    cookieCreatorMock.Verify(
        //        x => x.CreateTokenCookie(
        //            "access",
        //            fakeResponse.AccessToken,
        //            It.Is<DateTime>(d => d >= DateTime.UtcNow)
        //        ),    
        //        Times.Once
        //    );
        //    cookieCreatorMock.Verify(
        //        x => x.CreateTokenCookie(
        //            "refresh",
        //            fakeResponse.RefreshToken,
        //            It.Is<DateTime>(d => d >= DateTime.UtcNow)
        //        ),
        //        Times.Once
        //    );
        //}

        [Fact]
        public async Task LoginUser_ReturnsOk_WhenLoginSuccessful()
        {
            //Arrange
            LoginUserRequestDtoV1 fakeRequest = new LoginUserRequestDtoV1() 
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            AuthResponseDtoV1 fakeResponse = new()
            {
                UserId = Guid.NewGuid(),
                AccessToken = "123456789",
                RefreshToken = "987654321",
                RequiresTwoFa = false,
                RequiresConfirmEmail = false
            };

            CancellationToken cancellationToken = new CancellationToken();

            optionsMock.SetupGet(o => o.Value)
                .Returns(new JWTOptions
                {
                    AccessTokenExpiryMinutes = 15,
                    RefreshTokenExpiryDays = 7
                });

            userServiceMock.Setup(s => s.LoginAsync(fakeRequest, cancellationToken)).ReturnsAsync(fakeResponse);
            //Act
            var controller = new AuthController(
                userServiceMock.Object,
                gogleServiceMock.Object,
                cookieCreatorMock.Object,
                logQueueServiceMock.Object,
                optionsMock.Object,
                resetPasswordServiceMock.Object,
                twoFaServiceMock.Object
            );

            var result = await controller.Login(fakeRequest, cancellationToken);
            //Assert
            //Проверка того, что логи отправили корректное сообщение
            logQueueServiceMock.Verify(
                e => e.SendEventAsync(
                        It.Is<RabbitMqLogPublish>(
                            m => m.Message.Contains("POST /api/v1/auth/login was called")
                        ),
                    cancellationToken), 
                Times.Once);

            //Проверка того, что в метод CreateTokenCookie были переданы корректные данные.
            cookieCreatorMock.Verify(e => e.CreateTokenCookie(It.Is<string>(e => e.Contains("access")), fakeResponse.AccessToken, It.Is<DateTime>(e => e >= DateTime.UtcNow)), Times.Once);
            cookieCreatorMock.Verify(e => e.CreateTokenCookie(It.Is<string>(e => e.Contains("refresh")), fakeResponse.RefreshToken, It.Is<DateTime>(e => e >= DateTime.UtcNow)), Times.Once);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUserData = Assert.IsType<AuthResponseDtoV1>(okResult.Value);

            //check return data correctness
            Assert.Equal(fakeResponse.UserId, returnedUserData.UserId);
            Assert.Equal(fakeResponse.AccessToken, returnedUserData.AccessToken);
            Assert.Equal(fakeResponse.RefreshToken, returnedUserData.RefreshToken);
            Assert.Equal(fakeResponse.RequiresTwoFa, returnedUserData.RequiresTwoFa);
            Assert.Equal(fakeResponse.RequiresConfirmEmail, returnedUserData.RequiresConfirmEmail);


        }
    }
}
