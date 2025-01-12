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
        public decimal TotalInflowAmount { get; set; }
        public decimal TotalOutflowAmount { get; set; }
        public decimal TotalDebtAmount { get; set; }
        public decimal ClearedDebtAmount { get; set; }
        public List<Transaction> PendingDebts { get; set; } = new();
        public Transaction? HighestInflowTransaction { get; set; }
        public Transaction? HighestOutflowTransaction { get; set; }
        public Transaction? HighestDebtTransaction { get; set; }

        // Updated balance calculation: Credits - Debits - Cleared Debts
        public decimal Balance => TotalInflowAmount - TotalOutflowAmount - ClearedDebtAmount;

        // Updated remaining debt calculation: Only show positive uncleared debt amount
        public decimal RemainingDebt => TotalDebtAmount;

        // New property to show total available balance including pending debts
        public decimal TotalAvailableBalance => Balance - RemainingDebt;

        // New property to show if the account has sufficient balance
        public bool HasSufficientBalance => Balance >= 0;
    }
}