using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Identity.API.Authorization;
using Identity.API.Authorization.Requirements;
using Identity.API.Configuration;
using Identity.API.Services;
using Identity.Domain.Authorization;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

/************* ADD SERVICES *************/

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var migrationsAssembly = typeof(IdentityDbContext).GetTypeInfo().Assembly.GetName().Name;

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
    options.UseOpenIddict();
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity options and password complexity here
builder.Services.Configure<IdentityOptions>(options =>
{
    // User settings
    options.User.RequireUniqueEmail = true;

    // Configure Identity to use the same JWT claims as OpenIddict
    options.ClaimsIdentity.UserNameClaimType = Claims.Name;
    options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
    options.ClaimsIdentity.RoleClaimType = Claims.Role;
    options.ClaimsIdentity.EmailClaimType = Claims.Email;
});

// Configure OpenIddict periodic pruning of orphaned authorizations/tokens from the database.
builder.Services.AddQuartz(options =>
{
    options.UseSimpleTypeLoader();
    options.UseInMemoryStore();
});

// Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>();

        options.UseQuartz();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token");

        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        options.RegisterScopes(
            Scopes.Profile,
            Scopes.Email,
            Scopes.Address,
            Scopes.Phone,
            Scopes.Roles);

        var oidcCertFileName = builder.Configuration["OIDC:Certificates:Path"];
        var oidcCertFilePassword = builder.Configuration["OIDC:Certificates:Password"];

        if (!string.IsNullOrWhiteSpace(oidcCertFileName))
        {
            var oidcCertificate = X509CertificateLoader.LoadPkcs12FromFile(oidcCertFileName, oidcCertFilePassword);

            options.AddEncryptionCertificate(oidcCertificate)
                   .AddSigningCertificate(oidcCertificate);
        }
        else
        {
            // You must configure persisted keys for Encryption and Signing in production.
            // See https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html
            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey();
        }

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    o.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.ViewAllUsersPolicy,
        policy => policy.RequireClaim(CustomClaims.Permission, ApplicationPermissions.ViewUsers))
    .AddPolicy(AuthPolicies.ManageAllUsersPolicy,
        policy => policy.RequireClaim(CustomClaims.Permission, ApplicationPermissions.ManageUsers))
    .AddPolicy(AuthPolicies.ViewAllRolesPolicy,
        policy => policy.RequireClaim(CustomClaims.Permission, ApplicationPermissions.ViewRoles))
    .AddPolicy(AuthPolicies.ViewRoleByRoleNamePolicy,
        policy => policy.Requirements.Add(new ViewRoleAuthorizationRequirement()))
    .AddPolicy(AuthPolicies.ManageAllRolesPolicy,
        policy => policy.RequireClaim(CustomClaims.Permission, ApplicationPermissions.ManageRoles))
    .AddPolicy(AuthPolicies.AssignAllowedRolesPolicy,
        policy => policy.Requirements.Add(new AssignRolesAuthorizationRequirement()));

// Add cors
builder.Services.AddCors();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHealthChecks();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = OidcServerConfig.ServerName, Version = "v1" });
    c.OperationFilter<SwaggerAuthorizeOperationFilter>();
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Password = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/connect/token", UriKind.Relative)
            }
        }
    });
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

// Business Services
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();

// Other Services
builder.Services.AddScoped<IUserIdAccessor, UserIdAccessor>();

// Auth Handlers
builder.Services.AddSingleton<IAuthorizationHandler, ViewUserAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ManageUserAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ViewRoleAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AssignRolesAuthorizationHandler>();

// DB Creation and Seeding
builder.Services.AddTransient<IDatabaseSeeder, DatabaseSeeder>();

var app = builder.Build();

/************* CONFIGURE REQUEST PIPELINE *************/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.DocumentTitle = "Swagger UI - Identity";
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{OidcServerConfig.ServerName} V1");
        c.OAuthClientId(OidcServerConfig.SwaggerClientID);
    });

    IdentityModelEventSource.ShowPII = true;
}

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

/************* SEED DATABASE *************/

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbSeeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await dbSeeder.SeedAsync();

        await OidcServerConfig.RegisterClientApplicationsAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "An error occurred whilst creating/seeding database");

        throw;
    }
}

/************* RUN APP *************/

app.Run();

public partial class Program;
