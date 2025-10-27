namespace CL.GitHelper.Models;

/// <summary>
/// Result of a Git operation with diagnostics
/// </summary>
public class GitOperationResult<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The result data (if successful)
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception details (if available)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Diagnostic information about the operation
    /// </summary>
    public OperationDiagnostics Diagnostics { get; set; } = new();

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static GitOperationResult<T> Ok(T data, OperationDiagnostics? diagnostics = null)
    {
        return new GitOperationResult<T>
        {
            Success = true,
            Data = data,
            Diagnostics = diagnostics ?? new OperationDiagnostics()
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static GitOperationResult<T> Fail(string error, Exception? exception = null, OperationDiagnostics? diagnostics = null)
    {
        return new GitOperationResult<T>
        {
            Success = false,
            ErrorMessage = error,
            Exception = exception,
            Diagnostics = diagnostics ?? new OperationDiagnostics()
        };
    }
}

/// <summary>
/// Diagnostic information for Git operations
/// </summary>
public class OperationDiagnostics
{
    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

    /// <summary>
    /// Number of objects received/sent
    /// </summary>
    public int ObjectCount { get; set; }

    /// <summary>
    /// Number of bytes transferred
    /// </summary>
    public long BytesTransferred { get; set; }

    /// <summary>
    /// Number of files changed
    /// </summary>
    public int FilesChanged { get; set; }

    /// <summary>
    /// Number of commits processed
    /// </summary>
    public int CommitsProcessed { get; set; }

    /// <summary>
    /// Additional diagnostic messages
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Marks operation as completed
    /// </summary>
    public void Complete()
    {
        EndTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a diagnostic message
    /// </summary>
    public void AddMessage(string message)
    {
        Messages.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    }

    /// <summary>
    /// Adds a metric
    /// </summary>
    public void AddMetric(string name, object value)
    {
        Metrics[name] = value;
    }
}

/// <summary>
/// Progress information for long-running Git operations
/// </summary>
public class GitProgressInfo
{
    /// <summary>
    /// Current operation stage
    /// </summary>
    public string Stage { get; set; } = "";

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Current step number
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Detailed message
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Number of objects received
    /// </summary>
    public int ObjectsReceived { get; set; }

    /// <summary>
    /// Total objects to receive
    /// </summary>
    public int TotalObjects { get; set; }

    /// <summary>
    /// Bytes received
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Timestamp of this progress update
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Information about a Git repository
/// </summary>
public class RepositoryInfo
{
    /// <summary>
    /// Repository configuration ID
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Repository name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Local path
    /// </summary>
    public string LocalPath { get; set; } = "";

    /// <summary>
    /// Remote URL
    /// </summary>
    public string RemoteUrl { get; set; } = "";

    /// <summary>
    /// Current branch name
    /// </summary>
    public string CurrentBranch { get; set; } = "";

    /// <summary>
    /// HEAD commit SHA
    /// </summary>
    public string HeadCommitSha { get; set; } = "";

    /// <summary>
    /// Is repository bare
    /// </summary>
    public bool IsBare { get; set; }

    /// <summary>
    /// Repository state (normal, merging, rebasing, etc.)
    /// </summary>
    public string State { get; set; } = "Normal";

    /// <summary>
    /// Number of local branches
    /// </summary>
    public int LocalBranchCount { get; set; }

    /// <summary>
    /// Number of remote branches
    /// </summary>
    public int RemoteBranchCount { get; set; }

    /// <summary>
    /// Number of tags
    /// </summary>
    public int TagCount { get; set; }

    /// <summary>
    /// Number of stashes
    /// </summary>
    public int StashCount { get; set; }

    /// <summary>
    /// Has uncommitted changes
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Number of modified files
    /// </summary>
    public int ModifiedFiles { get; set; }

    /// <summary>
    /// Number of staged files
    /// </summary>
    public int StagedFiles { get; set; }

    /// <summary>
    /// Number of untracked files
    /// </summary>
    public int UntrackedFiles { get; set; }

    /// <summary>
    /// Last fetch time
    /// </summary>
    public DateTime? LastFetchTime { get; set; }

    /// <summary>
    /// Last pull time
    /// </summary>
    public DateTime? LastPullTime { get; set; }
}

/// <summary>
/// Information about a Git commit
/// </summary>
public class CommitInfo
{
    /// <summary>
    /// Commit SHA
    /// </summary>
    public string Sha { get; set; } = "";

    /// <summary>
    /// Short SHA (first 7 characters)
    /// </summary>
    public string ShortSha => Sha.Length > 7 ? Sha.Substring(0, 7) : Sha;

    /// <summary>
    /// Commit message
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Short message (first line)
    /// </summary>
    public string ShortMessage => Message.Split('\n')[0];

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = "";

    /// <summary>
    /// Author email
    /// </summary>
    public string AuthorEmail { get; set; } = "";

    /// <summary>
    /// Author date
    /// </summary>
    public DateTime AuthorDate { get; set; }

    /// <summary>
    /// Committer name
    /// </summary>
    public string CommitterName { get; set; } = "";

    /// <summary>
    /// Committer email
    /// </summary>
    public string CommitterEmail { get; set; } = "";

    /// <summary>
    /// Commit date
    /// </summary>
    public DateTime CommitDate { get; set; }

    /// <summary>
    /// Parent commit SHAs
    /// </summary>
    public List<string> ParentShas { get; set; } = new();

    /// <summary>
    /// Number of files changed
    /// </summary>
    public int FilesChanged { get; set; }

    /// <summary>
    /// Lines added
    /// </summary>
    public int LinesAdded { get; set; }

    /// <summary>
    /// Lines deleted
    /// </summary>
    public int LinesDeleted { get; set; }
}

/// <summary>
/// Information about a Git branch
/// </summary>
public class BranchInfo
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Friendly name (without refs/heads/ or refs/remotes/)
    /// </summary>
    public string FriendlyName { get; set; } = "";

    /// <summary>
    /// Is current branch
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Is remote branch
    /// </summary>
    public bool IsRemote { get; set; }

    /// <summary>
    /// Is tracking a remote branch
    /// </summary>
    public bool IsTracking { get; set; }

    /// <summary>
    /// Tracked branch name
    /// </summary>
    public string? TrackedBranchName { get; set; }

    /// <summary>
    /// Tip commit SHA
    /// </summary>
    public string TipSha { get; set; } = "";

    /// <summary>
    /// Commits ahead of tracked branch
    /// </summary>
    public int? AheadBy { get; set; }

    /// <summary>
    /// Commits behind tracked branch
    /// </summary>
    public int? BehindBy { get; set; }
}

/// <summary>
/// Repository status information
/// </summary>
public class RepositoryStatus
{
    /// <summary>
    /// Is dirty (has uncommitted changes)
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Modified files
    /// </summary>
    public List<FileStatus> ModifiedFiles { get; set; } = new();

    /// <summary>
    /// Staged files
    /// </summary>
    public List<FileStatus> StagedFiles { get; set; } = new();

    /// <summary>
    /// Untracked files
    /// </summary>
    public List<FileStatus> UntrackedFiles { get; set; } = new();

    /// <summary>
    /// Ignored files
    /// </summary>
    public List<FileStatus> IgnoredFiles { get; set; } = new();

    /// <summary>
    /// Conflicted files
    /// </summary>
    public List<FileStatus> ConflictedFiles { get; set; } = new();

    /// <summary>
    /// Total count of changed files
    /// </summary>
    public int TotalChangedFiles => ModifiedFiles.Count + StagedFiles.Count + UntrackedFiles.Count;
}

/// <summary>
/// File status information
/// </summary>
public class FileStatus
{
    /// <summary>
    /// File path
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// Status (Added, Modified, Deleted, etc.)
    /// </summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// Is staged
    /// </summary>
    public bool IsStaged { get; set; }

    /// <summary>
    /// Old file path (for renames)
    /// </summary>
    public string? OldFilePath { get; set; }
}

/// <summary>
/// Clone operation options
/// </summary>
public class CloneOptions
{
    /// <summary>
    /// Branch to checkout after clone
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Perform shallow clone with depth
    /// </summary>
    public int? Depth { get; set; }

    /// <summary>
    /// Clone only a single branch
    /// </summary>
    public bool SingleBranch { get; set; }

    /// <summary>
    /// Checkout files after clone
    /// </summary>
    public bool Checkout { get; set; } = true;

    /// <summary>
    /// Create a bare repository
    /// </summary>
    public bool Bare { get; set; }

    /// <summary>
    /// Recursively clone submodules
    /// </summary>
    public bool RecurseSubmodules { get; set; }

    /// <summary>
    /// Progress callback
    /// </summary>
    public Action<GitProgressInfo>? OnProgress { get; set; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Fetch operation options
/// </summary>
public class FetchOptions
{
    /// <summary>
    /// Remote name
    /// </summary>
    public string RemoteName { get; set; } = "origin";

    /// <summary>
    /// Prune deleted remote branches
    /// </summary>
    public bool Prune { get; set; }

    /// <summary>
    /// Fetch tags
    /// </summary>
    public bool FetchTags { get; set; } = true;

    /// <summary>
    /// Progress callback
    /// </summary>
    public Action<GitProgressInfo>? OnProgress { get; set; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Pull operation options
/// </summary>
public class PullOptions
{
    /// <summary>
    /// Remote name
    /// </summary>
    public string RemoteName { get; set; } = "origin";

    /// <summary>
    /// Merge strategy (merge, rebase, fast-forward only)
    /// </summary>
    public MergeStrategy Strategy { get; set; } = MergeStrategy.Merge;

    /// <summary>
    /// Fetch options
    /// </summary>
    public FetchOptions FetchOptions { get; set; } = new();

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Merge strategy
/// </summary>
public enum MergeStrategy
{
    /// <summary>
    /// Standard merge
    /// </summary>
    Merge,

    /// <summary>
    /// Rebase on top of remote
    /// </summary>
    Rebase,

    /// <summary>
    /// Only fast-forward merge
    /// </summary>
    FastForwardOnly
}

/// <summary>
/// Commit operation options
/// </summary>
public class CommitOptions
{
    /// <summary>
    /// Commit message
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Author name (defaults to config)
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Author email (defaults to config)
    /// </summary>
    public string? AuthorEmail { get; set; }

    /// <summary>
    /// Allow empty commit
    /// </summary>
    public bool AllowEmpty { get; set; }

    /// <summary>
    /// Amend last commit
    /// </summary>
    public bool Amend { get; set; }

    /// <summary>
    /// Files to stage before commit (null = commit all staged)
    /// </summary>
    public List<string>? FilesToStage { get; set; }
}

/// <summary>
/// Push operation options
/// </summary>
public class PushOptions
{
    /// <summary>
    /// Remote name
    /// </summary>
    public string RemoteName { get; set; } = "origin";

    /// <summary>
    /// Branch to push (null = current branch)
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Force push
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// Set upstream tracking
    /// </summary>
    public bool SetUpstream { get; set; }

    /// <summary>
    /// Push tags
    /// </summary>
    public bool PushTags { get; set; }

    /// <summary>
    /// Progress callback
    /// </summary>
    public Action<GitProgressInfo>? OnProgress { get; set; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
