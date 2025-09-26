using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using AzureDevOpsMcp.Models;

namespace AzureDevOpsMcp.Services;

public class AzureDevOpsService
{
    private readonly string _organizationUrl;
    private readonly string _personalAccessToken;
    private readonly VssConnection _connection;

    public AzureDevOpsService(string organizationUrl, string personalAccessToken)
    {
        _organizationUrl = organizationUrl;
        _personalAccessToken = personalAccessToken;
        
        var credentials = new VssBasicCredential(string.Empty, _personalAccessToken);
        _connection = new VssConnection(new Uri(_organizationUrl), credentials);
    }

    public async Task<List<Models.Project>> GetProjectsAsync()
    {
        var projectClient = _connection.GetClient<ProjectHttpClient>();
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

    public async Task<List<Models.WorkItem>> GetWorkItemsAsync(string projectName, int top = 100)
    {
        var workItemClient = _connection.GetClient<WorkItemTrackingHttpClient>();
        
        var wiql = new Wiql()
        {
            Query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo], [System.CreatedDate], [System.ChangedDate] FROM WorkItems WHERE [System.TeamProject] = '{projectName}' ORDER BY [System.ChangedDate] DESC"
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

    public async Task<List<Models.Repository>> GetRepositoriesAsync(string projectName)
    {
        var gitClient = _connection.GetClient<GitHttpClient>();
        var repositories = await gitClient.GetRepositoriesAsync(projectName);

        return repositories.Select(repo => new Models.Repository
        {
            Id = repo.Id.ToString(),
            Name = repo.Name,
            Url = repo.WebUrl,
            DefaultBranch = repo.DefaultBranch
        }).ToList();
    }

    public async Task<List<Models.PullRequest>> GetPullRequestsAsync(string projectName, string repositoryId, int top = 100)
    {
        var gitClient = _connection.GetClient<GitHttpClient>();
        var pullRequests = await gitClient.GetPullRequestsAsync(projectName, repositoryId, 
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
