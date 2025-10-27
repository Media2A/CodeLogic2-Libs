namespace CL.Mail.Models;

/// <summary>
/// Configuration for the Mail library
/// </summary>
public class MailConfiguration
{
    /// <summary>
    /// Gets or sets the SMTP configuration
    /// </summary>
    public SmtpConfiguration Smtp { get; set; } = new();

    /// <summary>
    /// Gets or sets the default sender email address
    /// </summary>
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the default sender display name
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Gets or sets the directory where mail templates are stored
    /// </summary>
    public string TemplateDirectory { get; set; } = "templates/mail/";

    /// <summary>
    /// Gets or sets whether to enable HTML by default
    /// </summary>
    public bool EnableHtmlByDefault { get; set; } = true;
}

/// <summary>
/// SMTP server configuration
/// </summary>
public class SmtpConfiguration
{
    /// <summary>
    /// Gets or sets the SMTP server hostname
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets the SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security mode (None, StartTls, SslTls)
    /// </summary>
    public SmtpSecurityMode SecurityMode { get; set; } = SmtpSecurityMode.StartTls;

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use connection pooling
    /// </summary>
    public bool UseConnectionPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of pooled connections
    /// </summary>
    public int MaxPooledConnections { get; set; } = 10;
}

/// <summary>
/// SMTP security modes
/// </summary>
public enum SmtpSecurityMode
{
    /// <summary>
    /// No security
    /// </summary>
    None,

    /// <summary>
    /// StartTLS encryption
    /// </summary>
    StartTls,

    /// <summary>
    /// SSL/TLS encryption
    /// </summary>
    SslTls
}
