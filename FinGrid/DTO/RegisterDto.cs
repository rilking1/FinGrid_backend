using System.ComponentModel.DataAnnotations;

namespace FinGrid.DTO
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string PasswordConfirm { get; set; }
    }
}