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
            try
            {
                var transactions = new List<Transaction>();
                var lines = await File.ReadAllLinesAsync(_filePath);

                Console.WriteLine($"Found {lines.Length} lines in transactions file"); // Debug log

                // Skip header line and check if there's any data
                if (lines.Length <= 1)
                {
                    Console.WriteLine("No transactions found (only header)"); // Debug log
                    return transactions;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    try
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
                                Tags = !string.IsNullOrEmpty(cols[7]) ? cols[7].Split(';').ToList() : new List<string>(),
                                DueDate = !string.IsNullOrEmpty(cols[8]) ? DateTime.Parse(cols[8]) : null,
                                DebtSource = cols[9],
                                IsCleared = bool.Parse(cols[10])
                            };

                            if (!startDate.HasValue || transaction.Date >= startDate)
                                if (!endDate.HasValue || transaction.Date <= endDate)
                                    transactions.Add(transaction);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing transaction at line {i}: {ex.Message}"); // Debug log
                        continue; // Skip problematic lines but continue processing
                    }
                }

                Console.WriteLine($"Returning {transactions.Count} transactions"); // Debug log
                return transactions.OrderByDescending(t => t.Date).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading transactions: {ex.Message}"); // Debug log
                throw;
            }
        }

        private async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            var lines = await File.ReadAllLinesAsync(_filePath);
            var transactions = new List<Transaction>();

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split(',');
                try
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
                        Tags = !string.IsNullOrEmpty(cols[7]) ? cols[7].Split(';').ToList() : new List<string>(),
                        DueDate = !string.IsNullOrEmpty(cols[8]) ? DateTime.Parse(cols[8]) : null,
                        DebtSource = cols[9],
                        IsCleared = bool.Parse(cols[10])
                    };
                    transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                }
            }
            return transactions;
        }

        private async Task SaveTransactionsAsync(List<Transaction> transactions)
        {
            var lines = new List<string>
            {
                "Id,Username,Type,Title,Amount,Date,Note,Tags,DueDate,DebtSource,IsCleared" // Header
            };

            foreach (var t in transactions)
            {
                var line = $"{t.Id},{t.Username},{t.Type},{t.Title}," +
                          $"{t.Amount},{t.Date:yyyy-MM-dd HH:mm:ss}," +
                          $"{t.Note?.Replace(",", ";")}," +
                          $"{string.Join(";", t.Tags)}," +
                          $"{(t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{t.DebtSource}," +
                          $"{t.IsCleared}";
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(_filePath, lines);
        }

        public async Task<TransactionSummary> GetTransactionSummaryAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetUserTransactionsAsync(username, startDate, endDate);

            // Only count uncleared debts and credits for total inflow
            var totalInflow = transactions.Where(t => t.Type == TransactionType.Credit ||
                                                    (t.Type == TransactionType.Debt && !t.IsCleared))
                                        .Sum(t => t.Amount);

            var totalOutflow = transactions.Where(t => t.Type == TransactionType.Debit)
                                         .Sum(t => t.Amount);

            var pendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared)
                                         .Sum(t => t.Amount);

            var clearedDebts = transactions.Where(t => t.Type == TransactionType.Debt && t.IsCleared)
                                         .Sum(t => t.Amount);

            return new TransactionSummary
            {
                TotalInflowAmount = totalInflow,
                TotalOutflowAmount = totalOutflow,
                TotalDebtAmount = pendingDebts,  // This will be positive
                ClearedDebtAmount = clearedDebts,
                PendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared)
                                         .OrderBy(t => t.DueDate)
                                         .ToList(),
                HighestInflowTransaction = transactions.Where(t => t.Type == TransactionType.Credit)
                                                     .MaxBy(t => t.Amount),
                HighestOutflowTransaction = transactions.Where(t => t.Type == TransactionType.Debit)
                                                      .MaxBy(t => t.Amount),
                HighestDebtTransaction = transactions.Where(t => t.Type == TransactionType.Debt)
                                                   .MaxBy(t => t.Amount)
            };
        }
        public async Task<bool> ClearDebtAsync(string debtId, string username)
        {
            try
            {
                var transactions = await GetAllTransactionsAsync();
                var debt = transactions.FirstOrDefault(t => t.Id == debtId && t.Username == username);

                if (debt == null || debt.Type != TransactionType.Debt)
                    return false;

                // Check if user has sufficient balance from cash inflows
                var creditTotal = transactions.Where(t => t.Username == username && t.Type == TransactionType.Credit)
                                            .Sum(t => t.Amount);
                var debitTotal = transactions.Where(t => t.Username == username && t.Type == TransactionType.Debit)
                                           .Sum(t => t.Amount);
                var availableBalance = creditTotal - debitTotal;

                if (availableBalance < debt.Amount)
                    return false;

                debt.IsCleared = true;
                await SaveTransactionsAsync(transactions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            try
            {
                // Ensure note and tags don't contain commas as they would break CSV format
                var safeNote = transaction.Note?.Replace(",", ";");
                var safeTags = string.Join(";", transaction.Tags ?? new List<string>());

                var line = $"{transaction.Id},{transaction.Username},{transaction.Type},{transaction.Title}," +
                          $"{transaction.Amount},{transaction.Date:yyyy-MM-dd HH:mm:ss}," +
                          $"{safeNote}," +
                          $"{safeTags}," +
                          $"{(transaction.DueDate.HasValue ? transaction.DueDate.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{transaction.DebtSource}," +
                          $"{transaction.IsCleared}";

                Console.WriteLine($"Adding transaction: {line}"); // Debug log
                await File.AppendAllTextAsync(_filePath, Environment.NewLine + line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding transaction: {ex.Message}"); // Debug log
                throw;
            }
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