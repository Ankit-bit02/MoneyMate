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
            // Hash password before saving
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

            var line = $"{user.Username},{hashedPassword},{user.Email},{user.Contact},{user.PreferredCurrency}";
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);
        }

        public async Task<bool> IsUserExistsAsync(string username)
        {
            if (!File.Exists(_filePath))
                return false;

            var lines = await File.ReadAllLinesAsync(_filePath);
            return lines.Skip(1).Any(line => line.StartsWith(username + ","));
        }

        public async Task<bool> ValidateLoginAsync(string username, string password)
        {
            if (!File.Exists(_filePath))
                return false;

            var lines = await File.ReadAllLinesAsync(_filePath);
            var userLine = lines.Skip(1) // Skip header
                               .FirstOrDefault(line => line.StartsWith(username + ","));

            if (userLine == null)
                return false;

            var parts = userLine.Split(',');
            if (parts.Length < 2)
                return false;

            string storedHash = parts[1];
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        public async Task<User?> GetUserAsync(string username)
        {
            if (!File.Exists(_filePath))
                return null;

            var lines = await File.ReadAllLinesAsync(_filePath);
            var userLine = lines.Skip(1)
                               .FirstOrDefault(line => line.StartsWith(username + ","));

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
    }
}
