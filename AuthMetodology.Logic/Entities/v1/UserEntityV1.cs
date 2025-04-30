using AuthMetodology.Logic.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthMetodology.Logic.Entities.v1
{
    [Table("auth", Schema = "authSchema")]
    public class UserEntityV1
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("monkey")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("userName")]
        public string UserName { get; set; } = string.Empty;

        [Column("email")]
        public required string Email { get; set; }

        [Column("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [Column("refreshTokenExpiry")]
        public DateTime RefreshTokenExpiry { get; set; }

        [Column("idGoogle")]
        public string IdGoogle { get; set; } = string.Empty;

        [Column("is2FAEnabled")]
        public bool Is2FaEnabled { get; set; } = false;

        [Column("isEmailConfirmed")]
        public bool IsEmailConfirmed { get; set; } = false;

        [Column("userRole")]
        public UserRole UserRole { get; set; } = UserRole.User;

        [Column("resetPasswordToken")]
        public string ResetPasswordToken { get; set; } = string.Empty;

        [Column("resetPasswordTokenExpiry")]
        public DateTime ResetPasswordTokenExpiry { get; set; }

        public static UserEntityV1 Create(Guid id,string passwordHash, string userName, string email, string refreshToken, DateTime refreshTokenExpiry, string idGoogle, bool is2FaEnabled, bool isEmailConfirmed, string resetPasswordToken, DateTime resetPasswordTokenExpiry)
        {
            return new UserEntityV1() { Email = email, 
                                        Id = id, 
                                        UserName = userName,
                                        PasswordHash = passwordHash,
                                        RefreshToken = refreshToken,
                                        RefreshTokenExpiry = refreshTokenExpiry,
                                        IdGoogle = idGoogle,
                                        Is2FaEnabled = is2FaEnabled,
                                        IsEmailConfirmed = isEmailConfirmed,
                                        ResetPasswordToken = resetPasswordToken,
                                        ResetPasswordTokenExpiry = resetPasswordTokenExpiry};
        }
    }
}
