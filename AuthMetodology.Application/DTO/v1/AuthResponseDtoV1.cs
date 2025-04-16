namespace AuthMetodology.Application.DTO.v1
{
    public class AuthResponseDtoV1
    {
        public required Guid UserId { get; set; }
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public bool RequiresTwoFa { get; set; } = false;
    }
}
