using AuthMetodology.Infrastructure.Interfaces;

namespace AuthMetodology.Infrastructure.Providers;

public class TwoFaProvider : ITwoFaProvider
{
    public string GenerateTwoFaCode()
    {
        Random random = new Random();
        return random.Next(0, 1000000).ToString("D6");
    }
}