using System.Text;
using MoneyMate.Models;

namespace MoneyMate.Services
{
    public class CsvService
    {
        private readonly string _filePath;

        public CsvService()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string directoryPath = Path.Combine(desktopPath, "MoneyMate");
            _filePath = Path.Combine(directoryPath, "users.csv");

            // Create directory if it doesn't exist
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Create file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                using (StreamWriter sw = File.CreateText(_filePath))
                {
                    sw.WriteLine("Username,Password,Email,Contact,PreferredCurrency");
                }
            }
        }

        public async Task SaveUserAsync(User user)
        {
            try
            {
                // Hash password before saving
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

                var line = $"{user.Username},{hashedPassword},{user.Email},{user.Contact},{user.PreferredCurrency}";
                await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);

                Console.WriteLine($"User saved successfully: {user.Username}"); // Debug log
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<bool> ValidateLoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"Starting login validation for user: {username}"); // Debug log

                if (!File.Exists(_filePath))
                {
                    Console.WriteLine("Users file does not exist"); // Debug log
                    return false;
                }

                var lines = await File.ReadAllLinesAsync(_filePath);
                Console.WriteLine($"Found {lines.Length} lines in users file"); // Debug log

                var userLine = lines.Skip(1) // Skip header
                                   .FirstOrDefault(line => line.StartsWith($"{username},"));

                if (userLine == null)
                {
                    Console.WriteLine("User not found"); // Debug log
                    return false;
                }

                var parts = userLine.Split(',');
                if (parts.Length < 2)
                {
                    Console.WriteLine("Invalid user data format"); // Debug log
                    return false;
                }

                string storedHash = parts[1];
                bool isValid = BCrypt.Net.BCrypt.Verify(password, storedHash);

                Console.WriteLine($"Password verification result: {isValid}"); // Debug log
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login validation: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<bool> IsUserExistsAsync(string username)
        {
            try
            {
                if (!File.Exists(_filePath))
                    return false;

                var lines = await File.ReadAllLinesAsync(_filePath);
                return lines.Skip(1).Any(line => line.StartsWith($"{username},"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking user existence: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<User?> GetUserAsync(string username)
        {
            try
            {
                if (!File.Exists(_filePath))
                    return null;

                var lines = await File.ReadAllLinesAsync(_filePath);
                var userLine = lines.Skip(1)
                                   .FirstOrDefault(line => line.StartsWith($"{username},"));

                if (userLine == null)
                    return null;

                var parts = userLine.Split(',');
                if (parts.Length < 5)
                    return null;

                return new User
                {
                    Username = parts[0],
                    Email = parts[2],
                    Contact = parts[3],
                    PreferredCurrency = parts[4]
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}"); // Debug log
                throw;
            }
        }

        public async Task<List<string>> GetUsernames()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<string>();

                var lines = await File.ReadAllLinesAsync(_filePath);
                return lines.Skip(1) // Skip header
                           .Select(line => line.Split(',')[0])
                           .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting usernames: {ex.Message}"); // Debug log
                throw;
            }
        }

        private bool ValidateFilePath()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Console.WriteLine("File path is null or empty"); // Debug log
                return false;
            }

            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"File does not exist at: {_filePath}"); // Debug log
                return false;
            }

            return true;
        }
    }
}