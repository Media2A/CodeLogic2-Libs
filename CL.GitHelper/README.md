# CL.GitHelper - Git Repository Management Library

Version 2.0.0 | CodeLogic Framework

## Overview

CL.GitHelper is a comprehensive Git repository management library for the CodeLogic Framework. Built on LibGit2Sharp, it provides a high-level API for Git operations with advanced features including repository caching, performance diagnostics, progress reporting, and full integration with the CodeLogic logging and configuration system.

## Features

- **Complete Git Operations**: Clone, fetch, pull, push, commit, branch management, and status tracking
- **Repository Caching**: Intelligent caching of repository instances with configurable timeout
- **Performance Diagnostics**: Detailed tracking of operation duration, object counts, and bytes transferred
- **Progress Reporting**: Real-time progress callbacks for long-running operations
- **Multi-Repository Support**: Manage multiple Git repositories with different configurations
- **Authentication Support**: HTTPS (username/password) and SSH (key-based) authentication
- **Batch Operations**: Execute operations across multiple repositories concurrently
- **Health Checks**: Monitor repository health and connectivity
- **CodeLogic Integration**: Seamless integration with logging, configuration, and dependency injection

## Installation

Add a project reference to CL.GitHelper in your CodeLogic application:

```xml
<ProjectReference Include="path\to\CL.GitHelper\CL.GitHelper.csproj" />
```

The library will be automatically loaded by the CodeLogic framework at startup.

## Configuration

Create a `config/git.json` file in your application directory:

```json
{
  "Repositories": [
    {
      "Id": "MyProject",
      "Name": "My Project Repository",
      "RepositoryUrl": "https://github.com/username/repository.git",
      "LocalPath": "repositories/my-project",
      "UseAppDataDir": true,
      "DefaultBranch": "main",
      "Username": "your-username",
      "Password": "your-pat-token",
      "AutoFetch": false,
      "AutoFetchIntervalMinutes": 0,
      "TimeoutSeconds": 300,
      "EnableProgressReporting": true,
      "EnableDiagnostics": true
    }
  ],
  "DefaultTimeoutSeconds": 300,
  "MaxConcurrentOperations": 3,
  "EnableGlobalDiagnostics": true,
  "EnableRepositoryCaching": true,
  "CacheTimeoutMinutes": 30
}
```

### Configuration Options

#### RepositoryConfiguration

- **Id**: Unique identifier for this repository
- **Name**: Display name for the repository
- **RepositoryUrl**: Remote repository URL (HTTPS or SSH)
- **LocalPath**: Local directory path for the repository
- **UseAppDataDir**: Use application data directory as base path
- **DefaultBranch**: Default branch name (e.g., "main", "master")
- **Username**: Username for HTTPS authentication (optional)
- **Password**: Password or Personal Access Token for HTTPS (optional)
- **SshKeyPath**: Path to SSH private key for SSH URLs (optional)
- **SshPassphrase**: SSH key passphrase (optional)
- **AutoFetch**: Automatically fetch on initialization
- **AutoFetchIntervalMinutes**: Auto-fetch interval in minutes (0 = disabled)
- **TimeoutSeconds**: Operation timeout in seconds
- **EnableProgressReporting**: Enable progress reporting callbacks
- **EnableDiagnostics**: Enable detailed diagnostics tracking

#### GitHelperConfiguration

- **Repositories**: List of repository configurations
- **DefaultTimeoutSeconds**: Default timeout for all operations
- **MaxConcurrentOperations**: Maximum concurrent batch operations
- **EnableGlobalDiagnostics**: Enable diagnostics across all repositories
- **EnableRepositoryCaching**: Cache repository instances for reuse
- **CacheTimeoutMinutes**: Cache expiration timeout in minutes

## Basic Usage

### Getting a Repository Instance

```csharp
// Get the GitHelper library
var gitHelper = framework.Libraries.GetLibrary<GitHelperLibrary>();

// Get a repository instance by ID
var repository = await gitHelper.GetRepositoryAsync("MyProject");
```

### Clone a Repository

```csharp
var cloneOptions = new CloneOptions
{
    BranchName = "main",
    Checkout = true,
    OnProgress = (progress) =>
    {
        Console.WriteLine($"{progress.Stage}: {progress.ProgressPercentage}% " +
                         $"({progress.ObjectsReceived}/{progress.TotalObjects} objects)");
    }
};

var result = await repository.CloneAsync(cloneOptions);

if (result.Success)
{
    Console.WriteLine($"Repository cloned successfully!");
    Console.WriteLine($"Duration: {result.Diagnostics.Duration}");
    Console.WriteLine($"Objects: {result.Diagnostics.ObjectCount}");
    Console.WriteLine($"Bytes: {result.Diagnostics.BytesTransferred:N0}");
}
```

### Fetch Updates

```csharp
var fetchOptions = new FetchOptions
{
    RemoteName = "origin",
    Prune = true,
    FetchTags = true
};

var result = await repository.FetchAsync(fetchOptions);

if (result.Success)
{
    Console.WriteLine("Fetch completed successfully");
}
```

### Pull Changes

```csharp
var pullOptions = new PullOptions
{
    RemoteName = "origin",
    Strategy = MergeStrategy.Merge
};

var result = await repository.PullAsync(pullOptions);

if (result.Success)
{
    Console.WriteLine($"Pull completed: {result.Data?.Status}");
}
```

### Commit Changes

```csharp
var commitOptions = new CommitOptions
{
    Message = "Updated configuration files",
    FilesToStage = new List<string> { "config/settings.json" }
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine($"Committed: {result.Data?.ShortSha} - {result.Data?.ShortMessage}");
}
```

### Push Changes

```csharp
var pushOptions = new PushOptions
{
    RemoteName = "origin",
    BranchName = "main",
    SetUpstream = true
};

var result = await repository.PushAsync(pushOptions);

if (result.Success)
{
    Console.WriteLine("Push completed successfully");
}
```

### Get Repository Status

```csharp
var statusResult = await repository.GetStatusAsync();

if (statusResult.Success)
{
    var status = statusResult.Data;
    Console.WriteLine($"Modified: {status.ModifiedFiles.Count}");
    Console.WriteLine($"Staged: {status.StagedFiles.Count}");
    Console.WriteLine($"Untracked: {status.UntrackedFiles.Count}");
}
```

### List Branches

```csharp
var branchesResult = await repository.ListBranchesAsync(includeRemote: true);

if (branchesResult.Success)
{
    foreach (var branch in branchesResult.Data)
    {
        var marker = branch.IsCurrent ? "*" : " ";
        Console.WriteLine($"{marker} {branch.FriendlyName}");
    }
}
```

## Advanced Features

### Repository Caching

The GitManager automatically caches repository instances based on configuration:

```csharp
var gitManager = gitHelper.GetGitManager();

// Get cache statistics
var stats = gitManager.GetCacheStatistics();
Console.WriteLine($"Cached repositories: {stats.TotalCachedRepositories}");

// Clear cache manually
await gitManager.ClearCacheAsync();
```

### Batch Operations

Execute operations across multiple repositories:

```csharp
var gitManager = gitHelper.GetGitManager();

// Fetch all repositories
var results = await gitManager.FetchAllAsync();

foreach (var (repoId, result) in results)
{
    Console.WriteLine($"{repoId}: {(result.Success ? "Success" : "Failed")}");
}

// Get status for all repositories
var statusResults = await gitManager.GetAllStatusAsync();

foreach (var (repoId, status) in statusResults)
{
    if (status.Success)
    {
        Console.WriteLine($"{repoId}: {status.Data.TotalChangedFiles} changes");
    }
}
```

### Health Checks

Monitor repository health:

```csharp
var gitManager = gitHelper.GetGitManager();

var healthResults = await gitManager.HealthCheckAsync();

foreach (var (repoId, isHealthy) in healthResults)
{
    Console.WriteLine($"{repoId}: {(isHealthy ? "Healthy" : "Unhealthy")}");
}
```

### Performance Diagnostics

All operations return detailed diagnostics:

```csharp
var result = await repository.CloneAsync();

if (result.Success)
{
    var diag = result.Diagnostics;
    Console.WriteLine($"Start: {diag.StartTime}");
    Console.WriteLine($"End: {diag.EndTime}");
    Console.WriteLine($"Duration: {diag.Duration}");
    Console.WriteLine($"Objects: {diag.ObjectCount}");
    Console.WriteLine($"Bytes: {diag.BytesTransferred:N0}");

    foreach (var message in diag.Messages)
    {
        Console.WriteLine($"  {message}");
    }
}
```

## Authentication

### HTTPS Authentication

Use username and Personal Access Token (PAT):

```json
{
  "RepositoryUrl": "https://github.com/username/repository.git",
  "Username": "your-username",
  "Password": "ghp_YourPersonalAccessToken"
}
```

### SSH Authentication

Use SSH key-based authentication:

```json
{
  "RepositoryUrl": "git@github.com:username/repository.git",
  "SshKeyPath": "C:\\Users\\YourUser\\.ssh\\id_rsa",
  "SshPassphrase": "optional-passphrase"
}
```

## Error Handling

All operations return `GitOperationResult<T>` with comprehensive error information:

```csharp
var result = await repository.FetchAsync();

if (!result.Success)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");

    if (result.Exception != null)
    {
        Console.WriteLine($"Exception: {result.Exception.Message}");
        Console.WriteLine($"Stack: {result.Exception.StackTrace}");
    }
}
```

## Thread Safety

All operations are thread-safe:
- GitRepository uses SemaphoreSlim for operation locking
- GitManager uses concurrent collections and locking for cache management
- Multiple repositories can be accessed concurrently

## Performance Considerations

- **Repository Caching**: Enable caching to reuse repository instances
- **Batch Operations**: Use batch methods to process multiple repositories efficiently
- **Concurrent Operations**: Configure `MaxConcurrentOperations` to control parallelism
- **Progress Reporting**: Disable progress callbacks for better performance if not needed
- **Diagnostics**: Disable diagnostics in production if not required

## Dependencies

- **LibGit2Sharp**: Git operations (v0.30.0+)
- **CL.Core**: CodeLogic core abstractions (v2.0.0+)
- **CodeLogic**: Main framework (v2.0.0+)

## Examples

See [EXAMPLES.md](EXAMPLES.md) for comprehensive examples covering all features.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and changes.

## License

Copyright © Media2A 2024

## Support

For issues, questions, or contributions, please contact Media2A.
