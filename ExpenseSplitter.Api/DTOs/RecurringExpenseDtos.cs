using System;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.DTOs
{
    public class CreateRecurringExpenseDto
    {
        public Guid PayerId { get; set; }
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public required string Category { get; set; }
        public required string Frequency { get; set; } // "Monthly"
        
        public DateTimeOffset StartDate { get; set; }
        
        public SplitType SplitTypeId { get; set; }
        public Guid? SplitTemplateId { get; set; }
    }

    public class RecurringExpenseDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid PayerId { get; set; }
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public required string Category { get; set; }
        public required string Frequency { get; set; }
        
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset NextRunDate { get; set; }
        
        public SplitType SplitTypeId { get; set; }
        public Guid? SplitTemplateId { get; set; }
    }
}
