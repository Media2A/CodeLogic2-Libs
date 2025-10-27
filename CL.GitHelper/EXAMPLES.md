# CL.GitHelper - Comprehensive Examples

This document provides detailed examples for using the CL.GitHelper library.

## Table of Contents

1. [Basic Setup](#basic-setup)
2. [Cloning Repositories](#cloning-repositories)
3. [Fetching and Pulling](#fetching-and-pulling)
4. [Committing Changes](#committing-changes)
5. [Pushing Changes](#pushing-changes)
6. [Branch Management](#branch-management)
7. [Repository Status](#repository-status)
8. [Batch Operations](#batch-operations)
9. [Advanced Scenarios](#advanced-scenarios)
10. [Error Handling](#error-handling)

## Basic Setup

### Getting Started with GitHelper

```csharp
using CL.GitHelper;
using CL.GitHelper.Models;
using CL.GitHelper.Services;

// Get the GitHelper library from CodeLogic framework
var gitHelper = framework.Libraries.GetLibrary<GitHelperLibrary>();

// Get a repository instance
var repository = await gitHelper.GetRepositoryAsync("MyProject");
```

### Registering a New Repository at Runtime

```csharp
var config = new RepositoryConfiguration
{
    Id = "NewRepo",
    Name = "New Repository",
    RepositoryUrl = "https://github.com/username/new-repo.git",
    LocalPath = "repositories/new-repo",
    UseAppDataDir = true,
    DefaultBranch = "main",
    Username = "username",
    Password = "ghp_token",
    EnableDiagnostics = true
};

gitHelper.RegisterRepository(config);

var newRepo = await gitHelper.GetRepositoryAsync("NewRepo");
```

## Cloning Repositories

### Basic Clone

```csharp
var result = await repository.CloneAsync();

if (result.Success)
{
    Console.WriteLine($"Repository cloned successfully!");
    Console.WriteLine($"Current branch: {result.Data?.CurrentBranch}");
}
else
{
    Console.WriteLine($"Clone failed: {result.ErrorMessage}");
}
```

### Clone with Progress Reporting

```csharp
var cloneOptions = new CloneOptions
{
    BranchName = "main",
    Checkout = true,
    OnProgress = (progress) =>
    {
        Console.Write($"\r{progress.Stage}: {progress.ProgressPercentage}% ");
        Console.Write($"({progress.ObjectsReceived}/{progress.TotalObjects} objects, ");
        Console.Write($"{progress.BytesReceived:N0} bytes)");
    }
};

var result = await repository.CloneAsync(cloneOptions);

Console.WriteLine(); // New line after progress

if (result.Success)
{
    Console.WriteLine("\nClone completed!");
    Console.WriteLine($"Duration: {result.Diagnostics.Duration}");
    Console.WriteLine($"Total objects: {result.Diagnostics.ObjectCount}");
    Console.WriteLine($"Total bytes: {result.Diagnostics.BytesTransferred:N0}");
}
```

### Shallow Clone

```csharp
var cloneOptions = new CloneOptions
{
    Depth = 1, // Only fetch the latest commit
    SingleBranch = true,
    BranchName = "main"
};

var result = await repository.CloneAsync(cloneOptions);

if (result.Success)
{
    Console.WriteLine("Shallow clone completed (depth: 1)");
}
```

### Clone with Submodules

```csharp
var cloneOptions = new CloneOptions
{
    RecurseSubmodules = true
};

var result = await repository.CloneAsync(cloneOptions);

if (result.Success)
{
    Console.WriteLine("Repository with submodules cloned successfully");
}
```

### Bare Clone (No Working Directory)

```csharp
var cloneOptions = new CloneOptions
{
    Bare = true,
    Checkout = false
};

var result = await repository.CloneAsync(cloneOptions);

if (result.Success)
{
    Console.WriteLine("Bare repository cloned successfully");
}
```

## Fetching and Pulling

### Basic Fetch

```csharp
var result = await repository.FetchAsync();

if (result.Success)
{
    Console.WriteLine("Fetch completed successfully");
    Console.WriteLine($"Duration: {result.Diagnostics.Duration}");
}
```

### Fetch with Options

```csharp
var fetchOptions = new FetchOptions
{
    RemoteName = "origin",
    Prune = true, // Remove deleted remote branches
    FetchTags = true,
    OnProgress = (progress) =>
    {
        Console.WriteLine($"{progress.Stage}: {progress.ProgressPercentage}%");
    }
};

var result = await repository.FetchAsync(fetchOptions);

if (result.Success)
{
    Console.WriteLine("Fetch completed with pruning and tags");
}
```

### Basic Pull

```csharp
var result = await repository.PullAsync();

if (result.Success)
{
    Console.WriteLine($"Pull completed: {result.Data?.Status}");
}
```

### Pull with Rebase

```csharp
var pullOptions = new PullOptions
{
    RemoteName = "origin",
    Strategy = MergeStrategy.Rebase
};

var result = await repository.PullAsync(pullOptions);

if (result.Success)
{
    Console.WriteLine($"Pull with rebase completed: {result.Data?.Status}");
}
```

### Pull with Fast-Forward Only

```csharp
var pullOptions = new PullOptions
{
    Strategy = MergeStrategy.FastForwardOnly
};

var result = await repository.PullAsync(pullOptions);

if (result.Success)
{
    Console.WriteLine("Fast-forward pull completed");
}
else
{
    Console.WriteLine("Fast-forward not possible, merge required");
}
```

## Committing Changes

### Basic Commit (All Staged Files)

```csharp
var commitOptions = new CommitOptions
{
    Message = "Update configuration files"
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine($"Committed: {result.Data?.Sha}");
    Console.WriteLine($"Message: {result.Data?.Message}");
    Console.WriteLine($"Author: {result.Data?.AuthorName}");
}
```

### Commit Specific Files

```csharp
var commitOptions = new CommitOptions
{
    Message = "Update README and configuration",
    FilesToStage = new List<string>
    {
        "README.md",
        "config/settings.json"
    }
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine($"Committed {result.Data?.FilesChanged} file(s)");
    Console.WriteLine($"+{result.Data?.LinesAdded} -{result.Data?.LinesDeleted}");
}
```

### Commit with Custom Author

```csharp
var commitOptions = new CommitOptions
{
    Message = "Feature implementation",
    AuthorName = "John Doe",
    AuthorEmail = "john.doe@example.com"
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine($"Committed by: {result.Data?.AuthorName} <{result.Data?.AuthorEmail}>");
}
```

### Allow Empty Commit

```csharp
var commitOptions = new CommitOptions
{
    Message = "Trigger CI pipeline",
    AllowEmpty = true
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine("Empty commit created successfully");
}
```

### Amend Last Commit

```csharp
var commitOptions = new CommitOptions
{
    Message = "Updated commit message",
    Amend = true
};

var result = await repository.CommitAsync(commitOptions);

if (result.Success)
{
    Console.WriteLine("Last commit amended successfully");
}
```

## Pushing Changes

### Basic Push

```csharp
var result = await repository.PushAsync();

if (result.Success)
{
    Console.WriteLine("Push completed successfully");
}
```

### Push with Options

```csharp
var pushOptions = new PushOptions
{
    RemoteName = "origin",
    BranchName = "feature/new-feature",
    SetUpstream = true,
    OnProgress = (progress) =>
    {
        Console.WriteLine($"{progress.Stage}: {progress.ProgressPercentage}%");
    }
};

var result = await repository.PushAsync(pushOptions);

if (result.Success)
{
    Console.WriteLine("Push completed and upstream set");
}
```

### Push with Tags

```csharp
var pushOptions = new PushOptions
{
    PushTags = true
};

var result = await repository.PushAsync(pushOptions);

if (result.Success)
{
    Console.WriteLine("Push completed with tags");
}
```

### Force Push (Use with Caution!)

```csharp
var pushOptions = new PushOptions
{
    Force = true
};

var result = await repository.PushAsync(pushOptions);

if (result.Success)
{
    Console.WriteLine("Force push completed");
}
```

## Branch Management

### List All Branches

```csharp
var result = await repository.ListBranchesAsync(includeRemote: true);

if (result.Success)
{
    Console.WriteLine("Branches:");

    foreach (var branch in result.Data)
    {
        var marker = branch.IsCurrent ? "*" : " ";
        var remote = branch.IsRemote ? "(remote)" : "";

        Console.WriteLine($"{marker} {branch.FriendlyName} {remote}");

        if (branch.IsTracking)
        {
            Console.WriteLine($"  Tracking: {branch.TrackedBranchName}");
            Console.WriteLine($"  Ahead: {branch.AheadBy}, Behind: {branch.BehindBy}");
        }
    }
}
```

### Checkout Branch

```csharp
var result = await repository.CheckoutBranchAsync("feature/new-feature");

if (result.Success)
{
    Console.WriteLine("Switched to branch: feature/new-feature");
}
else
{
    Console.WriteLine($"Checkout failed: {result.ErrorMessage}");
}
```

### Create and Checkout New Branch

```csharp
// First create the branch by checking it out
var result = await repository.CheckoutBranchAsync("feature/awesome-feature");

if (result.Success)
{
    Console.WriteLine("Created and switched to new branch");
}
```

## Repository Status

### Get Basic Status

```csharp
var result = await repository.GetStatusAsync();

if (result.Success)
{
    var status = result.Data;

    Console.WriteLine($"Repository is {(status.IsDirty ? "dirty" : "clean")}");
    Console.WriteLine($"Modified files: {status.ModifiedFiles.Count}");
    Console.WriteLine($"Staged files: {status.StagedFiles.Count}");
    Console.WriteLine($"Untracked files: {status.UntrackedFiles.Count}");
    Console.WriteLine($"Total changes: {status.TotalChangedFiles}");
}
```

### Get Detailed Status

```csharp
var result = await repository.GetStatusAsync();

if (result.Success)
{
    var status = result.Data;

    Console.WriteLine("Modified Files:");
    foreach (var file in status.ModifiedFiles)
    {
        Console.WriteLine($"  M {file.FilePath}");
    }

    Console.WriteLine("\nStaged Files:");
    foreach (var file in status.StagedFiles)
    {
        Console.WriteLine($"  A {file.FilePath}");
    }

    Console.WriteLine("\nUntracked Files:");
    foreach (var file in status.UntrackedFiles)
    {
        Console.WriteLine($"  ? {file.FilePath}");
    }

    if (status.ConflictedFiles.Count > 0)
    {
        Console.WriteLine("\nConflicted Files:");
        foreach (var file in status.ConflictedFiles)
        {
            Console.WriteLine($"  C {file.FilePath}");
        }
    }
}
```

### Get Repository Information

```csharp
var result = await repository.GetRepositoryInfoAsync();

if (result.Success)
{
    var info = result.Data;

    Console.WriteLine($"Repository: {info.Name}");
    Console.WriteLine($"Current Branch: {info.CurrentBranch}");
    Console.WriteLine($"HEAD: {info.HeadCommitSha}");
    Console.WriteLine($"State: {info.State}");
    Console.WriteLine($"Is Bare: {info.IsBare}");
    Console.WriteLine($"Local Branches: {info.LocalBranchCount}");
    Console.WriteLine($"Remote Branches: {info.RemoteBranchCount}");
    Console.WriteLine($"Tags: {info.TagCount}");
    Console.WriteLine($"Stashes: {info.StashCount}");

    if (info.IsDirty)
    {
        Console.WriteLine($"\nUncommitted Changes:");
        Console.WriteLine($"  Modified: {info.ModifiedFiles}");
        Console.WriteLine($"  Staged: {info.StagedFiles}");
        Console.WriteLine($"  Untracked: {info.UntrackedFiles}");
    }
}
```

## Batch Operations

### Fetch All Repositories

```csharp
var gitManager = gitHelper.GetGitManager();

var results = await gitManager.FetchAllAsync(maxConcurrency: 5);

Console.WriteLine("Fetch Results:");
foreach (var (repoId, result) in results)
{
    if (result.Success)
    {
        Console.WriteLine($"✓ {repoId}: Success ({result.Diagnostics?.Duration})");
    }
    else
    {
        Console.WriteLine($"✗ {repoId}: {result.ErrorMessage}");
    }
}
```

### Get Status for All Repositories

```csharp
var gitManager = gitHelper.GetGitManager();

var results = await gitManager.GetAllStatusAsync();

Console.WriteLine("Repository Status:");
foreach (var (repoId, result) in results)
{
    if (result.Success)
    {
        var status = result.Data;
        var marker = status.IsDirty ? "⚠" : "✓";

        Console.WriteLine($"{marker} {repoId}: {status.TotalChangedFiles} changes");

        if (status.IsDirty)
        {
            Console.WriteLine($"   M:{status.ModifiedFiles.Count} " +
                            $"S:{status.StagedFiles.Count} " +
                            $"U:{status.UntrackedFiles.Count}");
        }
    }
}
```

### Custom Batch Operation

```csharp
var gitManager = gitHelper.GetGitManager();

var results = await gitManager.ExecuteOnAllAsync(
    async (repo, repoId) =>
    {
        // Custom operation: Get latest commit info
        var info = await repo.GetRepositoryInfoAsync();

        return new OperationResult
        {
            Success = info.Success,
            ErrorMessage = info.ErrorMessage
        };
    },
    maxConcurrency: 3
);

foreach (var (repoId, result) in results)
{
    Console.WriteLine($"{repoId}: {(result.Success ? "OK" : "Failed")}");
}
```

### Health Check All Repositories

```csharp
var gitManager = gitHelper.GetGitManager();

var results = await gitManager.HealthCheckAsync();

Console.WriteLine("Repository Health:");
foreach (var (repoId, isHealthy) in results)
{
    var status = isHealthy ? "✓ Healthy" : "✗ Unhealthy";
    Console.WriteLine($"{repoId}: {status}");
}

var healthyCount = results.Values.Count(h => h);
Console.WriteLine($"\nTotal: {results.Count}, Healthy: {healthyCount}");
```

## Advanced Scenarios

### Clone, Commit, and Push Workflow

```csharp
var repository = await gitHelper.GetRepositoryAsync("MyProject");

// 1. Clone repository
Console.WriteLine("Cloning repository...");
var cloneResult = await repository.CloneAsync(new CloneOptions
{
    OnProgress = (p) => Console.Write($"\rCloning: {p.ProgressPercentage}%")
});

if (!cloneResult.Success)
{
    Console.WriteLine($"\nClone failed: {cloneResult.ErrorMessage}");
    return;
}

Console.WriteLine("\nClone completed!");

// 2. Make some changes to files (simulated)
// ... modify files in the working directory ...

// 3. Commit changes
Console.WriteLine("Committing changes...");
var commitResult = await repository.CommitAsync(new CommitOptions
{
    Message = "Add new feature implementation",
    FilesToStage = new List<string> { "src/NewFeature.cs" }
});

if (!commitResult.Success)
{
    Console.WriteLine($"Commit failed: {commitResult.ErrorMessage}");
    return;
}

Console.WriteLine($"Committed: {commitResult.Data?.ShortSha}");

// 4. Push to remote
Console.WriteLine("Pushing changes...");
var pushResult = await repository.PushAsync(new PushOptions
{
    OnProgress = (p) => Console.Write($"\rPushing: {p.ProgressPercentage}%")
});

if (!pushResult.Success)
{
    Console.WriteLine($"\nPush failed: {pushResult.ErrorMessage}");
    return;
}

Console.WriteLine("\nPush completed!");
```

### Sync Multiple Repositories

```csharp
var gitManager = gitHelper.GetGitManager();
var repoIds = gitManager.GetRepositoryIds();

Console.WriteLine($"Syncing {repoIds.Count} repositories...");

foreach (var repoId in repoIds)
{
    Console.WriteLine($"\nProcessing: {repoId}");

    var repo = await gitManager.GetRepositoryAsync(repoId);

    // Fetch latest changes
    var fetchResult = await repo.FetchAsync();
    if (!fetchResult.Success)
    {
        Console.WriteLine($"  Fetch failed: {fetchResult.ErrorMessage}");
        continue;
    }

    // Check status
    var statusResult = await repo.GetStatusAsync();
    if (statusResult.Success && statusResult.Data.IsDirty)
    {
        Console.WriteLine($"  Warning: Repository has uncommitted changes");
        continue;
    }

    // Pull changes
    var pullResult = await repo.PullAsync();
    if (pullResult.Success)
    {
        Console.WriteLine($"  Pull completed: {pullResult.Data?.Status}");
    }
    else
    {
        Console.WriteLine($"  Pull failed: {pullResult.ErrorMessage}");
    }
}

Console.WriteLine("\nSync completed!");
```

### Monitor Repository Changes

```csharp
var repository = await gitHelper.GetRepositoryAsync("MyProject");

while (true)
{
    var statusResult = await repository.GetStatusAsync();

    if (statusResult.Success && statusResult.Data.IsDirty)
    {
        Console.WriteLine($"Changes detected: {statusResult.Data.TotalChangedFiles} file(s)");

        foreach (var file in statusResult.Data.ModifiedFiles)
        {
            Console.WriteLine($"  Modified: {file.FilePath}");
        }
    }

    await Task.Delay(TimeSpan.FromSeconds(10));
}
```

### Repository Cache Management

```csharp
var gitManager = gitHelper.GetGitManager();

// Get cache statistics
var stats = gitManager.GetCacheStatistics();

Console.WriteLine($"Cache Status:");
Console.WriteLine($"  Enabled: {stats.CacheEnabled}");
Console.WriteLine($"  Timeout: {stats.CacheTimeoutMinutes} minutes");
Console.WriteLine($"  Cached Repositories: {stats.TotalCachedRepositories}");

foreach (var repoInfo in stats.Repositories)
{
    Console.WriteLine($"\n  Repository:");
    Console.WriteLine($"    Age: {repoInfo.Age.TotalMinutes:F1} minutes");
    Console.WriteLine($"    Last Access: {repoInfo.TimeSinceLastAccess.TotalMinutes:F1} minutes ago");
    Console.WriteLine($"    Expired: {repoInfo.IsExpired}");
}

// Clear cache if needed
if (stats.TotalCachedRepositories > 10)
{
    Console.WriteLine("\nClearing cache...");
    await gitManager.ClearCacheAsync();
    Console.WriteLine("Cache cleared!");
}
```

## Error Handling

### Comprehensive Error Handling

```csharp
try
{
    var repository = await gitHelper.GetRepositoryAsync("MyProject");
    var result = await repository.PullAsync();

    if (result.Success)
    {
        Console.WriteLine("Pull completed successfully");
    }
    else
    {
        // Operation failed
        Console.WriteLine($"Error: {result.ErrorMessage}");

        if (result.Exception != null)
        {
            Console.WriteLine($"Exception Type: {result.Exception.GetType().Name}");
            Console.WriteLine($"Exception Message: {result.Exception.Message}");

            if (result.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {result.Exception.InnerException.Message}");
            }
        }

        // Check diagnostics for more information
        if (result.Diagnostics != null)
        {
            Console.WriteLine("\nDiagnostic Messages:");
            foreach (var message in result.Diagnostics.Messages)
            {
                Console.WriteLine($"  {message}");
            }
        }
    }
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Retry Logic

```csharp
async Task<GitOperationResult<bool>> FetchWithRetry(
    GitRepository repository,
    int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        var result = await repository.FetchAsync();

        if (result.Success)
        {
            return result;
        }

        Console.WriteLine($"Fetch attempt {i + 1} failed: {result.ErrorMessage}");

        if (i < maxRetries - 1)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, i)); // Exponential backoff
            Console.WriteLine($"Retrying in {delay.TotalSeconds} seconds...");
            await Task.Delay(delay);
        }
    }

    return GitOperationResult<bool>.Fail($"Failed after {maxRetries} attempts");
}

// Usage
var repository = await gitHelper.GetRepositoryAsync("MyProject");
var result = await FetchWithRetry(repository);

if (result.Success)
{
    Console.WriteLine("Fetch succeeded!");
}
else
{
    Console.WriteLine($"Fetch failed after all retries: {result.ErrorMessage}");
}
```

## Performance Tips

1. **Enable Repository Caching**: Reuse repository instances for better performance
2. **Use Batch Operations**: Process multiple repositories concurrently
3. **Disable Progress Reporting**: For better performance when not needed
4. **Configure Timeouts**: Set appropriate timeout values for your network
5. **Limit Concurrent Operations**: Set `MaxConcurrentOperations` based on system resources

```csharp
// Optimal configuration for performance
{
  "EnableRepositoryCaching": true,
  "CacheTimeoutMinutes": 30,
  "MaxConcurrentOperations": 5,
  "DefaultTimeoutSeconds": 600,
  "Repositories": [
    {
      "EnableProgressReporting": false, // Disable if not needed
      "EnableDiagnostics": false // Disable in production
    }
  ]
}
```
