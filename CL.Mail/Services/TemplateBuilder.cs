using CL.Mail.Models;

namespace CL.Mail.Services;

/// <summary>
/// Fluent builder for constructing email templates
/// </summary>
public class TemplateBuilder
{
    private string? _id;
    private string? _name;
    private string? _description;
    private string? _subject;
    private string? _textBody;
    private string? _htmlBody;
    private readonly List<string> _variables = new();
    private readonly Dictionary<string, object> _metadata = new();

    /// <summary>
    /// Sets the template ID
    /// </summary>
    public TemplateBuilder Id(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the template name
    /// </summary>
    public TemplateBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the template description
    /// </summary>
    public TemplateBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the subject template
    /// </summary>
    public TemplateBuilder Subject(string subject)
    {
        _subject = subject;
        // Extract variables from subject
        ExtractVariables(subject);
        return this;
    }

    /// <summary>
    /// Sets the plain text body template
    /// </summary>
    public TemplateBuilder TextBody(string body)
    {
        _textBody = body;
        ExtractVariables(body);
        return this;
    }

    /// <summary>
    /// Sets the HTML body template
    /// </summary>
    public TemplateBuilder HtmlBody(string body)
    {
        _htmlBody = body;
        ExtractVariables(body);
        return this;
    }

    /// <summary>
    /// Sets both text and HTML body templates
    /// </summary>
    public TemplateBuilder Body(string textBody, string htmlBody)
    {
        _textBody = textBody;
        _htmlBody = htmlBody;
        ExtractVariables(textBody);
        ExtractVariables(htmlBody);
        return this;
    }

    /// <summary>
    /// Adds a template variable
    /// </summary>
    public TemplateBuilder Variable(string name)
    {
        if (!string.IsNullOrWhiteSpace(name) && !_variables.Contains(name))
            _variables.Add(name);
        return this;
    }

    /// <summary>
    /// Adds multiple template variables
    /// </summary>
    public TemplateBuilder Variables(params string[] names)
    {
        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            if (!_variables.Contains(name))
                _variables.Add(name);
        }
        return this;
    }

    /// <summary>
    /// Adds template metadata
    /// </summary>
    public TemplateBuilder Metadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the mail template
    /// </summary>
    public MailTemplate Build()
    {
        if (string.IsNullOrWhiteSpace(_id))
            throw new InvalidOperationException("Template ID must be specified");

        if (string.IsNullOrWhiteSpace(_subject))
            throw new InvalidOperationException("Subject must be specified");

        if (string.IsNullOrWhiteSpace(_textBody) && string.IsNullOrWhiteSpace(_htmlBody))
            throw new InvalidOperationException("At least one body (text or HTML) must be specified");

        return new MailTemplate
        {
            Id = _id,
            Name = _name ?? _id,
            Description = _description,
            Subject = _subject,
            TextBody = _textBody,
            HtmlBody = _htmlBody,
            Variables = _variables.AsReadOnly(),
            Metadata = _metadata.AsReadOnly()
        };
    }

    /// <summary>
    /// Extracts variable names from template text
    /// </summary>
    private void ExtractVariables(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Extract {{variable}} format
        var matches1 = System.Text.RegularExpressions.Regex.Matches(text, @"\{\{(\w+)\}\}");
        foreach (System.Text.RegularExpressions.Match match in matches1)
        {
            var varName = match.Groups[1].Value;
            if (!_variables.Contains(varName))
                _variables.Add(varName);
        }

        // Extract ${variable} format
        var matches2 = System.Text.RegularExpressions.Regex.Matches(text, @"\$\{(\w+)\}");
        foreach (System.Text.RegularExpressions.Match match in matches2)
        {
            var varName = match.Groups[1].Value;
            if (!_variables.Contains(varName))
                _variables.Add(varName);
        }

        // Extract {variable} format (legacy)
        var matches3 = System.Text.RegularExpressions.Regex.Matches(text, @"\{(\w+)\}");
        foreach (System.Text.RegularExpressions.Match match in matches3)
        {
            var varName = match.Groups[1].Value;
            if (!_variables.Contains(varName))
                _variables.Add(varName);
        }
    }
}
