using CL.Mail.Models;
using CodeLogic.Abstractions;

namespace CL.Mail.Services;

/// <summary>
/// A simple template engine that replaces {{variable}} and ${variable} placeholders
/// </summary>
public class SimpleTemplateEngine : IMailTemplateEngine
{
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the name of this template engine
    /// </summary>
    public string Name => "Simple";

    /// <summary>
    /// Initializes a new instance of the SimpleTemplateEngine
    /// </summary>
    public SimpleTemplateEngine(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Renders a mail template by replacing variables
    /// </summary>
    public async Task<MailResult<RenderedTemplate>> RenderAsync(
        MailTemplate template,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create a normalized variables dictionary (lowercase keys)
            var normalizedVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in variables)
            {
                normalizedVars[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }

            var subject = RenderText(template.Subject, normalizedVars);
            var textBody = template.TextBody != null ? RenderText(template.TextBody, normalizedVars) : null;
            var htmlBody = template.HtmlBody != null ? RenderText(template.HtmlBody, normalizedVars) : null;

            var rendered = new RenderedTemplate
            {
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            };

            return MailResult<RenderedTemplate>.Success(rendered);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Template rendering was cancelled");
            return MailResult<RenderedTemplate>.Failure(MailError.TemplateRenderingFailed, "Rendering cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error rendering template '{template.Id}'", ex);
            return MailResult<RenderedTemplate>.Failure(
                MailError.TemplateRenderingFailed,
                ex.Message);
        }
    }

    /// <summary>
    /// Replaces variables in text using {{var}} or ${var} syntax
    /// </summary>
    private static string RenderText(string text, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;

        // Replace {{variable}} format
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\{\{(\w+)\}\}",
            match =>
            {
                var key = match.Groups[1].Value;
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });

        // Replace ${variable} format
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\$\{(\w+)\}",
            match =>
            {
                var key = match.Groups[1].Value;
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });

        // Replace {variable} format (legacy)
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\{(\w+)\}",
            match =>
            {
                var key = match.Groups[1].Value;
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });

        return result;
    }
}
