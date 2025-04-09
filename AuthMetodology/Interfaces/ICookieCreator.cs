namespace AuthMetodology.API.Interfaces
{
    public interface ICookieCreator
    {
        public void CreateTokenCookie(string key, string token, DateTime expires);
    }
}
