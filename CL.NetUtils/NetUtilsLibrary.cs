using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.NetUtils.Models;
using CL.NetUtils.Services;

namespace CL.NetUtils;

/// <summary>
/// Network utilities library implementation for CodeLogic framework
/// </summary>
public class NetUtilsLibrary : ILibrary
{
    private DnsblChecker? _dnsblChecker;
    private Ip2LocationService? _ipLocationService;
    private ILogger? _logger;
    private DnsblConfiguration? _dnsblConfig;
    private Ip2LocationConfiguration? _ipLocationConfig;
    private bool _initialized;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public ILibraryManifest Manifest { get; } = new NetUtilsManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configurations or use defaults
        _dnsblConfig = context.Configuration.TryGetValue("Dnsbl", out var dnsblObj) && dnsblObj is DnsblConfiguration dnsblCfg
            ? dnsblCfg
            : new DnsblConfiguration();

        _ipLocationConfig = context.Configuration.TryGetValue("Ip2Location", out var ipLocObj) && ipLocObj is Ip2LocationConfiguration ipLocCfg
            ? ipLocCfg
            : new Ip2LocationConfiguration();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the library after dependencies are loaded
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_logger == null || _dnsblConfig == null || _ipLocationConfig == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        // Initialize DNSBL checker
        _dnsblChecker = new DnsblChecker(_dnsblConfig, _logger);

        // Initialize IP location service
        _ipLocationService = new Ip2LocationService(_ipLocationConfig, _logger);

        try
        {
            await _ipLocationService.InitializeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Warning($"IP geolocation service initialization failed: {ex.Message}");
            // Don't fail initialization - DNSBL can still work
        }

        _initialized = true;
        _logger.Info($"{Manifest.Name} initialized successfully");
    }

    /// <summary>
    /// Called when the library is being unloaded
    /// </summary>
    public Task OnUnloadAsync()
    {
        _logger?.Info($"Unloading {Manifest.Name}");

        _dnsblChecker = null;
        _ipLocationService?.Dispose();
        _ipLocationService = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _dnsblChecker == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        try
        {
            // Just check if DNSBL checker is available
            // IP location service is optional and may not have DB
            if (_dnsblChecker != null)
            {
                return Task.FromResult(HealthCheckResult.Healthy($"{Manifest.Name} is operational"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(
                "DNSBL checker not available",
                new InvalidOperationException("DNSBL checker is not available")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed",
                ex));
        }
    }

    /// <summary>
    /// Gets the DNSBL checker service
    /// </summary>
    public DnsblChecker GetDnsblChecker()
    {
        if (_dnsblChecker == null)
            throw new InvalidOperationException("Library not initialized");

        return _dnsblChecker;
    }

    /// <summary>
    /// Gets the IP location service
    /// </summary>
    public Ip2LocationService GetIpLocationService()
    {
        if (_ipLocationService == null)
            throw new InvalidOperationException("IP location service not initialized");

        return _ipLocationService;
    }
}

/// <summary>
/// Manifest for the NetUtils library
/// </summary>
internal class NetUtilsManifest : ILibraryManifest
{
    public string Id => "cl.netutils";
    public string Name => "CL.NetUtils";
    public string Version => "2.0.0";
    public string Author => "Media2A.com";
    public string Description => "Network utilities library with DNS blacklist checking and IP geolocation";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "networking", "dnsbl", "geolocation", "ip", "security"
    };
}
