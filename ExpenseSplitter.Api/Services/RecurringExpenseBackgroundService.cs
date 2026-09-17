using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services
{
    public class RecurringExpenseBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RecurringExpenseBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Simple timer that checks every 1 hour.
            // For testing purposes in a real environment, you might want this to run daily.
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessRecurringExpenses(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }

        public async Task ProcessRecurringExpenses(CancellationToken stoppingToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var generator = scope.ServiceProvider.GetRequiredService<IRecurringExpenseGenerator>();

            var now = DateTimeOffset.UtcNow;

            var dueExpenses = await context.RecurringExpenses
                .Where(re => re.NextRunDate <= now)
                .ToListAsync(stoppingToken);

            foreach (var re in dueExpenses)
            {
                // Create the transaction to ensure generating the expense AND bumping the date are atomic
                using var transaction = await context.Database.BeginTransactionAsync(stoppingToken);

                try
                {
                    // 1. Generate the expense via the shared generator
                    var expense = await generator.GenerateExpenseAsync(re, stoppingToken);

                    // 2. Add to context
                    context.Expenses.Add(expense);

                    // 3. Bump NextRunDate
                    re.NextRunDate = re.Frequency == "Monthly" ? re.NextRunDate.AddMonths(1) : re.NextRunDate.AddDays(30);

                    // 4. Commit everything together
                    await context.SaveChangesAsync(stoppingToken);
                    await transaction.CommitAsync(stoppingToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(stoppingToken);
                    // In a production app, log this error so it can be monitored
                }
            }
        }
    }
}
