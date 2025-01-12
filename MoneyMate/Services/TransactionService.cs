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

                Console.WriteLine($"Found {lines.Length} lines in transactions file");

                if (lines.Length <= 1)
                {
                    Console.WriteLine("No transactions found (only header)");
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

                            bool includeTransaction = true;
                            if (startDate.HasValue)
                            {
                                includeTransaction = transaction.Date.Date >= startDate.Value.Date;
                            }
                            if (endDate.HasValue && includeTransaction)
                            {
                                includeTransaction = transaction.Date.Date <= endDate.Value.Date;
                            }

                            if (includeTransaction)
                            {
                                transactions.Add(transaction);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing transaction at line {i}: {ex.Message}");
                        continue;
                    }
                }

                return transactions.OrderByDescending(t => t.Date).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading transactions: {ex.Message}");
                throw;
            }
        }

        private async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            var lines = await File.ReadAllLinesAsync(_filePath);
            var transactions = new List<Transaction>();

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
                "Id,Username,Type,Title,Amount,Date,Note,Tags,DueDate,DebtSource,IsCleared"
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
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var totalInflow = transactions.Where(t => t.Type == TransactionType.Credit ||
                                                    (t.Type == TransactionType.Debt && !t.IsCleared))
                                        .Sum(t => t.Amount);

            var totalOutflow = transactions.Where(t => t.Type == TransactionType.Debit)
                                         .Sum(t => t.Amount);

            var pendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared)
                                         .Sum(t => t.Amount);

            var clearedDebts = transactions.Where(t => t.Type == TransactionType.Debt && t.IsCleared)
                                         .Sum(t => t.Amount);

            // Get credit transactions for highest/lowest calculations
            var creditTransactions = transactions.Where(t => t.Type == TransactionType.Credit).ToList();
            var debitTransactions = transactions.Where(t => t.Type == TransactionType.Debit).ToList();
            var debtTransactions = transactions.Where(t => t.Type == TransactionType.Debt).ToList();

            return new TransactionSummary
            {
                TotalInflowAmount = totalInflow,
                TotalOutflowAmount = totalOutflow,
                TotalDebtAmount = pendingDebts,
                ClearedDebtAmount = clearedDebts,
                PendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared)
                                         .OrderBy(t => t.DueDate)
                                         .ToList(),
                HighestInflowTransaction = creditTransactions.MaxBy(t => t.Amount),
                HighestOutflowTransaction = debitTransactions.MaxBy(t => t.Amount),
                HighestDebtTransaction = debtTransactions.MaxBy(t => t.Amount),
                LowestInflowTransaction = creditTransactions.MinBy(t => t.Amount),
                LowestOutflowTransaction = debitTransactions.MinBy(t => t.Amount),
                LowestDebtTransaction = debtTransactions.MinBy(t => t.Amount),
                TransactionCount = transactions.Count,
                CurrentMonthTransactions = transactions.Count(t => t.Date.Month == currentMonth &&
                                                                 t.Date.Year == currentYear)
            };
        }

        public async Task<decimal> GetAvailableBalanceAsync(string username)
        {
            var transactions = await GetUserTransactionsAsync(username);
            var totalInflow = transactions.Where(t => t.Type == TransactionType.Credit ||
                                                    (t.Type == TransactionType.Debt && !t.IsCleared))
                                        .Sum(t => t.Amount);
            var totalOutflow = transactions.Where(t => t.Type == TransactionType.Debit)
                                         .Sum(t => t.Amount);
            return totalInflow - totalOutflow;
        }

        public async Task<bool> HasSufficientBalanceAsync(string username, decimal amount)
        {
            var availableBalance = await GetAvailableBalanceAsync(username);
            return availableBalance >= amount;
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            try
            {
                var safeNote = transaction.Note?.Replace(",", ";");
                var safeTags = string.Join(";", transaction.Tags ?? new List<string>());

                var line = $"{transaction.Id},{transaction.Username},{transaction.Type},{transaction.Title}," +
                          $"{transaction.Amount},{transaction.Date:yyyy-MM-dd HH:mm:ss}," +
                          $"{safeNote}," +
                          $"{safeTags}," +
                          $"{(transaction.DueDate.HasValue ? transaction.DueDate.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{transaction.DebtSource}," +
                          $"{transaction.IsCleared}";

                await File.AppendAllTextAsync(_filePath, Environment.NewLine + line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding transaction: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ClearDebtAsync(string debtId, string username, decimal paymentAmount)
        {
            try
            {
                var transactions = await GetAllTransactionsAsync();
                var debt = transactions.FirstOrDefault(t => t.Id == debtId && t.Username == username);

                if (debt == null || debt.Type != TransactionType.Debt || paymentAmount > debt.Amount)
                    return false;

                // When clearing a debt, its full amount should be available as balance
                // because the debt itself provides the money to pay itself
                var availableBalance = debt.Amount;  // The debt amount itself is available to clear the debt

                if (paymentAmount > availableBalance)
                    return false;

                if (paymentAmount == debt.Amount)
                {
                    debt.IsCleared = true;
                }
                else
                {
                    var remainingDebt = new Transaction
                    {
                        Username = debt.Username,
                        Type = TransactionType.Debt,
                        Title = $"Remaining: {debt.Title}",
                        Amount = debt.Amount - paymentAmount,
                        Date = DateTime.Now,
                        DueDate = debt.DueDate,
                        DebtSource = debt.DebtSource,
                        Note = $"Remaining debt from {debt.Title}",
                        Tags = debt.Tags,
                        IsCleared = false
                    };

                    debt.IsCleared = true;
                    debt.Amount = paymentAmount;
                    transactions.Add(remainingDebt);
                }

                await SaveTransactionsAsync(transactions);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing debt: {ex.Message}");
                return false;
            }
        }
    }
}