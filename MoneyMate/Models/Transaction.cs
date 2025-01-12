using System.ComponentModel.DataAnnotations;

namespace MoneyMate.Models
{
    public enum TransactionType
    {
        Credit,    // Inflow
        Debit,     // Outflow
        Debt       // Debt
    }

    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string? Note { get; set; }

        public List<string> Tags { get; set; } = new();

        public string Username { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }

        public string? DebtSource { get; set; }

        public bool IsCleared { get; set; }
    }

    public class TransactionSummary
    {
        // Basic Amounts
        public decimal TotalInflowAmount { get; set; }
        public decimal TotalOutflowAmount { get; set; }
        public decimal TotalDebtAmount { get; set; }
        public decimal ClearedDebtAmount { get; set; }

        // Lists
        public List<Transaction> PendingDebts { get; set; } = new();
        public List<Transaction> Transactions { get; set; } = new();

        // Highest Transactions
        public Transaction? HighestInflowTransaction { get; set; }
        public Transaction? HighestOutflowTransaction { get; set; }
        public Transaction? HighestDebtTransaction { get; set; }

        // Lowest Transactions
        public Transaction? LowestInflowTransaction { get; set; }
        public Transaction? LowestOutflowTransaction { get; set; }
        public Transaction? LowestDebtTransaction { get; set; }

        // Counts
        public int TransactionCount { get; set; }
        public int CurrentMonthTransactions { get; set; }

        // Calculated Properties
        public decimal Balance => TotalInflowAmount - TotalOutflowAmount - ClearedDebtAmount;
        public decimal RemainingDebt => TotalDebtAmount;
        public decimal TotalAvailableBalance => Balance - RemainingDebt;
        public bool HasSufficientBalance => Balance >= 0;

        // Helper method to check if there's enough balance for a specific amount
        public bool HasSufficientBalanceFor(decimal amount) => Balance >= amount;

        // Helper method to get formatted currency string
        public string FormatCurrency(decimal amount, string currencySymbol = "")
            => $"{currencySymbol} {amount:N2}".Trim();
    }
}