using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using AzureDevOpsMcp.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AzureDevOpsMcp.Tools;

[McpServerToolType]
public static class AzureDevOpsTools
{
    private static VssConnection GetConnection()
    {
        var orgUrl = Environment.GetEnvironmentVariable("AZURE_DEVOPS_ORG_URL") 
            ?? throw new InvalidOperationException("AZURE_DEVOPS_ORG_URL environment variable is required");
        var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT") 
            ?? throw new InvalidOperationException("AZURE_DEVOPS_PAT environment variable is required");
        
        var credentials = new VssBasicCredential(string.Empty, pat);
        return new VssConnection(new Uri(orgUrl), credentials);
    }

    [McpServerTool, Description("Get all projects in the Azure DevOps organization")]
    public static async Task<List<Models.Project>> GetProjects()
    {
        using var connection = GetConnection();
        var projectClient = connection.GetClient<ProjectHttpClient>();
        var projects = await projectClient.GetProjects();
        
        return projects.Select(p => new Models.Project
        {
            Id = p.Id.ToString(),
            Name = p.Name,
            Description = p.Description,
            Url = p.Url,
            State = p.State.ToString()
        }).ToList();
    }

    [McpServerTool, Description("Get work items from a specific project")]
    public static async Task<List<Models.WorkItem>> GetWorkItems(
        [Description("The name of the project")] string project,
        [Description("Maximum number of work items to return")] int top = 100)
    {
        using var connection = GetConnection();
        var workItemClient = connection.GetClient<WorkItemTrackingHttpClient>();
        
        var wiql = new Wiql()
        {
            Query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo], [System.CreatedDate], [System.ChangedDate] FROM WorkItems WHERE [System.TeamProject] = '{project}' ORDER BY [System.ChangedDate] DESC"
        };

        var result = await workItemClient.QueryByWiqlAsync(wiql, top: top);
        
        if (result.WorkItems?.Any() != true)
            return new List<Models.WorkItem>();

        var workItemIds = result.WorkItems.Select(wi => wi.Id).ToArray();
        var workItems = await workItemClient.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.Fields);

        return workItems.Select(wi => new Models.WorkItem
        {
            Id = wi.Id ?? 0,
            Title = wi.Fields.GetValueOrDefault("System.Title")?.ToString() ?? string.Empty,
            State = wi.Fields.GetValueOrDefault("System.State")?.ToString() ?? string.Empty,
            WorkItemType = wi.Fields.GetValueOrDefault("System.WorkItemType")?.ToString() ?? string.Empty,
            AssignedTo = wi.Fields.GetValueOrDefault("System.AssignedTo")?.ToString(),
            CreatedDate = DateTime.Parse(wi.Fields.GetValueOrDefault("System.CreatedDate")?.ToString() ?? DateTime.MinValue.ToString()),
            ChangedDate = DateTime.Parse(wi.Fields.GetValueOrDefault("System.ChangedDate")?.ToString() ?? DateTime.MinValue.ToString()),
            Url = wi.Url ?? string.Empty
        }).ToList();
    }

    [McpServerTool, Description("Get repositories from a specific project")]
    public static async Task<List<Models.Repository>> GetRepositories(
        [Description("The name of the project")] string project)
    {
        using var connection = GetConnection();
        var gitClient = connection.GetClient<GitHttpClient>();
        var repositories = await gitClient.GetRepositoriesAsync(project);

        return repositories.Select(repo => new Models.Repository
        {
            Id = repo.Id.ToString(),
            Name = repo.Name,
            Url = repo.WebUrl,
            DefaultBranch = repo.DefaultBranch
        }).ToList();
    }

    [McpServerTool, Description("Get pull requests from a specific repository")]
    public static async Task<List<Models.PullRequest>> GetPullRequests(
        [Description("The name of the project")] string project,
        [Description("The ID of the repository")] string repositoryId,
        [Description("Maximum number of pull requests to return")] int top = 100)
    {
        using var connection = GetConnection();
        var gitClient = connection.GetClient<GitHttpClient>();
        var pullRequests = await gitClient.GetPullRequestsAsync(project, repositoryId, 
            new GitPullRequestSearchCriteria { Status = PullRequestStatus.All }, top: top);

        return pullRequests.Select(pr => new Models.PullRequest
        {
            PullRequestId = pr.PullRequestId,
            Title = pr.Title,
            Description = pr.Description,
            Status = pr.Status.ToString(),
            CreatedBy = pr.CreatedBy?.DisplayName ?? string.Empty,
            CreationDate = pr.CreationDate,
            SourceRefName = pr.SourceRefName,
            TargetRefName = pr.TargetRefName,
            Url = pr.Url
        }).ToList();
    }
}
