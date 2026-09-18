using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ExpenseSplitter.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.IAuthService, ExpenseSplitter.Api.Services.AuthService>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.ISplitCalculator, ExpenseSplitter.Api.Services.SplitCalculator>();
builder.Services.AddSingleton<ExpenseSplitter.Api.Services.IProrationCalculator, ExpenseSplitter.Api.Services.ProrationCalculator>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.IRecurringExpenseGenerator, ExpenseSplitter.Api.Services.RecurringExpenseGenerator>();
builder.Services.AddScoped<ExpenseSplitter.Api.Services.IBalanceCalculator, ExpenseSplitter.Api.Services.BalanceCalculator>();
builder.Services.AddSingleton<ExpenseSplitter.Api.Services.ISettlementCalculator, ExpenseSplitter.Api.Services.SettlementCalculator>();
builder.Services.AddHostedService<ExpenseSplitter.Api.Services.RecurringExpenseBackgroundService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? ""))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
