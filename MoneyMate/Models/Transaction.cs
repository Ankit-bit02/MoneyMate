﻿namespace MoneyMate.Models
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
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
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
        public decimal Balance => TotalInflowAmount - TotalOutflowAmount;
        public decimal RemainingDebt => TotalDebtAmount - ClearedDebtAmount;
    }
}