using System.Text.Json;
using MoneyMate.Models;

namespace MoneyMate.Services
{
    public class TransactionService
    {
        private readonly string _filePath;

        public TransactionService()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string directoryPath = Path.Combine(desktopPath, "MoneyMate");
            _filePath = Path.Combine(directoryPath, "transactions.json");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, JsonSerializer.Serialize(new List<Transaction>()));
            }
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            var content = await File.ReadAllTextAsync(_filePath);
            var transactions = JsonSerializer.Deserialize<List<Transaction>>(content) ?? new List<Transaction>();

            return transactions
                .Where(t => t.Username == username)
                .Where(t => !startDate.HasValue || t.Date >= startDate)
                .Where(t => !endDate.HasValue || t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public async Task<TransactionSummary> GetTransactionSummaryAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetUserTransactionsAsync(username, startDate, endDate);

            return new TransactionSummary
            {
                TotalInflowAmount = transactions.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount),
                TotalOutflowAmount = transactions.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount),
                TotalDebtAmount = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared).Sum(t => t.Amount),
                ClearedDebtAmount = transactions.Where(t => t.Type == TransactionType.Debt && t.IsCleared).Sum(t => t.Amount),
                PendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared).ToList(),
                HighestInflowTransaction = transactions.Where(t => t.Type == TransactionType.Credit).MaxBy(t => t.Amount),
                HighestOutflowTransaction = transactions.Where(t => t.Type == TransactionType.Debit).MaxBy(t => t.Amount),
                HighestDebtTransaction = transactions.Where(t => t.Type == TransactionType.Debt).MaxBy(t => t.Amount)
            };
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            var transactions = await GetAllTransactionsAsync();
            transactions.Add(transaction);
            await SaveTransactionsAsync(transactions);
        }

        private async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            var content = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<Transaction>>(content) ?? new List<Transaction>();
        }

        private async Task SaveTransactionsAsync(List<Transaction> transactions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(transactions, options));
        }
    }
}