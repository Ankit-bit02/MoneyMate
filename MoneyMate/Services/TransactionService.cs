using System.Text;
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
            _filePath = Path.Combine(directoryPath, "transactions.csv");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(_filePath))
            {
                using (StreamWriter sw = File.CreateText(_filePath))
                {
                    sw.WriteLine("Id,Username,Type,Title,Amount,Date,Note,Tags,DueDate,DebtSource,IsCleared");
                }
            }
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = new List<Transaction>();
            var lines = await File.ReadAllLinesAsync(_filePath);

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                if (cols[1] == username)
                {
                    var transaction = new Transaction
                    {
                        Id = cols[0],
                        Username = cols[1],
                        Type = Enum.Parse<TransactionType>(cols[2]),
                        Title = cols[3],
                        Amount = decimal.Parse(cols[4]),
                        Date = DateTime.Parse(cols[5]),
                        Note = cols[6],
                        Tags = cols[7].Split(';').ToList(),
                        DueDate = !string.IsNullOrEmpty(cols[8]) ? DateTime.Parse(cols[8]) : null,
                        DebtSource = cols[9],
                        IsCleared = bool.Parse(cols[10])
                    };

                    if (!startDate.HasValue || transaction.Date >= startDate)
                        if (!endDate.HasValue || transaction.Date <= endDate)
                            transactions.Add(transaction);
                }
            }

            return transactions.OrderByDescending(t => t.Date).ToList();
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
            var line = $"{transaction.Id},{transaction.Username},{transaction.Type},{transaction.Title}," +
                      $"{transaction.Amount},{transaction.Date:yyyy-MM-dd}," +
                      $"{transaction.Note?.Replace(",", ";")}," +
                      $"{string.Join(";", transaction.Tags)}," +
                      $"{transaction.DueDate:yyyy-MM-dd}," +
                      $"{transaction.DebtSource}," +
                      $"{transaction.IsCleared}";

            await File.AppendAllTextAsync(_filePath, Environment.NewLine + line);
        }

        public async Task<bool> HasSufficientBalanceAsync(string username, decimal amount)
        {
            var transactions = await GetUserTransactionsAsync(username);
            var income = transactions.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);
            var expenses = transactions.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);
            return (income - expenses) >= amount;
        }
    }
}