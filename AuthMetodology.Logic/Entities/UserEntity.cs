using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthMetodology.Logic.Entities
{
    [Table("auth", Schema = "authSchema")]
    public class UserEntity
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("monkey")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("email")]
        public required string Email { get; set; }

        [Column("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [Column("refreshTokenExpiry")]
        public DateTime RefreshTokenExpiry { get; set; }

        public static UserEntity Create(Guid id,string passwordHash, string email)
        {
            return new UserEntity() { Email = email, Id = id, PasswordHash = passwordHash };
        }
    }
}
