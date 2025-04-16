using System.ComponentModel.DataAnnotations;

namespace AuthMetodology.Application.DTO.v1
{
    public class ForgotPasswordRequestDtoV1
    {
        [Required(ErrorMessage = "Email is required")]
        [Length(3, 30, ErrorMessage = "Must be between 5 and 30 characters")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
