namespace CL.Core;

using CodeLogic.Abstractions;
using CodeLogic.Models;

/// <summary>
/// CL.Core library providing core utilities and helpers for CodeLogic framework
/// </summary>
public class CoreLibrary : ILibrary
{
    private ILogger? _logger;
    private bool _initialized;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public ILibraryManifest Manifest { get; } = new CoreManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the library after dependencies are loaded
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_logger == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

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

        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        try
        {
            // Core library is always operational
            return Task.FromResult(HealthCheckResult.Healthy($"{Manifest.Name} is operational"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed",
                ex));
        }
    }
}

/// <summary>
/// Library manifest for CL.Core
/// </summary>
internal class CoreManifest : ILibraryManifest
{
    /// <summary>
    /// Gets the library ID
    /// </summary>
    public string Id => "cl.core";

    /// <summary>
    /// Gets the library name
    /// </summary>
    public string Name => "CL.Core";

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
    public string Description => "Core utilities and helpers library for CodeLogic framework";

    /// <summary>
    /// Gets library dependencies
    /// </summary>
    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    /// <summary>
    /// Gets library tags
    /// </summary>
    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "core", "utilities", "helpers", "compression", "conversion", "encryption", "hashing", "validation"
    };
}
