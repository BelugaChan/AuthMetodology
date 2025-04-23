using AuthMetodology.Logic.Enums;

namespace AuthMetodology.Logic.Models.v1
{
    public class UserV1
    {
        public Guid Id { get; set; }

        public string PasswordHash { get; private set; }

        public string Email { get; private set; }

        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }

        public string IdGoogle { get; set; }

        public bool Is2FaEnabled { get; set; }

        public bool IsEmailConfirmed { get; set; }

        public UserRole UserRole { get; private set; } = UserRole.User;

        public string ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenExpiry { get; set; }

        public static UserV1 Create(Guid id, string passwordHash, string email, string refreshToken, DateTime refreshTokenExpiry, string idGoogle, bool is2FaEnabled, bool isEmailConfirmed, string resetPasswordToken, DateTime resetPasswordTokenExpiry) =>
            new UserV1() { Id=id,PasswordHash=passwordHash,Email=email,RefreshToken=refreshToken,RefreshTokenExpiry=refreshTokenExpiry,IdGoogle=idGoogle,Is2FaEnabled=is2FaEnabled,IsEmailConfirmed=isEmailConfirmed,ResetPasswordToken=resetPasswordToken,ResetPasswordTokenExpiry= resetPasswordTokenExpiry };
    }
}
