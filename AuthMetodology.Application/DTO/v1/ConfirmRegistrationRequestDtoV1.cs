using System.ComponentModel.DataAnnotations;

namespace AuthMetodology.Application.DTO.v1
{
    public class ConfirmRegistrationRequestDtoV1
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public required string RegistrationCode { get; set; }
    }
}
