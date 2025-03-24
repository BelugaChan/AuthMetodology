using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthMetodology.Logic.Models
{
    public class User
    {
        private User(Guid id, string passwordHash, string email, string refreshToken, DateTime refreshTokenExpiry)
        {
            Id = id;
            PasswordHash = passwordHash;
            Email = email;
            RefreshToken = refreshToken;
            RefreshTokenExpiry = refreshTokenExpiry;
        }

        public Guid Id { get; set; }

        public string PasswordHash { get; private set; }

        public string Email { get; private set; }

        public string RefreshToken { get; set; }

        public DateTime RefreshTokenExpiry { get; set; }

        public static User Create(Guid id, string passwordHash, string email, string refreshToken, DateTime refreshTokenExpiry) =>
            new User(id, passwordHash, email, refreshToken, refreshTokenExpiry);
    }
}
