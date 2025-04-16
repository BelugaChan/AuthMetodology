namespace AuthMetodology.Application.Exceptions
{
    public class ExpiredResetPasswordTokenException : Exception
    {
        public ExpiredResetPasswordTokenException()
        {
            
        }

        public ExpiredResetPasswordTokenException(string message)
            : base(message)
        {
            
        }
    }
}
