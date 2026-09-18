using System;
using System.Collections.Generic;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services
{
    public class SplitParticipantInput
    {
        public Guid UserId { get; set; }
        public decimal? ShareValue { get; set; }
        public bool IsPayer { get; set; }
    }

    public class CalculatedShare
    {
        public Guid UserId { get; set; }
        public decimal OwedAmount { get; set; }
        public bool IsPayer { get; set; }
    }

    public interface ISplitCalculator
    {
        IEnumerable<CalculatedShare> Calculate(decimal totalAmount, SplitType splitType, IEnumerable<SplitParticipantInput> participants);
    }
}
