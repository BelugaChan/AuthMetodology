namespace AuthMetodology.Infrastructure.Interfaces;

public interface ITwoFaProvider
{
    string GenerateTwoFaCode();
}