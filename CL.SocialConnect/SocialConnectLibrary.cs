using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SocialConnect.Models;
using CL.SocialConnect.Services.Discord;
using CL.SocialConnect.Services.Steam;

namespace CL.SocialConnect;

/// <summary>
/// Social platform integration library for CodeLogic framework
/// </summary>
public class SocialConnectLibrary : ILibrary
{
    private DiscordWebhookService? _discordWebhookService;
    private SteamProfileService? _steamProfileService;
    private SteamAuthenticationService? _steamAuthService;
    private ILogger? _logger;
    private SocialConnectConfiguration? _config;
    private bool _initialized;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public ILibraryManifest Manifest { get; } = new SocialConnectManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configuration or use defaults
        _config = context.Configuration.TryGetValue("SocialConnect", out var scObj) && scObj is SocialConnectConfiguration scCfg
            ? scCfg
            : new SocialConnectConfiguration();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the library after dependencies are loaded
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_logger == null || _config == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        // Initialize Discord webhook service
        _discordWebhookService = new DiscordWebhookService(_logger);

        // Initialize Steam profile service
        if (!string.IsNullOrWhiteSpace(_config.Steam.ApiKey))
        {
            _steamProfileService = new SteamProfileService(
                _config.Steam.ApiKey,
                _config.Steam.ApiBaseUrl,
                _config.Steam.CacheDurationSeconds,
                _config.Steam.EnableCaching,
                _logger);
        }

        // Initialize Steam authentication service
        if (!string.IsNullOrWhiteSpace(_config.Steam.ReturnUrl))
        {
            _steamAuthService = new SteamAuthenticationService(
                _config.Steam.OpenIdBaseUrl,
                _config.Steam.ReturnUrl,
                _logger);
        }

        _initialized = true;
        _logger.Info($"{Manifest.Name} initialized successfully");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when the library is being unloaded
    /// </summary>
    public Task OnUnloadAsync()
    {
        _logger?.Info($"Unloading {Manifest.Name}");

        _discordWebhookService = null;
        _steamProfileService = null;
        _steamAuthService?.Dispose();
        _steamAuthService = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _discordWebhookService == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        try
        {
            // Services are instantiated and ready
            return Task.FromResult(HealthCheckResult.Healthy($"{Manifest.Name} is operational"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed",
                ex));
        }
    }

    /// <summary>
    /// Gets the Discord webhook service
    /// </summary>
    public DiscordWebhookService GetDiscordWebhookService()
    {
        if (_discordWebhookService == null)
            throw new InvalidOperationException("Library not initialized");

        return _discordWebhookService;
    }

    /// <summary>
    /// Gets the Steam profile service
    /// </summary>
    public SteamProfileService GetSteamProfileService()
    {
        if (_steamProfileService == null)
            throw new InvalidOperationException("Steam API key not configured");

        return _steamProfileService;
    }

    /// <summary>
    /// Gets the Steam authentication service
    /// </summary>
    public SteamAuthenticationService GetSteamAuthenticationService()
    {
        if (_steamAuthService == null)
            throw new InvalidOperationException("Steam return URL not configured");

        return _steamAuthService;
    }
}

/// <summary>
/// Manifest for the SocialConnect library
/// </summary>
internal class SocialConnectManifest : ILibraryManifest
{
    /// <summary>
    /// Gets the library ID
    /// </summary>
    public string Id => "cl.socialconnect";

    /// <summary>
    /// Gets the library name
    /// </summary>
    public string Name => "CL.SocialConnect";

    /// <summary>
    /// Gets the library version
    /// </summary>
    public string Version => "2.0.0";

    /// <summary>
    /// Gets the library author
    /// </summary>
    public string Author => "Media2A.com";

    /// <summary>
    /// Gets the library description
    /// </summary>
    public string Description => "Social platform integration with Discord and Steam support";

    /// <summary>
    /// Gets library dependencies
    /// </summary>
    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    /// <summary>
    /// Gets library tags
    /// </summary>
    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "social", "discord", "steam", "integration", "api"
    };
}
