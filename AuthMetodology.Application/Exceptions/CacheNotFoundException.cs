namespace AuthMetodology.Application.Exceptions
{
    public class CacheNotFoundException : Exception
    {
        public CacheNotFoundException()
        {
            
        }

        public CacheNotFoundException(string message)
            : base(message)
        {
            
        }
    }
}
