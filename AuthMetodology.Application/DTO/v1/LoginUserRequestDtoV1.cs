using System.ComponentModel.DataAnnotations;

namespace AuthMetodology.Application.DTO.v1
{
    public class LoginUserRequestDtoV1
    {
        [Required(ErrorMessage = "Email is required")]
        [Length(3, 30, ErrorMessage = "Must be between 5 and 30 characters")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=[^А-Яа-я]*$)(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль должен содержать цифры, спецсимволы, латинские буквы в верхнем и нижнем регистре и не должен содержать кириллицу")]
        public string Password { get; set; }

        /*
             ^                       # Начало строки
            (?=[^А-Яа-я]*$)          # Проверка отсутствия кириллицы (Unicode-диапазон)
            (?=.*[a-z])              # Хотя бы одна строчная латинская буква
            (?=.*[A-Z])              # Хотя бы одна заглавная латинская буква
            (?=.*\d)                 # Хотя бы одна цифра
            (?=.*[^\da-zA-Z])        # Хотя бы один спецсимвол (не буква и не цифра)
            .{8,}                    # Минимум 8 символов
            $                        # Конец строки
         */
    }
}
