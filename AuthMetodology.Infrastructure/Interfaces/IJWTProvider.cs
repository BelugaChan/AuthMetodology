using AuthMetodology.Logic.Models.v1;

namespace AuthMetodology.Infrastructure.Interfaces
{
    public interface IJWTProvider
    {
        string GenerateToken(UserV1 user);

        string GenerateRefreshToken();

        string GenerateResetToken();
    }
}
