using CL.Mail.Models;

namespace CL.Mail.Services;

/// <summary>
/// Fluent builder for constructing email messages
/// </summary>
public class MailBuilder
{
    private string? _from;
    private string? _fromName;
    private readonly List<string> _to = new();
    private readonly List<string> _cc = new();
    private readonly List<string> _bcc = new();
    private string? _subject;
    private string? _textBody;
    private string? _htmlBody;
    private readonly List<string> _attachments = new();
    private readonly Dictionary<string, string> _headers = new();
    private MailPriority _priority = MailPriority.Normal;

    /// <summary>
    /// Sets the sender email address
    /// </summary>
    public MailBuilder From(string email)
    {
        _from = email;
        return this;
    }

    /// <summary>
    /// Sets the sender email address and display name
    /// </summary>
    public MailBuilder From(string email, string displayName)
    {
        _from = email;
        _fromName = displayName;
        return this;
    }

    /// <summary>
    /// Adds a recipient
    /// </summary>
    public MailBuilder To(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
            _to.Add(email);
        return this;
    }

    /// <summary>
    /// Adds multiple recipients
    /// </summary>
    public MailBuilder To(params string[] emails)
    {
        foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            _to.Add(email);
        return this;
    }

    /// <summary>
    /// Adds a CC recipient
    /// </summary>
    public MailBuilder Cc(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
            _cc.Add(email);
        return this;
    }

    /// <summary>
    /// Adds multiple CC recipients
    /// </summary>
    public MailBuilder Cc(params string[] emails)
    {
        foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            _cc.Add(email);
        return this;
    }

    /// <summary>
    /// Adds a BCC recipient
    /// </summary>
    public MailBuilder Bcc(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
            _bcc.Add(email);
        return this;
    }

    /// <summary>
    /// Adds multiple BCC recipients
    /// </summary>
    public MailBuilder Bcc(params string[] emails)
    {
        foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            _bcc.Add(email);
        return this;
    }

    /// <summary>
    /// Sets the subject
    /// </summary>
    public MailBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    /// <summary>
    /// Sets the plain text body
    /// </summary>
    public MailBuilder TextBody(string body)
    {
        _textBody = body;
        return this;
    }

    /// <summary>
    /// Sets the HTML body
    /// </summary>
    public MailBuilder HtmlBody(string body)
    {
        _htmlBody = body;
        return this;
    }

    /// <summary>
    /// Sets both text and HTML bodies
    /// </summary>
    public MailBuilder Body(string textBody, string htmlBody)
    {
        _textBody = textBody;
        _htmlBody = htmlBody;
        return this;
    }

    /// <summary>
    /// Adds a file attachment
    /// </summary>
    public MailBuilder Attach(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
            _attachments.Add(filePath);
        return this;
    }

    /// <summary>
    /// Adds multiple file attachments
    /// </summary>
    public MailBuilder Attach(params string[] filePaths)
    {
        foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            _attachments.Add(path);
        return this;
    }

    /// <summary>
    /// Adds a custom header
    /// </summary>
    public MailBuilder Header(string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _headers[name] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets the priority
    /// </summary>
    public MailBuilder Priority(MailPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Builds the mail message
    /// </summary>
    public MailMessage Build()
    {
        if (string.IsNullOrWhiteSpace(_from))
            throw new InvalidOperationException("Sender email must be specified");

        if (_to.Count == 0)
            throw new InvalidOperationException("At least one recipient must be specified");

        if (string.IsNullOrWhiteSpace(_subject))
            throw new InvalidOperationException("Subject must be specified");

        if (string.IsNullOrWhiteSpace(_textBody) && string.IsNullOrWhiteSpace(_htmlBody))
            throw new InvalidOperationException("At least one body (text or HTML) must be specified");

        return new MailMessage
        {
            From = _from,
            FromName = _fromName,
            To = _to.AsReadOnly(),
            Cc = _cc.AsReadOnly(),
            Bcc = _bcc.AsReadOnly(),
            Subject = _subject,
            TextBody = _textBody,
            HtmlBody = _htmlBody,
            Attachments = _attachments.AsReadOnly(),
            Headers = _headers.AsReadOnly(),
            Priority = _priority
        };
    }
}
