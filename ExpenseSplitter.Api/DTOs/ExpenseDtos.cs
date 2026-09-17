using System;
using System.Collections.Generic;

namespace ExpenseSplitter.Api.DTOs
{
    public class CreateExpenseDto
    {
        public Guid PayerId { get; set; }
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTimeOffset Date { get; set; }
        public required string Category { get; set; }
        public ExpenseSplitter.Api.Models.SplitType SplitTypeId { get; set; }
        
        public List<CreateExpenseParticipantDto> Participants { get; set; } = new List<CreateExpenseParticipantDto>();
    }

    public class CreateExpenseParticipantDto
    {
        public Guid UserId { get; set; }
        public decimal? ShareValue { get; set; }
    }

    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid PayerId { get; set; }
        public required string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTimeOffset Date { get; set; }
        public required string Category { get; set; }
        
        public List<ExpenseParticipantDto> Participants { get; set; } = new List<ExpenseParticipantDto>();
    }

    public class ExpenseParticipantDto
    {
        public Guid UserId { get; set; }
        public decimal OwedAmount { get; set; }
    }
}
