using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseSplitter.Api.Exceptions;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services
{
    public class ProrationParticipantInput
    {
        public Guid UserId { get; set; }
        public bool IsPayer { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? LeftAt { get; set; }
        public decimal? ShareValue { get; set; } // Null for Equal splits, 0 if not in template
    }

    public interface IProrationCalculator
    {
        IEnumerable<CalculatedShare> CalculateProratedShares(
            decimal totalAmount, 
            SplitType splitType, 
            DateTimeOffset cycleStart, 
            DateTimeOffset cycleEnd, 
            IEnumerable<ProrationParticipantInput> participants);
    }

    public class ProrationCalculator : IProrationCalculator
    {
        public IEnumerable<CalculatedShare> CalculateProratedShares(
            decimal totalAmount, 
            SplitType splitType, 
            DateTimeOffset cycleStart, 
            DateTimeOffset cycleEnd, 
            IEnumerable<ProrationParticipantInput> participants)
        {
            if (totalAmount <= 0) throw new ArgumentException("Total amount must be positive.");
            if (cycleStart >= cycleEnd) throw new ArgumentException("Cycle end date must be strictly after cycle start date.");

            var participantList = participants.OrderBy(p => p.UserId).ToList();
            if (!participantList.Any()) throw new ValidationException("No participants provided.");

            int totalDays = (cycleEnd - cycleStart).Days;
            if (totalDays <= 0) throw new ValidationException("Billing cycle must be at least 1 day long.");

            // 1. Identify active days
            int activeDaysCount = 0;
            var activeMembersPerDay = new List<List<ProrationParticipantInput>>();

            for (int i = 0; i < totalDays; i++)
            {
                var currentDay = cycleStart.AddDays(i);
                
                // Active if JoinedAt <= currentDay AND (LeftAt is null OR LeftAt > currentDay)
                // We use AddDays(1) on currentDay to check if they were active AT ALL during that day
                var nextDay = currentDay.AddDays(1);
                
                var activeToday = participantList.Where(p => 
                    p.JoinedAt < nextDay && 
                    (p.LeftAt == null || p.LeftAt > currentDay)
                ).ToList();

                activeMembersPerDay.Add(activeToday);
                if (activeToday.Any()) activeDaysCount++;
            }

            if (activeDaysCount == 0)
                throw new ValidationException("No members were active during this billing cycle.");

            // 2. Calculate daily cost, skipping empty days
            decimal dailyCost = totalAmount / activeDaysCount;
            
            var userTotals = participantList.ToDictionary(p => p.UserId, p => 0m);

            // 3. Distribute daily cost
            for (int i = 0; i < totalDays; i++)
            {
                var activeToday = activeMembersPerDay[i];
                if (!activeToday.Any()) continue; // Skip empty days (cost redistributed to active days)

                decimal sumOfWeights = 0;
                foreach (var p in activeToday)
                {
                    decimal weight = splitType == SplitType.Equal ? 1m : (p.ShareValue ?? 0m);
                    sumOfWeights += weight;
                }

                if (sumOfWeights <= 0)
                    throw new ValidationException($"Active members on day {i+1} have a total weight of 0.");

                foreach (var p in activeToday)
                {
                    decimal weight = splitType == SplitType.Equal ? 1m : (p.ShareValue ?? 0m);
                    decimal portion = weight / sumOfWeights;
                    userTotals[p.UserId] += (portion * dailyCost);
                }
            }

            // 4. Rounding and remainder distribution
            var results = new List<CalculatedShare>();
            decimal sumCalculated = 0m;

            foreach (var p in participantList)
            {
                decimal flooredAmount = Math.Floor(userTotals[p.UserId] * 100) / 100;
                sumCalculated += flooredAmount;
                results.Add(new CalculatedShare
                {
                    UserId = p.UserId,
                    OwedAmount = flooredAmount,
                    IsPayer = p.IsPayer
                });
            }

            decimal remainder = totalAmount - sumCalculated;
            int remainderCents = (int)Math.Round(remainder * 100);

            for (int i = 0; i < remainderCents; i++)
            {
                results[i].OwedAmount += 0.01m;
            }

            return results;
        }
    }
}
