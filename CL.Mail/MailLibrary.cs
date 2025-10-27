using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.Mail.Models;
using CL.Mail.Services;

namespace CL.Mail;

/// <summary>
/// Modern email library implementation for CodeLogic framework
/// </summary>
public class MailLibrary : ILibrary
{
    private SmtpService? _smtpService;
    private IMailTemplateProvider? _templateProvider;
    private IMailTemplateEngine? _templateEngine;
    private ILogger? _logger;
    private MailConfiguration? _config;
    private bool _initialized;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public ILibraryManifest Manifest { get; } = new MailManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configuration or use defaults
        _config = context.Configuration.TryGetValue("Mail", out var mailObj) && mailObj is MailConfiguration mailCfg
            ? mailCfg
            : new MailConfiguration();

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

        // Initialize SMTP service
        _smtpService = new SmtpService(_config.Smtp, _logger);

        // Initialize template provider
        _templateProvider = new FileMailTemplateProvider(_config.TemplateDirectory, _logger);

        // Initialize template engine
        _templateEngine = new SimpleTemplateEngine(_logger);

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

        _smtpService = null;
        _templateProvider = null;
        _templateEngine = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _smtpService == null || _templateProvider == null || _templateEngine == null)
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
    /// Gets the SMTP service for sending emails
    /// </summary>
    public SmtpService GetSmtpService()
    {
        if (_smtpService == null)
            throw new InvalidOperationException("Library not initialized");

        return _smtpService;
    }

    /// <summary>
    /// Gets the template provider for loading templates
    /// </summary>
    public IMailTemplateProvider GetTemplateProvider()
    {
        if (_templateProvider == null)
            throw new InvalidOperationException("Library not initialized");

        return _templateProvider;
    }

    /// <summary>
    /// Gets the template engine for rendering templates
    /// </summary>
    public IMailTemplateEngine GetTemplateEngine()
    {
        if (_templateEngine == null)
            throw new InvalidOperationException("Library not initialized");

        return _templateEngine;
    }

    /// <summary>
    /// Creates a new mail builder
    /// </summary>
    public MailBuilder CreateMailBuilder() => new MailBuilder();

    /// <summary>
    /// Creates a new template builder
    /// </summary>
    public TemplateBuilder CreateTemplateBuilder() => new TemplateBuilder();
}

/// <summary>
/// Manifest for the Mail library
/// </summary>
internal class MailManifest : ILibraryManifest
{
    /// <summary>
    /// Gets the library ID
    /// </summary>
    public string Id => "cl.mail";

    /// <summary>
    /// Gets the library name
    /// </summary>
    public string Name => "CL.Mail";

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
    public string Description => "Modern email library with advanced template system";

    /// <summary>
    /// Gets library dependencies
    /// </summary>
    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    /// <summary>
    /// Gets library tags
    /// </summary>
    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "email", "smtp", "templates", "mail", "messaging"
    };
}
