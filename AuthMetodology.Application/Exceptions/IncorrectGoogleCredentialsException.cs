namespace AuthMetodology.Application.Exceptions
{
    public class IncorrectGoogleCredentialsException : Exception
    {
        public IncorrectGoogleCredentialsException()
        {
        }

        public IncorrectGoogleCredentialsException(string message)
            : base(message)
        {
            
        }
    }
}
