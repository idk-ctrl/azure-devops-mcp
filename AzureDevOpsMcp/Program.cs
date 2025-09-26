using AzureDevOpsMcp.Models;
using AzureDevOpsMcp.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<AzureDevOpsService>(provider =>
{
    var orgUrl = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL") 
        ?? throw new InvalidOperationException("AZURE_DEVOPS_ORG_URL environment variable is required");
    var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT") 
        ?? throw new InvalidOperationException("AZURE_DEVOPS_PAT environment variable is required");
    
    return new AzureDevOpsService(orgUrl, pat);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/mcp/info", () => new McpResponse<object>
{
    Id = "info",
    Result = new
    {
        name = "azure-devops-mcp",
        version = "1.0.0",
        description = "MCP Server for Azure DevOps integration",
        capabilities = new
        {
            tools = new[]
            {
                "get_projects",
                "get_work_items", 
                "get_repositories",
                "get_pull_requests"
            }
        }
    }
})
.WithName("GetMcpInfo")
.WithOpenApi();

app.MapPost("/mcp/tools", async (McpRequest request, AzureDevOpsService azureDevOpsService) =>
{
    try
    {
        return request.Method switch
        {
            "get_projects" => new McpResponse<object>
            {
                Id = request.Id,
                Result = await azureDevOpsService.GetProjectsAsync()
            },
            "get_work_items" => await HandleGetWorkItems(request, azureDevOpsService),
            "get_repositories" => await HandleGetRepositories(request, azureDevOpsService),
            "get_pull_requests" => await HandleGetPullRequests(request, azureDevOpsService),
            _ => new McpResponse<object>
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32601,
                    Message = $"Method '{request.Method}' not found"
                }
            }
        };
    }
    catch (Exception ex)
    {
        return new McpResponse<object>
        {
            Id = request.Id,
            Error = new McpError
            {
                Code = -32603,
                Message = "Internal error",
                Data = ex.Message
            }
        };
    }
})
.WithName("HandleMcpTools")
.WithOpenApi();

app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

async Task<McpResponse<object>> HandleGetWorkItems(McpRequest request, AzureDevOpsService service)
{
    var paramsJson = JsonSerializer.Serialize(request.Params);
    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);
    
    var projectName = parameters?.GetValueOrDefault("project")?.ToString() 
        ?? throw new ArgumentException("Project parameter is required");
    var top = int.Parse(parameters?.GetValueOrDefault("top")?.ToString() ?? "100");
    
    var workItems = await service.GetWorkItemsAsync(projectName, top);
    
    return new McpResponse<object>
    {
        Id = request.Id,
        Result = workItems
    };
}

async Task<McpResponse<object>> HandleGetRepositories(McpRequest request, AzureDevOpsService service)
{
    var paramsJson = JsonSerializer.Serialize(request.Params);
    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);
    
    var projectName = parameters?.GetValueOrDefault("project")?.ToString() 
        ?? throw new ArgumentException("Project parameter is required");
    
    var repositories = await service.GetRepositoriesAsync(projectName);
    
    return new McpResponse<object>
    {
        Id = request.Id,
        Result = repositories
    };
}

async Task<McpResponse<object>> HandleGetPullRequests(McpRequest request, AzureDevOpsService service)
{
    var paramsJson = JsonSerializer.Serialize(request.Params);
    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsJson);
    
    var projectName = parameters?.GetValueOrDefault("project")?.ToString() 
        ?? throw new ArgumentException("Project parameter is required");
    var repositoryId = parameters?.GetValueOrDefault("repositoryId")?.ToString() 
        ?? throw new ArgumentException("RepositoryId parameter is required");
    var top = int.Parse(parameters?.GetValueOrDefault("top")?.ToString() ?? "100");
    
    var pullRequests = await service.GetPullRequestsAsync(projectName, repositoryId, top);
    
    return new McpResponse<object>
    {
        Id = request.Id,
        Result = pullRequests
    };
}
