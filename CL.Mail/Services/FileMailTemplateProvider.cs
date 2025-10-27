using CL.Mail.Models;
using CodeLogic.Abstractions;
using System.Text.Json;

namespace CL.Mail.Services;

/// <summary>
/// Mail template provider that loads templates from the file system
/// </summary>
public class FileMailTemplateProvider : IMailTemplateProvider
{
    private readonly string _templateDirectory;
    private readonly ILogger _logger;
    private readonly Dictionary<string, MailTemplate> _cache;

    /// <summary>
    /// Gets the provider name
    /// </summary>
    public string Name => "File";

    /// <summary>
    /// Initializes a new instance of the FileMailTemplateProvider
    /// </summary>
    public FileMailTemplateProvider(string templateDirectory, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(templateDirectory))
            throw new ArgumentNullException(nameof(templateDirectory));

        _templateDirectory = templateDirectory;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new Dictionary<string, MailTemplate>();

        // Create directory if it doesn't exist
        if (!Directory.Exists(_templateDirectory))
        {
            try
            {
                Directory.CreateDirectory(_templateDirectory);
                _logger.Info($"Created mail template directory: {_templateDirectory}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to create template directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Loads a mail template from a JSON file
    /// </summary>
    public async Task<MailResult<MailTemplate>> LoadTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check cache first
            if (_cache.TryGetValue(templateId, out var cached))
            {
                _logger.Debug($"Template '{templateId}' loaded from cache");
                return MailResult<MailTemplate>.Success(cached);
            }

            var templatePath = Path.Combine(_templateDirectory, $"{templateId}.json");

            if (!File.Exists(templatePath))
            {
                _logger.Warning($"Template file not found: {templatePath}");
                return MailResult<MailTemplate>.Failure(
                    MailError.TemplateNotFound,
                    $"Template '{templateId}' not found");
            }

            var json = await File.ReadAllTextAsync(templatePath, cancellationToken).ConfigureAwait(false);
            var template = JsonSerializer.Deserialize<MailTemplate>(json);

            if (template == null)
            {
                _logger.Warning($"Failed to deserialize template: {templatePath}");
                return MailResult<MailTemplate>.Failure(
                    MailError.TemplateRenderingFailed,
                    "Failed to parse template file");
            }

            // Cache the template
            _cache[templateId] = template;
            _logger.Debug($"Template '{templateId}' loaded from file");

            return MailResult<MailTemplate>.Success(template);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template loading was cancelled");
            return MailResult<MailTemplate>.Failure(MailError.TemplateNotFound, "Loading cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading template '{templateId}'", ex);
            return MailResult<MailTemplate>.Failure(
                MailError.TemplateNotFound,
                ex.Message);
        }
    }

    /// <summary>
    /// Saves a mail template to a JSON file
    /// </summary>
    public async Task<MailResult> SaveTemplateAsync(
        MailTemplate template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var templatePath = Path.Combine(_templateDirectory, $"{template.Id}.json");
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(templatePath, json, cancellationToken).ConfigureAwait(false);

            // Update cache
            _cache[template.Id] = template;
            _logger.Info($"Template '{template.Id}' saved to file");

            return MailResult.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template saving was cancelled");
            return MailResult.Failure(MailError.TemplateRenderingFailed, "Saving cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error saving template '{template.Id}'", ex);
            return MailResult.Failure(MailError.TemplateRenderingFailed, ex.Message);
        }
    }

    /// <summary>
    /// Deletes a mail template
    /// </summary>
    public async Task<MailResult> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var templatePath = Path.Combine(_templateDirectory, $"{templateId}.json");

            if (!File.Exists(templatePath))
            {
                return MailResult.Failure(
                    MailError.TemplateNotFound,
                    $"Template '{templateId}' not found");
            }

            File.Delete(templatePath);
            _cache.Remove(templateId);
            _logger.Info($"Template '{templateId}' deleted");

            return MailResult.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template deletion was cancelled");
            return MailResult.Failure(MailError.TemplateRenderingFailed, "Deletion cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting template '{templateId}'", ex);
            return MailResult.Failure(MailError.TemplateRenderingFailed, ex.Message);
        }
    }

    /// <summary>
    /// Lists all available templates
    /// </summary>
    public async Task<MailResult<IReadOnlyList<string>>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(_templateDirectory))
                return MailResult<IReadOnlyList<string>>.Success(Array.Empty<string>());

            var templates = Directory
                .GetFiles(_templateDirectory, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();

            _logger.Debug($"Found {templates.Count} templates");
            return MailResult<IReadOnlyList<string>>.Success(templates.AsReadOnly());
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template listing was cancelled");
            return MailResult<IReadOnlyList<string>>.Failure(MailError.Unknown, "Listing cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error("Error listing templates", ex);
            return MailResult<IReadOnlyList<string>>.Failure(MailError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Clears the template cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.Debug("Template cache cleared");
    }
}
