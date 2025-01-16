using System.ComponentModel.DataAnnotations;


namespace MoneyMate.Models
{
    public class LoginModel  // Class representing the login form data structure
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
