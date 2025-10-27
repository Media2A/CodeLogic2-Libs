using CodeLogic.Abstractions;
using CL.GitHelper.Models;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace CL.GitHelper.Services;

/// <summary>
/// Git repository service with basic operations (simplified for LibGit2Sharp 0.30.0 compatibility)
/// </summary>
public class GitRepository : IDisposable
{
    private readonly RepositoryConfiguration _config;
    private readonly ILogger? _logger;
    private Repository? _repository;
    private bool _disposed;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    /// <summary>
    /// Initializes a new Git repository service
    /// </summary>
    public GitRepository(RepositoryConfiguration config, ILogger? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;

        if (!_config.IsValid())
        {
            throw new ArgumentException("Invalid repository configuration", nameof(config));
        }
    }

    #region Clone Operations

    /// <summary>
    /// Clones a repository (simplified version)
    /// </summary>
    public async Task<GitOperationResult<RepositoryInfo>> CloneAsync(Models.CloneOptions? options = null)
    {
        var diagnostics = new OperationDiagnostics();
        diagnostics.AddMessage("Starting clone operation");

        try
        {
            options ??= new Models.CloneOptions();

            _logger?.LogInfo("GitRepository", $"Cloning repository: {_config.RepositoryUrl}");

            // Ensure directory doesn't exist or is empty
            if (Directory.Exists(_config.LocalPath))
            {
                var files = Directory.GetFiles(_config.LocalPath);
                var dirs = Directory.GetDirectories(_config.LocalPath);

                if (files.Length > 0 || dirs.Length > 0)
                {
                    return GitOperationResult<RepositoryInfo>.Fail(
                        $"Directory '{_config.LocalPath}' already exists and is not empty",
                        null,
                        diagnostics);
                }
            }
            else
            {
                Directory.CreateDirectory(_config.LocalPath);
                diagnostics.AddMessage($"Created directory: {_config.LocalPath}");
            }

            // Perform simple clone
            var clonePath = await Task.Run(() =>
                Repository.Clone(_config.RepositoryUrl, _config.LocalPath),
                options.CancellationToken);

            diagnostics.AddMessage($"Repository cloned to: {clonePath}");
            diagnostics.Complete();

            // Open repository to get info
            _repository = new Repository(_config.LocalPath);
            var repoInfo = GetRepositoryInfo();

            _logger?.LogSuccess("GitRepository",
                $"Clone completed in {diagnostics.Duration?.TotalSeconds:F2}s");

            return GitOperationResult<RepositoryInfo>.Ok(repoInfo, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            _logger?.LogError("GitRepository", $"Clone failed: {ex.Message}", ex);
            return GitOperationResult<RepositoryInfo>.Fail($"Clone failed: {ex.Message}", ex, diagnostics);
        }
    }

    #endregion

    #region Fetch/Pull Operations

    /// <summary>
    /// Fetches changes from remote
    /// </summary>
    public async Task<GitOperationResult<bool>> FetchAsync(Models.FetchOptions? options = null)
    {
        var diagnostics = new OperationDiagnostics();

        try
        {
            await EnsureRepositoryAsync();
            await _operationLock.WaitAsync();

            try
            {
                options ??= new Models.FetchOptions();
                diagnostics.AddMessage($"Starting fetch from remote: {options.RemoteName}");

                _logger?.LogInfo("GitRepository", $"Fetching from remote: {options.RemoteName}");

                await EnsureRepositoryAsync();

                var remote = _repository!.Network.Remotes[options.RemoteName];
                if (remote == null)
                {
                    return GitOperationResult<bool>.Fail($"Remote '{options.RemoteName}' not found", null, diagnostics);
                }

                var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

                await Task.Run(() =>
                    Commands.Fetch(_repository, options.RemoteName, refSpecs, null, null),
                    options.CancellationToken);

                diagnostics.Complete();

                _logger?.LogSuccess("GitRepository",
                    $"Fetch completed in {diagnostics.Duration?.TotalSeconds:F2}s");

                return GitOperationResult<bool>.Ok(true, diagnostics);
            }
            finally
            {
                _operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            _logger?.LogError("GitRepository", $"Fetch failed: {ex.Message}", ex);
            return GitOperationResult<bool>.Fail($"Fetch failed: {ex.Message}", ex, diagnostics);
        }
    }

    /// <summary>
    /// Pulls changes from remote
    /// </summary>
    public async Task<GitOperationResult<MergeResult>> PullAsync(Models.PullOptions? options = null)
    {
        var diagnostics = new OperationDiagnostics();

        try
        {
            await EnsureRepositoryAsync();
            await _operationLock.WaitAsync();

            try
            {
                options ??= new Models.PullOptions();
                diagnostics.AddMessage($"Starting pull from remote: {options.RemoteName}");

                _logger?.LogInfo("GitRepository", $"Pulling from remote: {options.RemoteName}");

                // First fetch
                var fetchResult = await FetchAsync(options.FetchOptions);
                if (!fetchResult.Success)
                {
                    return GitOperationResult<MergeResult>.Fail(fetchResult.ErrorMessage, fetchResult.Exception, diagnostics);
                }

                // Get signature
                var signature = GetSignature();

                // Perform merge
                var mergeResult = await Task.Run(() =>
                    Commands.Pull(_repository, signature, null),
                    options.CancellationToken);

                diagnostics.Complete();

                _logger?.LogSuccess("GitRepository",
                    $"Pull completed: {mergeResult.Status} in {diagnostics.Duration?.TotalSeconds:F2}s");

                return GitOperationResult<MergeResult>.Ok(mergeResult, diagnostics);
            }
            finally
            {
                _operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            _logger?.LogError("GitRepository", $"Pull failed: {ex.Message}", ex);
            return GitOperationResult<MergeResult>.Fail($"Pull failed: {ex.Message}", ex, diagnostics);
        }
    }

    #endregion

    #region Push Operations

    /// <summary>
    /// Pushes changes to remote
    /// </summary>
    public async Task<GitOperationResult<bool>> PushAsync(Models.PushOptions? options = null)
    {
        var diagnostics = new OperationDiagnostics();

        try
        {
            await EnsureRepositoryAsync();
            await _operationLock.WaitAsync();

            try
            {
                options ??= new Models.PushOptions();
                var branchName = options.BranchName ?? _repository!.Head.FriendlyName;

                diagnostics.AddMessage($"Starting push to remote: {options.RemoteName}, branch: {branchName}");
                _logger?.LogInfo("GitRepository", $"Pushing to remote: {options.RemoteName}");

                var remote = _repository.Network.Remotes[options.RemoteName];
                if (remote == null)
                {
                    return GitOperationResult<bool>.Fail($"Remote '{options.RemoteName}' not found", null, diagnostics);
                }

                var pushRefSpec = $"refs/heads/{branchName}:refs/heads/{branchName}";

                await Task.Run(() =>
                {
                    _repository.Network.Push(remote, pushRefSpec, (LibGit2Sharp.PushOptions?)null);
                }, options.CancellationToken);

                diagnostics.Complete();

                _logger?.LogSuccess("GitRepository",
                    $"Push completed in {diagnostics.Duration?.TotalSeconds:F2}s");

                return GitOperationResult<bool>.Ok(true, diagnostics);
            }
            finally
            {
                _operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            _logger?.LogError("GitRepository", $"Push failed: {ex.Message}", ex);
            return GitOperationResult<bool>.Fail($"Push failed: {ex.Message}", ex, diagnostics);
        }
    }

    #endregion

    #region Commit Operations

    /// <summary>
    /// Commits changes
    /// </summary>
    public async Task<GitOperationResult<CommitInfo>> CommitAsync(Models.CommitOptions options)
    {
        var diagnostics = new OperationDiagnostics();

        try
        {
            await EnsureRepositoryAsync();
            await _operationLock.WaitAsync();

            try
            {
                diagnostics.AddMessage($"Starting commit: {options.Message}");

                // Stage files if specified
                if (options.FilesToStage != null && options.FilesToStage.Count > 0)
                {
                    foreach (var file in options.FilesToStage)
                    {
                        Commands.Stage(_repository!, file);
                        diagnostics.AddMessage($"Staged file: {file}");
                    }
                }

                var signature = GetSignature(options.AuthorName, options.AuthorEmail);

                var commit = await Task.Run(() =>
                    _repository!.Commit(options.Message, signature, signature));

                var commitInfo = ConvertToCommitInfo(commit);
                diagnostics.CommitsProcessed = 1;
                diagnostics.AddMessage($"Created commit: {commitInfo.ShortSha}");
                diagnostics.Complete();

                _logger?.LogSuccess("GitRepository", $"Commit created: {commitInfo.ShortSha} - {commitInfo.ShortMessage}");

                return GitOperationResult<CommitInfo>.Ok(commitInfo, diagnostics);
            }
            finally
            {
                _operationLock.Release();
            }
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            _logger?.LogError("GitRepository", $"Commit failed: {ex.Message}", ex);
            return GitOperationResult<CommitInfo>.Fail($"Commit failed: {ex.Message}", ex, diagnostics);
        }
    }

    #endregion

    #region Status and Info Operations

    /// <summary>
    /// Gets repository status
    /// </summary>
    public async Task<GitOperationResult<Models.RepositoryStatus>> GetStatusAsync()
    {
        var diagnostics = new OperationDiagnostics();

        try
        {
            await EnsureRepositoryAsync();

            var status = await Task.Run(() => _repository!.RetrieveStatus());

            var repoStatus = new Models.RepositoryStatus
            {
                IsDirty = status.IsDirty
            };

            foreach (var item in status)
            {
                var fileStatus = new Models.FileStatus
                {
                    FilePath = item.FilePath,
                    Status = item.State.ToString(),
                    IsStaged = item.State.HasFlag(LibGit2Sharp.FileStatus.ModifiedInIndex) ||
                              item.State.HasFlag(LibGit2Sharp.FileStatus.NewInIndex)
                };

                if (item.State.HasFlag(LibGit2Sharp.FileStatus.ModifiedInWorkdir) ||
                    item.State.HasFlag(LibGit2Sharp.FileStatus.ModifiedInIndex))
                {
                    repoStatus.ModifiedFiles.Add(fileStatus);
                }
                else if (item.State.HasFlag(LibGit2Sharp.FileStatus.NewInIndex) ||
                         item.State.HasFlag(LibGit2Sharp.FileStatus.NewInWorkdir))
                {
                    if (item.State.HasFlag(LibGit2Sharp.FileStatus.NewInIndex))
                        repoStatus.StagedFiles.Add(fileStatus);
                    else
                        repoStatus.UntrackedFiles.Add(fileStatus);
                }
                else if (item.State.HasFlag(LibGit2Sharp.FileStatus.Ignored))
                {
                    repoStatus.IgnoredFiles.Add(fileStatus);
                }
                else if (item.State.HasFlag(LibGit2Sharp.FileStatus.Conflicted))
                {
                    repoStatus.ConflictedFiles.Add(fileStatus);
                }
            }

            diagnostics.FilesChanged = repoStatus.TotalChangedFiles;
            diagnostics.Complete();

            return GitOperationResult<Models.RepositoryStatus>.Ok(repoStatus, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Complete();
            return GitOperationResult<Models.RepositoryStatus>.Fail($"Failed to get status: {ex.Message}", ex, diagnostics);
        }
    }

    /// <summary>
    /// Gets repository information
    /// </summary>
    public RepositoryInfo GetRepositoryInfo()
    {
        if (_repository == null)
        {
            throw new InvalidOperationException("Repository not initialized");
        }

        var status = _repository.RetrieveStatus();

        return new RepositoryInfo
        {
            Id = _config.Id,
            Name = _config.Name,
            LocalPath = _config.LocalPath,
            RemoteUrl = _repository.Network.Remotes.FirstOrDefault()?.Url ?? "",
            CurrentBranch = _repository.Head.FriendlyName,
            HeadCommitSha = _repository.Head.Tip?.Sha ?? "",
            IsBare = _repository.Info.IsBare,
            State = _repository.Info.CurrentOperation.ToString(),
            LocalBranchCount = _repository.Branches.Count(b => !b.IsRemote),
            RemoteBranchCount = _repository.Branches.Count(b => b.IsRemote),
            TagCount = _repository.Tags.Count(),
            StashCount = _repository.Stashes.Count(),
            IsDirty = status.IsDirty,
            ModifiedFiles = status.Modified.Count(),
            StagedFiles = status.Staged.Count(),
            UntrackedFiles = status.Untracked.Count()
        };
    }

    /// <summary>
    /// Gets repository information asynchronously
    /// </summary>
    public async Task<GitOperationResult<RepositoryInfo>> GetRepositoryInfoAsync()
    {
        try
        {
            await EnsureRepositoryAsync();
            var info = GetRepositoryInfo();
            return GitOperationResult<RepositoryInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return GitOperationResult<RepositoryInfo>.Fail($"Failed to get repository info: {ex.Message}", ex);
        }
    }

    #endregion

    #region Branch Operations

    /// <summary>
    /// Lists all branches
    /// </summary>
    public async Task<GitOperationResult<List<BranchInfo>>> ListBranchesAsync(bool includeRemote = true)
    {
        try
        {
            await EnsureRepositoryAsync();

            var branches = await Task.Run(() =>
            {
                var branchList = new List<BranchInfo>();

                foreach (var branch in _repository!.Branches)
                {
                    if (!includeRemote && branch.IsRemote)
                        continue;

                    branchList.Add(new BranchInfo
                    {
                        Name = branch.CanonicalName,
                        FriendlyName = branch.FriendlyName,
                        IsCurrent = branch.IsCurrentRepositoryHead,
                        IsRemote = branch.IsRemote,
                        IsTracking = branch.IsTracking,
                        TrackedBranchName = branch.TrackedBranch?.FriendlyName,
                        TipSha = branch.Tip?.Sha ?? "",
                        AheadBy = branch.TrackingDetails.AheadBy,
                        BehindBy = branch.TrackingDetails.BehindBy
                    });
                }

                return branchList;
            });

            return GitOperationResult<List<BranchInfo>>.Ok(branches);
        }
        catch (Exception ex)
        {
            return GitOperationResult<List<BranchInfo>>.Fail($"Failed to list branches: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks out a branch
    /// </summary>
    public async Task<GitOperationResult<bool>> CheckoutBranchAsync(string branchName)
    {
        try
        {
            await EnsureRepositoryAsync();

            await Task.Run(() =>
            {
                Commands.Checkout(_repository!, branchName);
            });

            _logger?.LogSuccess("GitRepository", $"Checked out branch: {branchName}");

            return GitOperationResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError("GitRepository", $"Checkout failed: {ex.Message}", ex);
            return GitOperationResult<bool>.Fail($"Checkout failed: {ex.Message}", ex);
        }
    }

    #endregion

    #region Helper Methods

    private async Task EnsureRepositoryAsync()
    {
        if (_repository != null)
            return;

        if (!Directory.Exists(_config.LocalPath) || !Directory.Exists(Path.Combine(_config.LocalPath, ".git")))
        {
            throw new InvalidOperationException($"Repository not found at '{_config.LocalPath}'. Clone it first.");
        }

        await Task.Run(() => _repository = new Repository(_config.LocalPath));
    }

    private Signature GetSignature(string? name = null, string? email = null)
    {
        name ??= _repository?.Config.Get<string>("user.name")?.Value ?? "CodeLogic";
        email ??= _repository?.Config.Get<string>("user.email")?.Value ?? "codelogic@localhost";

        return new Signature(name, email, DateTimeOffset.Now);
    }

    private CommitInfo ConvertToCommitInfo(Commit commit)
    {
        return new CommitInfo
        {
            Sha = commit.Sha,
            Message = commit.Message,
            AuthorName = commit.Author.Name,
            AuthorEmail = commit.Author.Email,
            AuthorDate = commit.Author.When.DateTime,
            CommitterName = commit.Committer.Name,
            CommitterEmail = commit.Committer.Email,
            CommitDate = commit.Committer.When.DateTime,
            ParentShas = commit.Parents.Select(p => p.Sha).ToList()
        };
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (_disposed)
            return;

        _repository?.Dispose();
        _operationLock?.Dispose();

        _disposed = true;
    }

    #endregion
}
