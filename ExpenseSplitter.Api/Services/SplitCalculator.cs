using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseSplitter.Api.Exceptions;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services
{
    public class SplitCalculator : ISplitCalculator
    {
        public IEnumerable<CalculatedShare> Calculate(decimal totalAmount, SplitType splitType, IEnumerable<SplitParticipantInput> participants)
        {
            if (totalAmount <= 0)
                throw new ArgumentException("Total amount must be positive.");

            var participantList = participants.OrderBy(p => p.UserId).ToList();
            if (!participantList.Any())
                throw new ArgumentException("Must have at least one participant.");

            return splitType switch
            {
                SplitType.Equal => CalculateEqual(totalAmount, participantList),
                SplitType.Percentage => CalculatePercentage(totalAmount, participantList),
                SplitType.ExactAmount => CalculateExact(totalAmount, participantList),
                SplitType.Template => CalculateWeighted(totalAmount, participantList), // Templates resolve to weights
                _ => throw new NotImplementedException($"SplitType {splitType} is not supported.")
            };
        }

        private IEnumerable<CalculatedShare> CalculateEqual(decimal totalAmount, List<SplitParticipantInput> participants)
        {
            int count = participants.Count;
            decimal baseAmount = Math.Floor((totalAmount / count) * 100) / 100; // Floor to 2 decimals
            decimal remainder = totalAmount - (baseAmount * count);
            int remainderCents = (int)Math.Round(remainder * 100);

            var results = new List<CalculatedShare>();
            for (int i = 0; i < count; i++)
            {
                decimal amount = baseAmount;
                if (i < remainderCents)
                {
                    amount += 0.01m;
                }
                results.Add(new CalculatedShare 
                { 
                    UserId = participants[i].UserId, 
                    OwedAmount = amount,
                    IsPayer = participants[i].IsPayer
                });
            }

            return results;
        }

        private IEnumerable<CalculatedShare> CalculatePercentage(decimal totalAmount, List<SplitParticipantInput> participants)
        {
            if (participants.Any(p => !p.ShareValue.HasValue))
                throw new ValidationException("All participants must have a ShareValue for Percentage splits.");

            decimal totalPercentage = participants.Sum(p => p.ShareValue.Value);
            if (totalPercentage != 100m)
                throw new ValidationException($"Total percentage must equal 100. Current sum is {totalPercentage}.");

            var results = new List<CalculatedShare>();
            decimal sumCalculated = 0m;

            // Calculate base amounts
            foreach (var p in participants)
            {
                decimal amount = Math.Floor((totalAmount * (p.ShareValue.Value / 100m)) * 100) / 100;
                sumCalculated += amount;
                results.Add(new CalculatedShare
                {
                    UserId = p.UserId,
                    OwedAmount = amount,
                    IsPayer = p.IsPayer
                });
            }

            // Distribute remainder cents to the first N users (since they are already sorted by UserId)
            decimal remainder = totalAmount - sumCalculated;
            int remainderCents = (int)Math.Round(remainder * 100);

            for (int i = 0; i < remainderCents; i++)
            {
                results[i].OwedAmount += 0.01m;
            }

            return results;
        }

        private IEnumerable<CalculatedShare> CalculateExact(decimal totalAmount, List<SplitParticipantInput> participants)
        {
            if (participants.Any(p => !p.ShareValue.HasValue))
                throw new ValidationException("All participants must have a ShareValue for ExactAmount splits.");

            decimal totalExact = participants.Sum(p => p.ShareValue.Value);
            if (totalExact != totalAmount)
                throw new ValidationException($"Sum of exact amounts ({totalExact}) does not match total expense amount ({totalAmount}).");

            return participants.Select(p => new CalculatedShare
            {
                UserId = p.UserId,
                OwedAmount = p.ShareValue.Value,
                IsPayer = p.IsPayer
            });
        }

        private IEnumerable<CalculatedShare> CalculateWeighted(decimal totalAmount, List<SplitParticipantInput> participants)
        {
            if (participants.Any(p => !p.ShareValue.HasValue))
                throw new ValidationException("All participants must have a ShareValue for Template splits.");

            decimal totalWeight = participants.Sum(p => p.ShareValue.Value);
            if (totalWeight <= 0)
                throw new ValidationException("Total weight must be positive.");

            var results = new List<CalculatedShare>();
            decimal sumCalculated = 0m;

            // Calculate base amounts
            foreach (var p in participants)
            {
                decimal amount = Math.Floor((totalAmount * (p.ShareValue.Value / totalWeight)) * 100) / 100;
                sumCalculated += amount;
                results.Add(new CalculatedShare
                {
                    UserId = p.UserId,
                    OwedAmount = amount,
                    IsPayer = p.IsPayer
                });
            }

            // Distribute remainder cents to the first N users (since they are already sorted by UserId)
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
