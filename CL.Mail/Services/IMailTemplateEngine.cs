using CL.Mail.Models;

namespace CL.Mail.Services;

/// <summary>
/// Defines a contract for rendering mail templates
/// </summary>
public interface IMailTemplateEngine
{
    /// <summary>
    /// Renders a mail template with the provided variables
    /// </summary>
    /// <param name="template">The template to render</param>
    /// <param name="variables">The variables to use for replacement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rendered template result</returns>
    Task<MailResult<RenderedTemplate>> RenderAsync(
        MailTemplate template,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of this template engine
    /// </summary>
    string Name { get; }
}
