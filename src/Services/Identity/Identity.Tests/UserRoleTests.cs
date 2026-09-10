using System.Net;
using System.Net.Http.Json;

namespace Identity.Tests;

public class UserRoleTests(IdentityWebApplicationFactory factory) : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Permissions_EndpointReturnsAllApplicationPermissions()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/account/permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var permissions = (await response.Content.ReadAsStringAsync()).ReadJson()
            .EnumerateArray()
            .Select(p => p.GetProperty("value").GetString())
            .ToArray();

        Assert.Contains("users.view", permissions);
        Assert.Contains("roles.manage", permissions);
    }

    [Fact]
    public async Task SeededAdministratorRole_HasAllPermissions()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/account/roles/name/administrator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var role = (await response.Content.ReadAsStringAsync()).ReadJson();
        var permissions = role.GetProperty("permissions").EnumerateArray()
            .Select(p => p.GetProperty("value").GetString())
            .ToArray();

        Assert.Contains("users.manage", permissions);
        Assert.Contains("roles.assign", permissions);
    }

    [Fact]
    public async Task Admin_CanCreateRoleWithPermissionsAndDeleteIt()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();
        var roleName = $"role-{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync("/api/account/roles", new
        {
            name = roleName,
            description = "Created by integration test",
            permissions = new[] { new { value = "users.view" } }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var roleId = (await createResponse.Content.ReadAsStringAsync()).ReadJson().GetProperty("id").GetString()!;

        var role = (await (await client.GetAsync($"/api/account/roles/{roleId}")).Content.ReadAsStringAsync()).ReadJson();
        var permissions = role.GetProperty("permissions").EnumerateArray()
            .Select(p => p.GetProperty("value").GetString())
            .ToArray();

        Assert.Equal("users.view", Assert.Single(permissions));

        var deleteResponse = await client.DeleteAsync($"/api/account/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }
}
