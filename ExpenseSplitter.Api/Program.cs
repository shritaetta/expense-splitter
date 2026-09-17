using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ExpenseSplitter.Api.Services.ISplitCalculator, ExpenseSplitter.Api.Services.SplitCalculator>();
builder.Services.AddSingleton<ExpenseSplitter.Api.Services.IProrationCalculator, ExpenseSplitter.Api.Services.ProrationCalculator>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.IRecurringExpenseGenerator, ExpenseSplitter.Api.Services.RecurringExpenseGenerator>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.IBalanceCalculator, ExpenseSplitter.Api.Services.BalanceCalculator>();
builder.Services.AddSingleton<ExpenseSplitter.Api.Services.ISettlementCalculator, ExpenseSplitter.Api.Services.SettlementCalculator>();
builder.Services.AddHostedService<ExpenseSplitter.Api.Services.RecurringExpenseBackgroundService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
