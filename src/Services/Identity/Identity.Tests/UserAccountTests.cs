using System.Net;
using System.Net.Http.Json;

namespace Identity.Tests;

public class UserAccountTests(IdentityWebApplicationFactory factory) : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory = factory;

    private static object NewUser(string userName) => new
    {
        userName,
        fullName = "Integration Test User",
        email = $"{userName}@quickapp.test",
        jobTitle = "Tester",
        isEnabled = true,
        roles = new[] { "user" },
        newPassword = "tempP@ss123",
        confirmPassword = "tempP@ss123"
    };

    [Fact]
    public async Task Admin_CanCreateReadUpdateAndDeleteUsers()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();
        var userName = $"crud-{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync("/api/account/users", NewUser(userName));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = (await createResponse.Content.ReadAsStringAsync()).ReadJson();
        var userId = created.GetProperty("id").GetString()!;

        var getResponse = await client.GetAsync($"/api/account/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync($"/api/account/users/{userId}", new
        {
            id = userId,
            userName,
            fullName = "Updated Name",
            email = $"{userName}@quickapp.test",
            jobTitle = "Senior Tester",
            isEnabled = true,
            roles = new[] { "user" }
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updated = (await (await client.GetAsync($"/api/account/users/{userId}")).Content.ReadAsStringAsync()).ReadJson();
        Assert.Equal("Updated Name", updated.GetProperty("fullName").GetString());

        var deleteResponse = await client.DeleteAsync($"/api/account/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var afterDelete = await client.GetAsync($"/api/account/users/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAssignRolesToUser()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();
        var userName = $"role-{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync("/api/account/users", NewUser(userName));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var userId = (await createResponse.Content.ReadAsStringAsync()).ReadJson().GetProperty("id").GetString()!;

        var updateResponse = await client.PutAsJsonAsync($"/api/account/users/{userId}", new
        {
            id = userId,
            userName,
            fullName = "Integration Test User",
            email = $"{userName}@quickapp.test",
            isEnabled = true,
            roles = new[] { "user", "administrator" }
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var reloaded = (await (await client.GetAsync($"/api/account/users/{userId}")).Content.ReadAsStringAsync()).ReadJson();
        var roles = reloaded.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray();

        Assert.Contains("administrator", roles);
        Assert.Contains("user", roles);

        await client.DeleteAsync($"/api/account/users/{userId}");
    }

    [Fact]
    public async Task StandardUser_CannotListAllUsers()
    {
        var client = await _factory.CreateClient()
            .AuthenticateAsync(TestClientExtensions.StandardUserName, TestClientExtensions.DefaultPassword);

        var response = await client.GetAsync("/api/account/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
