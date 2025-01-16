using System.Text;
using MoneyMate.Models;

namespace MoneyMate.Services
{
    public class TransactionService  // Class responsible for handling all transaction-related operations
    {
        private readonly string _filePath; // Private field to store the path to the transactions CSV file

        public TransactionService() // Constructor to initialize the TransactionService
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);  // Get the path to the current user's desktop
            string directoryPath = Path.Combine(desktopPath, "MoneyMate");  // Create a path for the MoneyMate directory on the desktop
            _filePath = Path.Combine(directoryPath, "transactions.csv");// Set the full path for the transactions.csv file

            if (!Directory.Exists(directoryPath)) // Create the directory if it doesn't exist
                Directory.CreateDirectory(directoryPath); 

            if (!File.Exists(_filePath)) // Create the transactions CSV file with headers if it doesn't exist
            {
                using (StreamWriter sw = File.CreateText(_filePath))
                {
                    sw.WriteLine("Id,Username,Type,Title,Amount,Date,Note,Tags,DueDate,DebtSource,IsCleared");// Write the CSV header with all transaction fields
                }
            }
        }


        // Method to get transactions for a specific user within an optional date range
        public async Task<List<Transaction>> GetUserTransactionsAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var transactions = new List<Transaction>();  // Initialize empty list for transactions
                var lines = await File.ReadAllLinesAsync(_filePath);  // Read all lines from the CSV file

                Console.WriteLine($"Found {lines.Length} lines in transactions file");  // Log the number of lines found

                if (lines.Length <= 1) // Return empty list if file only contains header
                {
                    Console.WriteLine("No transactions found (only header)");
                    return transactions;
                }

                for (int i = 1; i < lines.Length; i++) // Process each line after the header
                {
                    try
                    {
                        var cols = lines[i].Split(',');  // Split the line into columns
                        if (cols[1] == username)  // Check if transaction belongs to requested user
                        {
                            var transaction = new Transaction   // Create new Transaction object from CSV data
                            {
                                Id = cols[0],
                                Username = cols[1],
                                Type = Enum.Parse<TransactionType>(cols[2]),
                                Title = cols[3],
                                Amount = decimal.Parse(cols[4]),
                                Date = DateTime.Parse(cols[5]),
                                Note = cols[6],
                                // Split tags by semicolon or create empty list
                                Tags = !string.IsNullOrEmpty(cols[7]) ? cols[7].Split(';').ToList() : new List<string>(),
                                DueDate = !string.IsNullOrEmpty(cols[8]) ? DateTime.Parse(cols[8]) : null, // Parse due date if present
                                DebtSource = cols[9],
                                IsCleared = bool.Parse(cols[10])
                            };

                            bool includeTransaction = true;   // Check if transaction falls within date range
                            if (startDate.HasValue)
                            {
                                includeTransaction = transaction.Date.Date >= startDate.Value.Date;
                            }
                            if (endDate.HasValue && includeTransaction)
                            {
                                includeTransaction = transaction.Date.Date <= endDate.Value.Date;
                            }

                            if (includeTransaction) // Add transaction if it meets date criteria
                            {
                                transactions.Add(transaction);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing transaction at line {i}: {ex.Message}"); // Log error but continue processing other transactions
                        continue;
                    }
                }

                return transactions.OrderByDescending(t => t.Date).ToList(); // Return transactions sorted by date (newest first)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading transactions: {ex.Message}");
                throw;
            }
        }

        private async Task<List<Transaction>> GetAllTransactionsAsync()   // Private method to get all transactions regardless of user
        {
            var lines = await File.ReadAllLinesAsync(_filePath); // Read all lines from the CSV file
            var transactions = new List<Transaction>();

            for (int i = 1; i < lines.Length; i++)  // Process each line after the header
            {
                var cols = lines[i].Split(',');
                try
                {
                    var transaction = new Transaction   // Create Transaction object from CSV data
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
                    Console.WriteLine($"Error parsing line {i}: {ex.Message}");  // Log error but continue processing other transactions
                }
            }
            return transactions;
        }

        private async Task SaveTransactionsAsync(List<Transaction> transactions)  // Private method to save all transactions back to the CSV file
        {
            var lines = new List<string>  // Create list starting with header row
            {
                "Id,Username,Type,Title,Amount,Date,Note,Tags,DueDate,DebtSource,IsCleared"
            };

            foreach (var t in transactions)   // Convert each transaction to CSV format
            {
                // Format transaction data, handling special characters and null values
                var line = $"{t.Id},{t.Username},{t.Type},{t.Title}," +
                          $"{t.Amount},{t.Date:yyyy-MM-dd HH:mm:ss}," +
                          $"{t.Note?.Replace(",", ";")}," +
                          $"{string.Join(";", t.Tags)}," +
                          $"{(t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{t.DebtSource}," +
                          $"{t.IsCleared}";
                lines.Add(line);
            }

            await File.WriteAllLinesAsync(_filePath, lines); // Write all lines to the file
        }


        // Method to get transaction summary for a user within an optional date range
        public async Task<TransactionSummary> GetTransactionSummaryAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Get all user transactions within date range
            var transactions = await GetUserTransactionsAsync(username, startDate, endDate);
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Calculate total inflow (credits and uncleared debts)
            var totalInflow = transactions.Where(t => t.Type == TransactionType.Credit ||
                                                    (t.Type == TransactionType.Debt && !t.IsCleared))
                                        .Sum(t => t.Amount);

            // Calculate total outflow (debits)
            var totalOutflow = transactions.Where(t => t.Type == TransactionType.Debit)
                                         .Sum(t => t.Amount);

            // Calculate pending and cleared debts
            var pendingDebts = transactions.Where(t => t.Type == TransactionType.Debt && !t.IsCleared)
                                         .Sum(t => t.Amount);

            var clearedDebts = transactions.Where(t => t.Type == TransactionType.Debt && t.IsCleared)
                                         .Sum(t => t.Amount);

            // Get credit transactions for highest/lowest calculations
            var creditTransactions = transactions.Where(t => t.Type == TransactionType.Credit).ToList();
            var debitTransactions = transactions.Where(t => t.Type == TransactionType.Debit).ToList();
            var debtTransactions = transactions.Where(t => t.Type == TransactionType.Debt).ToList();

            // Create and return transaction summary
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
                                                                 t.Date.Year == currentYear),
                // Add the transactions list, ordered by date descending and take the most recent ones
                Transactions = transactions.OrderByDescending(t => t.Date)
                                         .ToList()
            };
        }

        public async Task<decimal> GetAvailableBalanceAsync(string username)  // Method to calculate available balance for a user
        {
            var transactions = await GetUserTransactionsAsync(username); // Get all user transactions

            // Calculate total inflow (credits and uncleared debts)
            var totalInflow = transactions.Where(t => t.Type == TransactionType.Credit ||
                                                    (t.Type == TransactionType.Debt && !t.IsCleared))
                                        .Sum(t => t.Amount);

            // Calculate total outflow (debits)
            var totalOutflow = transactions.Where(t => t.Type == TransactionType.Debit)
                                         .Sum(t => t.Amount);

            // Return available balance
            return totalInflow - totalOutflow;
        }

        public async Task<bool> HasSufficientBalanceAsync(string username, decimal amount)  // Method to check if user has sufficient balance for a transaction
        {
            var availableBalance = await GetAvailableBalanceAsync(username);
            return availableBalance >= amount;
        }

        public async Task AddTransactionAsync(Transaction transaction) // Method to add a new transaction
        {
            try
            {
                var safeNote = transaction.Note?.Replace(",", ";"); // Replace commas with semicolons in note to prevent CSV issues
                var safeTags = string.Join(";", transaction.Tags ?? new List<string>());  // Join tags with semicolons

                // Format transaction data for CSV
                var line = $"{transaction.Id},{transaction.Username},{transaction.Type},{transaction.Title}," +
                          $"{transaction.Amount},{transaction.Date:yyyy-MM-dd HH:mm:ss}," +
                          $"{safeNote}," +
                          $"{safeTags}," +
                          $"{(transaction.DueDate.HasValue ? transaction.DueDate.Value.ToString("yyyy-MM-dd") : "")}," +
                          $"{transaction.DebtSource}," +
                          $"{transaction.IsCleared}";

                await File.AppendAllTextAsync(_filePath, Environment.NewLine + line);   // Append transaction to CSV file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding transaction: {ex.Message}");  // Log error and rethrow
                throw;
            }
        }

        public async Task<bool> ClearDebtAsync(string debtId, string username, decimal paymentAmount)   // Method to clear a debt transaction
        {
            try
            {
                var transactions = await GetAllTransactionsAsync();  // Get all transactions
                var debt = transactions.FirstOrDefault(t => t.Id == debtId && t.Username == username);  // Find the specific debt transaction

                // Validate debt transaction
                if (debt == null || debt.Type != TransactionType.Debt || paymentAmount > debt.Amount)
                    return false;

                // When clearing a debt, its full amount should be available as balance
                // because the debt itself provides the money to pay itself
                var availableBalance = debt.Amount;  // The debt amount itself is available to clear the debt

                if (paymentAmount > availableBalance)    // Check if payment amount exceeds available balance
                    return false;

                if (paymentAmount == debt.Amount) // Mark debt as cleared if fully paid
                {
                    debt.IsCleared = true;
                }
                else
                {
                    var remainingDebt = new Transaction  // Create new transaction for remaining debt
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

                    // Update original debt
                    debt.IsCleared = true;   
                    debt.Amount = paymentAmount;
                    transactions.Add(remainingDebt);
                }

                // Save all transactions back to file
                await SaveTransactionsAsync(transactions);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing debt: {ex.Message}");  // Log error and return false
                return false;
            }
        }
    }
}