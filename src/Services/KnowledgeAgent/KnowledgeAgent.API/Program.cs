using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["AppInsights:ConnectionString"];
});

// Add Knowledge Agent infrastructure services
builder.Services.AddKnowledgeAgentInfrastructure(builder.Configuration);

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SharePoint Knowledge Agent API",
        Version = "v1",
        Description = "AI Agent that reads SharePoint documents, creates Knowledge Articles, " +
                      "stores them in a vector database, and integrates with Application Insights " +
                      "to provide relevant knowledge when issues are detected."
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize vector store on startup
using (var scope = app.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();
    await vectorStore.InitializeAsync();
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge Agent API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
