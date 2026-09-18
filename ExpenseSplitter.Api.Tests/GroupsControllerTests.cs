using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.DTOs;
using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Tests
{
    public class GroupsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GroupsControllerTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "InMemoryDbForGroupsTesting_" + Guid.NewGuid().ToString();

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
        public async Task CreateGroup_AutoAddsCreatorAsMember()
        {
            // Arrange
            var client = _factory.CreateClient();
            var email = $"creator_{Guid.NewGuid()}@example.com";

            // 1. Register a user
            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Email = email,
                Name = "Group Creator",
                Password = "Password123!"
            });
            registerResponse.EnsureSuccessStatusCode();
            var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(authResult);
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

            // Act - 2. Create a Group
            var createGroupResponse = await client.PostAsJsonAsync("/api/groups", new CreateGroupDto
            {
                Name = "Test Trip",
                Currency = "USD"
            });
            createGroupResponse.EnsureSuccessStatusCode();
            var groupResult = await createGroupResponse.Content.ReadFromJsonAsync<GroupDto>();
            Assert.NotNull(groupResult);

            // Act - 3. GET its members (This route is protected by RequireGroupMember, so this proves it works too)
            var getMembersResponse = await client.GetAsync($"/api/groups/{groupResult.Id}/members");
            getMembersResponse.EnsureSuccessStatusCode();
            
            var members = await getMembersResponse.Content.ReadFromJsonAsync<IEnumerable<GroupMemberDto>>();
            
            // Assert
            Assert.NotNull(members);
            Assert.Single(members); // Only 1 member
            var onlyMember = members.First();
            Assert.Equal(authResult.User.Id, onlyMember.UserId);
            Assert.Equal("Group Creator", onlyMember.UserName);
        }
    }
}
