using AuthMetodology.API.Interfaces;

namespace AuthMetodology.API.CookieCreator
{
    public class CookieTokenCreator : ICookieCreator
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        public CookieTokenCreator(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        public void CreateTokenCookie(string key, string accessToken, DateTime expires)
        {
            httpContextAccessor.HttpContext?.Response.Cookies.Append(key, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expires
                //add expires
            });
        }
    }
}
