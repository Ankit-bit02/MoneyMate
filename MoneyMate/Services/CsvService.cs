using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MoneyMate.Models;
using BCrypt.Net;

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
    }
}
