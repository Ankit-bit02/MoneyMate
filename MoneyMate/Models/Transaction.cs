using System.ComponentModel.DataAnnotations;

namespace MoneyMate.Models
{
    public enum TransactionType  // Enum to define different types of transactions
    {
        Credit,    // Inflow
        Debit,     // Outflow
        Debt       // Debt
    }

    public class Transaction  // Class representing a single financial transaction
    {
        // Unique identifier for the transaction, auto-generated using GUID
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Title/description of the transaction
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        // Amount of money involved in the transaction
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        // Type of transaction (Credit/Debit/Debt)
        [Required]
        public TransactionType Type { get; set; }

        // Date when transaction occurred, defaults to current time
        public DateTime Date { get; set; } = DateTime.Now;

        // Optional additional notes about the transaction
        public string? Note { get; set; }

        // List of tags for categorizing transactions
        public List<string> Tags { get; set; } = new();

        // Username of the transaction owner
        public string Username { get; set; } = string.Empty;

        // Optional due date for debt transactions
        public DateTime? DueDate { get; set; }

        // Optional source of debt (e.g., person or institution)
        public string? DebtSource { get; set; }

        // Flag indicating if a debt has been cleared/paid
        public bool IsCleared { get; set; }
    }

    public class TransactionSummary  // Class providing a comprehensive summary of transactions
    {
        // Basic Amounts
        public decimal TotalInflowAmount { get; set; }
        public decimal TotalOutflowAmount { get; set; }
        public decimal TotalDebtAmount { get; set; }
        public decimal ClearedDebtAmount { get; set; }

        // Collections of transactions
        public List<Transaction> PendingDebts { get; set; } = new();  // Unpaid debts
        public List<Transaction> Transactions { get; set; } = new(); // All transactions

        // Transactions with highest amounts by type
        public Transaction? HighestInflowTransaction { get; set; }
        public Transaction? HighestOutflowTransaction { get; set; }
        public Transaction? HighestDebtTransaction { get; set; }

        // Transactions with lowest amounts by type
        public Transaction? LowestInflowTransaction { get; set; }
        public Transaction? LowestOutflowTransaction { get; set; }
        public Transaction? LowestDebtTransaction { get; set; }

        // Statistical counts
        public int TransactionCount { get; set; }  // Total number of transactions
        public int CurrentMonthTransactions { get; set; }  // Transactions in current month

        // Calculated financial properties
        public decimal Balance => TotalInflowAmount - TotalOutflowAmount - ClearedDebtAmount;  // Current balance
        public decimal RemainingDebt => TotalDebtAmount;  // Unpaid debt total
        public decimal TotalAvailableBalance => Balance - RemainingDebt;  // Balance minus debts
        public bool HasSufficientBalance => Balance >= 0;  // Check if balance is positive

        // Helper method to check if there's enough balance for a specific amount
        public bool HasSufficientBalanceFor(decimal amount) => Balance >= amount;

        // Helper method to format currency amounts with optional symbol
        public string FormatCurrency(decimal amount, string currencySymbol = "")
            => $"{currencySymbol} {amount:N2}".Trim();
    }
}