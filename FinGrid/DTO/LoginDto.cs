using System.ComponentModel.DataAnnotations;

namespace FinGrid.DTO
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}