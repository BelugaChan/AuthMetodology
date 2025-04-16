using System.ComponentModel.DataAnnotations;

namespace AuthMetodology.Application.DTO.v1
{
    public class ResetPasswordRequestDtoV1
    {
        public required string Token { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 20 символов")]
        [RegularExpression(@"^(?=[^А-Яа-я]*$)(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль должен содержать цифры, спецсимволы, латинские буквы в верхнем и нижнем регистре и не должен содержать кириллицу")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirmation is required")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Пароль должен быть от 8 до 20 символов")]
        [RegularExpression(@"^(?=[^А-Яа-я]*$)(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль должен содержать цифры, спецсимволы, латинские буквы в верхнем и нижнем регистре и не должен содержать кириллицу")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
