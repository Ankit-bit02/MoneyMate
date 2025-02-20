﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyMate.Models
{
    public class UserValidation   // Class containing user validation rules and structure      
    {
        public class User   
        {
            [Required(ErrorMessage = "Username is required")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Contact number is required")]
            [Phone(ErrorMessage = "Invalid phone number")]
            public string Contact { get; set; }

            [Required(ErrorMessage = "Please select a preferred currency")]
            public string PreferredCurrency { get; set; }
        }
    }
}
