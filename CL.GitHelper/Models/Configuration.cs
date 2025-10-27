namespace CL.GitHelper.Models;

/// <summary>
/// Configuration for a Git repository
/// </summary>
public class RepositoryConfiguration
{
    /// <summary>
    /// Unique identifier for this repository configuration
    /// </summary>
    public string Id { get; set; } = "Default";

    /// <summary>
    /// Display name for this repository
    /// </summary>
    public string Name { get; set; } = "Default Repository";

    /// <summary>
    /// Remote repository URL (HTTPS or SSH)
    /// </summary>
    public string RepositoryUrl { get; set; } = "";

    /// <summary>
    /// Local directory path for the repository
    /// </summary>
    public string LocalPath { get; set; } = "";

    /// <summary>
    /// Use application data directory as base path
    /// </summary>
    public bool UseAppDataDir { get; set; } = true;

    /// <summary>
    /// Default branch name
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Username for authentication (HTTPS)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password or Personal Access Token for authentication (HTTPS)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// SSH private key path (for SSH URLs)
    /// </summary>
    public string? SshKeyPath { get; set; }

    /// <summary>
    /// SSH key passphrase
    /// </summary>
    public string? SshPassphrase { get; set; }

    /// <summary>
    /// Auto-fetch on initialization
    /// </summary>
    public bool AutoFetch { get; set; } = false;

    /// <summary>
    /// Fetch interval in minutes (0 = disabled)
    /// </summary>
    public int AutoFetchIntervalMinutes { get; set; } = 0;

    /// <summary>
    /// Operation timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enable progress reporting
    /// </summary>
    public bool EnableProgressReporting { get; set; } = true;

    /// <summary>
    /// Enable detailed diagnostics
    /// </summary>
    public bool EnableDiagnostics { get; set; } = true;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(RepositoryUrl) &&
               !string.IsNullOrWhiteSpace(LocalPath);
    }

    /// <summary>
    /// Returns a default configuration template
    /// </summary>
    public static RepositoryConfiguration GetDefaultTemplate()
    {
        return new RepositoryConfiguration
        {
            Id = "Default",
            Name = "My Repository",
            RepositoryUrl = "https://github.com/username/repository.git",
            LocalPath = "repositories/my-repo",
            UseAppDataDir = true,
            DefaultBranch = "main",
            Username = "",
            Password = "",
            AutoFetch = false,
            TimeoutSeconds = 300,
            EnableProgressReporting = true,
            EnableDiagnostics = true
        };
    }
}

/// <summary>
/// Root configuration container for Git repositories
/// </summary>
public class GitHelperConfiguration
{
    /// <summary>
    /// List of configured repositories
    /// </summary>
    public List<RepositoryConfiguration> Repositories { get; set; } = new();

    /// <summary>
    /// Default operation timeout in seconds
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum concurrent operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 3;

    /// <summary>
    /// Enable global diagnostics
    /// </summary>
    public bool EnableGlobalDiagnostics { get; set; } = true;

    /// <summary>
    /// Cache repository instances
    /// </summary>
    public bool EnableRepositoryCaching { get; set; } = true;

    /// <summary>
    /// Cache timeout in minutes
    /// </summary>
    public int CacheTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets a repository configuration by ID
    /// </summary>
    public RepositoryConfiguration? GetRepository(string id)
    {
        return Repositories.FirstOrDefault(r =>
            r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a default configuration template
    /// </summary>
    public static GitHelperConfiguration GetDefaultTemplate()
    {
        return new GitHelperConfiguration
        {
            Repositories = new List<RepositoryConfiguration>
            {
                RepositoryConfiguration.GetDefaultTemplate()
            },
            DefaultTimeoutSeconds = 300,
            MaxConcurrentOperations = 3,
            EnableGlobalDiagnostics = true,
            EnableRepositoryCaching = true,
            CacheTimeoutMinutes = 30
        };
    }
}
