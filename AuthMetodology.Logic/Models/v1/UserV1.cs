using AuthMetodology.Logic.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthMetodology.Logic.Models.v1
{
    public class UserV1
    {
        private UserV1(Guid id, string passwordHash, string email, string refreshToken, DateTime refreshTokenExpiry, string idGoogle, bool is2FaEnabled, string resetPasswordToken, DateTime resetTokenPasswordExpire)
        {
            Id = id;
            PasswordHash = passwordHash;
            Email = email;
            RefreshToken = refreshToken;
            RefreshTokenExpiry = refreshTokenExpiry;
            IdGoogle = idGoogle;
            Is2FaEnabled = is2FaEnabled;
            ResetPasswordToken = resetPasswordToken;
            ResetPasswordTokenExpiry = resetTokenPasswordExpire;
        }

        public Guid Id { get; set; }

        public string PasswordHash { get; private set; }

        public string Email { get; private set; }

        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }

        public string IdGoogle { get; set; }

        public bool Is2FaEnabled { get; set; }

        public UserRole UserRole { get; private set; } = UserRole.User;

        public string ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenExpiry { get; set; }

        public static UserV1 Create(Guid id, string passwordHash, string email, string refreshToken, DateTime refreshTokenExpiry, string idGoogle, bool is2FaEnabled, string resetPasswordToken, DateTime resetTokenPasswordExpire) =>
            new UserV1(id, passwordHash, email, refreshToken, refreshTokenExpiry, idGoogle, is2FaEnabled, resetPasswordToken, resetTokenPasswordExpire);
    }
}
