using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseSplitter.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public string? PasswordHash { get; set; }
        
        public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    }

    public class Group
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Currency { get; set; }
        
        public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public ICollection<SplitTemplate> SplitTemplates { get; set; } = new List<SplitTemplate>();
        public ICollection<RecurringExpense> RecurringExpenses { get; set; } = new List<RecurringExpense>();
    }

    public class GroupMember
    {
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? LeftAt { get; set; }
    }

    public enum SplitType
    {
        Equal,
        Percentage,
        ExactAmount,
        Template
    }

    public class Expense
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public Guid PayerId { get; set; }
        public User Payer { get; set; } = null!;
        
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTimeOffset Date { get; set; }
        public required string Category { get; set; }
        public SplitType SplitTypeId { get; set; }
        
        public Guid? RecurringExpenseId { get; set; }
        public RecurringExpense? RecurringExpense { get; set; }
        
        public ICollection<ExpenseParticipant> Participants { get; set; } = new List<ExpenseParticipant>();
    }

    public class ExpenseParticipant
    {
        public Guid ExpenseId { get; set; }
        public Expense Expense { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public decimal OwedAmount { get; set; }
        public decimal? ShareValue { get; set; }
    }

    public class SplitTemplate
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public required string Name { get; set; }
        
        public ICollection<SplitTemplateItem> TemplateItems { get; set; } = new List<SplitTemplateItem>();
    }

    public class SplitTemplateItem
    {
        public Guid TemplateId { get; set; }
        public SplitTemplate Template { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public decimal ShareValue { get; set; }
    }

    public class RecurringExpense
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public Guid PayerId { get; set; }
        public User Payer { get; set; } = null!;
        
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public required string Category { get; set; }
        public required string Frequency { get; set; } // e.g., "Monthly"
        
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset NextRunDate { get; set; }
        
        public SplitType SplitTypeId { get; set; }
        public Guid? SplitTemplateId { get; set; }
        public SplitTemplate? SplitTemplate { get; set; }
    }

    public class Settlement
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
        
        public Guid PayerId { get; set; }
        public User Payer { get; set; } = null!;
        
        public Guid PayeeId { get; set; }
        public User Payee { get; set; } = null!;
        
        public decimal Amount { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
