using System.ComponentModel.DataAnnotations;

namespace MoneyMate.Models
{
    public class User  // Class representing a user in the MoneyMate application
    {

        // Username property with validation rules
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        // Password property with validation rules
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",  // Regular expression for password complexity
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        // Email property with email format validation
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]  // Validates email format
        public string Email { get; set; } = string.Empty;

        // Contact number property with phone format validation
        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$",  // Advanced phone format validation
            ErrorMessage = "Please enter a valid phone number")]
        public string Contact { get; set; } = string.Empty;

        // Preferred currency property
        [Required(ErrorMessage = "Please select a preferred currency")]
        public string PreferredCurrency { get; set; } = string.Empty;
    }
}
