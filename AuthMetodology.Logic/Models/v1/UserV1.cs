﻿using AuthMetodology.Logic.Enums;

namespace AuthMetodology.Logic.Models.v1
{
    public class UserV1
    {
        public Guid Id { get; set; }

        public string PasswordHash { get; private set; }

        public string UserName { get; set; }

        public string Email { get; private set; }

        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }

        public string IdGoogle { get; set; }

        public bool Is2FaEnabled { get; set; }

        public bool IsEmailConfirmed { get; set; }

        public UserRole UserRole { get; private set; } = UserRole.User;

        public string ResetPasswordToken { get; set; }
        public DateTime ResetPasswordTokenExpiry { get; set; }

        public static UserV1 Create(Guid id, string passwordHash, string userName, string email, string refreshToken, DateTime refreshTokenExpiry, string idGoogle, bool is2FaEnabled, bool isEmailConfirmed, UserRole userRole, string resetPasswordToken, DateTime resetPasswordTokenExpiry) =>
            new UserV1() { Id=id,PasswordHash=passwordHash,UserName=userName,Email=email,RefreshToken=refreshToken,RefreshTokenExpiry=refreshTokenExpiry,IdGoogle=idGoogle,Is2FaEnabled=is2FaEnabled,IsEmailConfirmed=isEmailConfirmed, UserRole=userRole, ResetPasswordToken=resetPasswordToken,ResetPasswordTokenExpiry= resetPasswordTokenExpiry };
    }
}
