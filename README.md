# Azure DevOps MCP Server

A Model Context Protocol (MCP) server for Azure DevOps integration, built with .NET 8 Web API.

## Features

- **Projects**: List all projects in your Azure DevOps organization
- **Work Items**: Retrieve work items from specific projects
- **Repositories**: Get repository information for projects
- **Pull Requests**: Fetch pull requests from repositories
- **Health Check**: Monitor server status

## Prerequisites

- .NET 8 SDK
- Azure DevOps organization with Personal Access Token (PAT)

## Environment Variables

The following environment variables are required:

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_DEVOPS_ORG_URL` | Your Azure DevOps organization URL | `https://dev.azure.com/yourorg` |
| `AZURE_DEVOPS_PAT` | Personal Access Token with appropriate permissions | `your-pat-token-here` |

### Setting Environment Variables

#### Windows (Command Prompt)
```cmd
set AZURE_DEVOPS_ORG_URL=https://dev.azure.com/yourorg
set AZURE_DEVOPS_PAT=your-pat-token-here
```

#### Windows (PowerShell)
```powershell
$env:AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg"
$env:AZURE_DEVOPS_PAT="your-pat-token-here"
```

#### Linux/macOS
```bash
export AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg"
export AZURE_DEVOPS_PAT="your-pat-token-here"
```

#### Using .env file (for development)
Create a `.env` file in the project root:
```
AZURE_DEVOPS_ORG_URL=https://dev.azure.com/yourorg
AZURE_DEVOPS_PAT=your-pat-token-here
```

## Running Locally

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd azure-devops-mcp
   ```

2. **Set environment variables** (see above)

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Health Check
```bash
curl -X GET "https://localhost:5001/health"
```

### MCP Server Info
```bash
curl -X GET "https://localhost:5001/mcp/info"
```

### Get Projects
```bash
curl -X POST "https://localhost:5001/mcp/tools" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "get_projects"
  }'
```

### Get Work Items
```bash
curl -X POST "https://localhost:5001/mcp/tools" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "2",
    "method": "get_work_items",
    "params": {
      "project": "YourProjectName",
      "top": 50
    }
  }'
```

### Get Repositories
```bash
curl -X POST "https://localhost:5001/mcp/tools" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "3",
    "method": "get_repositories",
    "params": {
      "project": "YourProjectName"
    }
  }'
```

### Get Pull Requests
```bash
curl -X POST "https://localhost:5001/mcp/tools" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "4",
    "method": "get_pull_requests",
    "params": {
      "project": "YourProjectName",
      "repositoryId": "repository-guid",
      "top": 25
    }
  }'
```

## Using HTTPie

If you prefer HTTPie over curl:

### Get Projects
```bash
http POST localhost:5001/mcp/tools \
  jsonrpc="2.0" \
  id="1" \
  method="get_projects"
```

### Get Work Items
```bash
http POST localhost:5001/mcp/tools \
  jsonrpc="2.0" \
  id="2" \
  method="get_work_items" \
  params:='{"project": "YourProjectName", "top": 50}'
```

## Deployment to Azure Web App

### Option 1: Using GitHub Actions

1. **Create Azure Web App**
   ```bash
   az webapp create \
     --resource-group myResourceGroup \
     --plan myAppServicePlan \
     --name azure-devops-mcp \
     --runtime "DOTNETCORE:8.0"
   ```

2. **Set up GitHub Actions workflow** (`.github/workflows/deploy.yml`):
   ```yaml
   name: Deploy to Azure Web App

   on:
     push:
       branches: [ main ]

   jobs:
     deploy:
       runs-on: ubuntu-latest
       
       steps:
       - uses: actions/checkout@v3
       
       - name: Setup .NET
         uses: actions/setup-dotnet@v3
         with:
           dotnet-version: '8.0.x'
           
       - name: Restore dependencies
         run: dotnet restore
         
       - name: Build
         run: dotnet build --configuration Release --no-restore
         
       - name: Publish
         run: dotnet publish --configuration Release --no-build --output ./publish
         
       - name: Deploy to Azure Web App
         uses: azure/webapps-deploy@v2
         with:
           app-name: 'azure-devops-mcp'
           publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
           package: ./publish
   ```

3. **Configure environment variables in Azure**
   ```bash
   az webapp config appsettings set \
     --resource-group myResourceGroup \
     --name azure-devops-mcp \
     --settings AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg" \
                AZURE_DEVOPS_PAT="your-pat-token"
   ```

### Option 2: Using Azure CLI (`az webapp up`)

1. **Build and publish the application**
   ```bash
   dotnet publish --configuration Release --output ./publish
   ```

2. **Deploy using az webapp up**
   ```bash
   cd publish
   az webapp up \
     --name azure-devops-mcp \
     --resource-group myResourceGroup \
     --plan myAppServicePlan \
     --runtime "DOTNETCORE:8.0"
   ```

3. **Set environment variables**
   ```bash
   az webapp config appsettings set \
     --resource-group myResourceGroup \
     --name azure-devops-mcp \
     --settings AZURE_DEVOPS_ORG_URL="https://dev.azure.com/yourorg" \
                AZURE_DEVOPS_PAT="your-pat-token"
   ```

## Azure DevOps PAT Permissions

Your Personal Access Token should have the following permissions:
- **Project and Team**: Read
- **Work Items**: Read
- **Code**: Read (for repositories and pull requests)

## Project Structure

```
azure-devops-mcp/
├── AzureDevOpsMcp/
│   ├── Models/
│   │   ├── AzureDevOpsModels.cs
│   │   └── McpRequest.cs
│   ├── Services/
│   │   └── AzureDevOpsService.cs
│   ├── Program.cs
│   ├── AzureDevOpsMcp.csproj
│   └── appsettings.json
└── README.md
```

## Error Handling

The API follows JSON-RPC 2.0 error format:

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": "Detailed error message"
  }
}
```

Common error codes:
- `-32601`: Method not found
- `-32602`: Invalid params
- `-32603`: Internal error

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.
