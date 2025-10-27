using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.TwoFactorAuth.Models;
using CL.TwoFactorAuth.Services;

namespace CL.TwoFactorAuth;

/// <summary>
/// Two-factor authentication library implementation for CodeLogic framework
/// </summary>
public class TwoFactorAuthLibrary : ILibrary
{
    private TwoFactorAuthenticator? _authenticator;
    private QrCodeGenerator? _qrGenerator;
    private ILogger? _logger;
    private TwoFactorAuthConfiguration? _config;
    private bool _initialized;

    public ILibraryManifest Manifest { get; } = new TwoFactorAuthManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configuration or use defaults
        _config = context.Configuration.TryGetValue("TwoFactorAuth", out var configObj) && configObj is TwoFactorAuthConfiguration tfaConfig
            ? tfaConfig
            : new TwoFactorAuthConfiguration();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the library after dependencies are loaded
    /// </summary>
    public Task OnInitializeAsync()
    {
        if (_logger == null || _config == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        _authenticator = new TwoFactorAuthenticator(_config, _logger);
        _qrGenerator = new QrCodeGenerator(_config, _logger);
        _initialized = true;

        _logger.Info($"{Manifest.Name} initialized successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the library is being unloaded
    /// </summary>
    public Task OnUnloadAsync()
    {
        _logger?.Info($"Unloading {Manifest.Name}");

        _authenticator = null;
        _qrGenerator = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _authenticator == null || _qrGenerator == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        try
        {
            // Test generating a secret key
            var testKey = _authenticator.GenerateSecretKey();
            if (string.IsNullOrEmpty(testKey))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Failed to generate secret key",
                    new InvalidOperationException("Secret key generation failed")));
            }

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
    /// Gets the authenticator service (must be initialized first)
    /// </summary>
    public TwoFactorAuthenticator GetAuthenticator()
    {
        if (_authenticator == null)
            throw new InvalidOperationException("Library not initialized");

        return _authenticator;
    }

    /// <summary>
    /// Gets the QR code generator service (must be initialized first)
    /// </summary>
    public QrCodeGenerator GetQrCodeGenerator()
    {
        if (_qrGenerator == null)
            throw new InvalidOperationException("Library not initialized");

        return _qrGenerator;
    }
}

/// <summary>
/// Manifest for the TwoFactorAuth library
/// </summary>
internal class TwoFactorAuthManifest : ILibraryManifest
{
    public string Id => "cl.twofactorauth";
    public string Name => "CL.TwoFactorAuth";
    public string Version => "2.0.0";
    public string Author => "Media2A.com";
    public string Description => "Two-factor authentication library with TOTP and QR code generation";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "authentication", "2fa", "totp", "security", "qrcode"
    };
}
