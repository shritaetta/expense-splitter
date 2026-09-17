using System;

namespace ExpenseSplitter.Api.DTOs
{
    public class SuggestedSettlementDto
    {
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateSettlementDto
    {
        public Guid PayerId { get; set; } // The person paying the debt (From)
        public Guid PayeeId { get; set; } // The person receiving the money (To)
        public decimal Amount { get; set; }
    }

    public class SettlementDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid PayerId { get; set; }
        public Guid PayeeId { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset Date { get; set; }
    }
}
