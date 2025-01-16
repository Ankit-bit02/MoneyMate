using System.Text;
using MoneyMate.Models;

namespace MoneyMate.Services
{
    public class CsvService  // Class responsible for handling CSV operations related to user data
    {
        private readonly string _filePath; // Private field to store the path to the CSV file

        public CsvService()   // Constructor to initialize the CSV service
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);   // Get the path to the current user's desktop
            string directoryPath = Path.Combine(desktopPath, "MoneyMate");   // Create a path for the MoneyMate directory on the desktop

            _filePath = Path.Combine(directoryPath, "users.csv");  // Set the full path for the users.csv file

            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Create file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                using (StreamWriter sw = File.CreateText(_filePath))  // Create the CSV file with headers if it doesn't exist
                {
                    sw.WriteLine("Username,Password,Email,Contact,PreferredCurrency");  // Write the CSV header row
                }
            }
        }

        public async Task SaveUserAsync(User user)  // Method to save a new user to the CSV file
        {
            try
            {
                // Hash password before saving
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

                var line = $"{user.Username},{hashedPassword},{user.Email},{user.Contact},{user.PreferredCurrency}"; // Create a CSV line with user data
                await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);  // Append the user data to the CSV file

                Console.WriteLine($"User saved successfully: {user.Username}"); // Debug log
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<bool> ValidateLoginAsync(string username, string password)  // Method to validate user login credentials
        {
            try
            {
                Console.WriteLine($"Starting login validation for user: {username}"); // Debug log

                if (!File.Exists(_filePath))  // Check if the users file exists
                {
                    Console.WriteLine("Users file does not exist"); // Debug log
                    return false;
                }

                var lines = await File.ReadAllLinesAsync(_filePath);  // Read all lines from the CSV file
                Console.WriteLine($"Found {lines.Length} lines in users file"); // Debug log (Log the number of lines found)

                var userLine = lines.Skip(1) // Skip header row
                                   .FirstOrDefault(line => line.StartsWith($"{username},"));

                if (userLine == null)  // Check if user exists
                {
                    Console.WriteLine("User not found"); // Debug log
                    return false;
                }

                var parts = userLine.Split(',');  // Split the line into parts
                if (parts.Length < 2)
                {
                    Console.WriteLine("Invalid user data format"); // Debug log
                    return false;
                }

                string storedHash = parts[1];   // Get the stored password hash
                bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);  // Verify the provided password against the stored hash

                Console.WriteLine($"Password verification result: {isValid}"); // debug log for the verification result
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login validation: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<bool> IsUserExistsAsync(string username)  // Method to check if a username already exists
        {
            try
            {
                if (!File.Exists(_filePath))  // Check if the file exists
                    return false;

                var lines = await File.ReadAllLinesAsync(_filePath); // Read all lines and check if username exists
                return lines.Skip(1).Any(line => line.StartsWith($"{username},"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking user existence: {ex.Message}"); // Log any errors during user existence check
                throw;
            }
        }

        public async Task<User?> GetUserAsync(string username)  // Method to get user details by username
        {
            try
            {
                if (!File.Exists(_filePath))  // Check if the file exists
                    return null;

                var lines = await File.ReadAllLinesAsync(_filePath); // Read all lines from the file
                var userLine = lines.Skip(1) // Find the line containing user data
                                   .FirstOrDefault(line => line.StartsWith($"{username},"));

                if (userLine == null)  // Return null if user not found
                    return null;

                var parts = userLine.Split(','); // Split the line into parts
                if (parts.Length < 5) // Verify the line has all required fields
                    return null;

                return new User  // Create and return a new User object with the data
                {
                    Username = parts[0],
                    Email = parts[2],
                    Contact = parts[3],
                    PreferredCurrency = parts[4]
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}"); // Log any errors during user retrieval
                throw;
            }
        }

        public async Task<List<string>> GetUsernames() // Method to get all usernames from the CSV file
        {
            try
            {
                if (!File.Exists(_filePath)) // Check if the file exists
                    return new List<string>();

                var lines = await File.ReadAllLinesAsync(_filePath); // Read all lines and extract usernames
                return lines.Skip(1) // Skip header row
                           .Select(line => line.Split(',')[0]) // Get username from each line
                           .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting usernames: {ex.Message}"); // Debug log(Log any errors during username retrieval)
                throw;
            }
        }

        private bool ValidateFilePath() // Private method to validate the CSV file path
        {
            if (string.IsNullOrEmpty(_filePath)) // Check if the file path is empty
            {
                Console.WriteLine("File path is null or empty");  // Log error if file path is invalid
                return false;
            }

            if (!File.Exists(_filePath)) // Check if the file exists
            {
                Console.WriteLine($"File does not exist at: {_filePath}"); // Debug log
                return false;
            }

            return true;
        }
    }
}