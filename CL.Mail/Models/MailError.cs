namespace CL.Mail.Models;

/// <summary>
/// Error types for mail operations
/// </summary>
public enum MailError
{
    /// <summary>
    /// No error
    /// </summary>
    None,

    /// <summary>
    /// Invalid recipient address
    /// </summary>
    InvalidRecipient,

    /// <summary>
    /// Invalid sender address
    /// </summary>
    InvalidSender,

    /// <summary>
    /// Invalid subject
    /// </summary>
    InvalidSubject,

    /// <summary>
    /// Template not found
    /// </summary>
    TemplateNotFound,

    /// <summary>
    /// Template rendering failed
    /// </summary>
    TemplateRenderingFailed,

    /// <summary>
    /// SMTP configuration missing or invalid
    /// </summary>
    SmtpConfigInvalid,

    /// <summary>
    /// SMTP authentication failed
    /// </summary>
    SmtpAuthenticationFailed,

    /// <summary>
    /// SMTP connection timeout
    /// </summary>
    SmtpTimeout,

    /// <summary>
    /// SMTP server rejected the message
    /// </summary>
    SmtpRejected,

    /// <summary>
    /// Network error during send
    /// </summary>
    NetworkError,

    /// <summary>
    /// General SMTP error
    /// </summary>
    SmtpError,

    /// <summary>
    /// File attachment not found
    /// </summary>
    AttachmentNotFound,

    /// <summary>
    /// Invalid attachment
    /// </summary>
    InvalidAttachment,

    /// <summary>
    /// Unknown error occurred
    /// </summary>
    Unknown
}
