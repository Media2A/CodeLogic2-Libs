using CL.Mail.Models;

namespace CL.Mail.Services;

/// <summary>
/// Defines a contract for loading mail templates from various sources
/// </summary>
public interface IMailTemplateProvider
{
    /// <summary>
    /// Loads a mail template by ID
    /// </summary>
    /// <param name="templateId">The unique template identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded template or null if not found</returns>
    Task<MailResult<MailTemplate>> LoadTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a mail template
    /// </summary>
    /// <param name="template">The template to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<MailResult> SaveTemplateAsync(MailTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a mail template
    /// </summary>
    /// <param name="templateId">The template ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<MailResult> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available template IDs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of template IDs</returns>
    Task<MailResult<IReadOnlyList<string>>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the provider name
    /// </summary>
    string Name { get; }
}
