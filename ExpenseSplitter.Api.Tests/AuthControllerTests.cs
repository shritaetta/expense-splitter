using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using System.Net.Http.Headers;
using ExpenseSplitter.Api.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ExpenseSplitter.Api.Tests
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "InMemoryDbForAuthTesting_" + Guid.NewGuid().ToString();
            
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "JwtSettings:Secret", "VerySecureSecretKeyForTestingPurposesOnly!" },
                        { "JwtSettings:Issuer", "TestIssuer" },
                        { "JwtSettings:Audience", "TestAudience" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Use a unique in-memory database for each test run to ensure isolation
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });
                });
            });
        }

        [Fact]
        public async Task RegisterAndLogin_ReturnsValidJwt()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"test{Guid.NewGuid()}@example.com";
            var password = "StrongPassword123!";

            var registerDto = new RegisterDto
            {
                Email = email,
                Name = "Test User",
                Password = password
            };

            // Act - Register
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Assert
            registerResponse.EnsureSuccessStatusCode();
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(registerResult);
            Assert.NotEmpty(registerResult.Token);
            Assert.Equal(email, registerResult.User.Email);

            // Act - Login
            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(loginResult);
            Assert.NotEmpty(loginResult.Token);
            Assert.Equal(email, loginResult.User.Email);
        }

        [Fact]
        public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}/groups");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GroupEndpoint_WithTokenButNotMember_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"test{Guid.NewGuid()}@example.com";
            
            // Register a user to get a token
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Email = email,
                Name = "Test User",
                Password = "Password123!"
            });
            var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

            // Try to access a group they don't belong to
            var randomGroupId = Guid.NewGuid();
            var response = await client.GetAsync($"/api/groups/{randomGroupId}/members");
            
            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UserGroupsEndpoint_WithMismatchedUserId_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"test{Guid.NewGuid()}@example.com";
            
            // Register a user to get a token
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Email = email,
                Name = "Test User",
                Password = "Password123!"
            });
            var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

            // Try to access ANOTHER user's groups
            var randomUserId = Guid.NewGuid();
            var response = await client.GetAsync($"/api/users/{randomUserId}/groups");
            
            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
