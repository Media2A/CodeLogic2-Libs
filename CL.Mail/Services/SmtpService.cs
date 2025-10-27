using CL.Mail.Models;
using CodeLogic.Abstractions;
using System.Net;
using System.Net.Mail;
using System.Security.Authentication;

namespace CL.Mail.Services;

/// <summary>
/// SMTP email service for sending emails
/// </summary>
public class SmtpService
{
    private readonly SmtpConfiguration _config;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the SmtpService
    /// </summary>
    public SmtpService(SmtpConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends an email message
    /// </summary>
    public async Task<MailResult> SendAsync(Models.MailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate configuration
            var configValidation = ValidateConfiguration();
            if (!configValidation.IsSuccess)
                return configValidation;

            // Validate message
            var messageValidation = ValidateMessage(message);
            if (!messageValidation.IsSuccess)
                return messageValidation;

            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(message);

            try
            {
                await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
                _logger.Info($"Email sent to {string.Join(", ", message.To)}");
                return MailResult.Success();
            }
            catch (SmtpException ex) when (ex.InnerException is AuthenticationException)
            {
                _logger.Error("SMTP authentication failed", ex);
                return MailResult.Failure(MailError.SmtpAuthenticationFailed, "SMTP authentication failed");
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
            {
                _logger.Error("SMTP TLS required but not available", ex);
                return MailResult.Failure(MailError.SmtpError, "SMTP TLS required");
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.TransactionFailed)
            {
                _logger.Error("SMTP transaction failed", ex);
                return MailResult.Failure(MailError.SmtpRejected, "Message rejected by server");
            }
            catch (SmtpException ex)
            {
                _logger.Error("SMTP error", ex);
                return MailResult.Failure(MailError.SmtpError, ex.Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Email send operation cancelled");
            return MailResult.Failure(MailError.Unknown, "Operation cancelled");
        }
        catch (TimeoutException)
        {
            _logger.Error("Email send operation timed out");
            return MailResult.Failure(MailError.SmtpTimeout, "SMTP timeout");
        }
        catch (Exception ex)
        {
            _logger.Error("Unexpected error sending email", ex);
            return MailResult.Failure(MailError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Validates the SMTP configuration
    /// </summary>
    private MailResult ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Host))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP host not configured");

        if (_config.Port <= 0 || _config.Port > 65535)
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP port is invalid");

        if (string.IsNullOrWhiteSpace(_config.Username))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP username not configured");

        if (string.IsNullOrWhiteSpace(_config.Password))
            return MailResult.Failure(MailError.SmtpConfigInvalid, "SMTP password not configured");

        return MailResult.Success();
    }

    /// <summary>
    /// Validates a mail message
    /// </summary>
    private MailResult ValidateMessage(Models.MailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.From))
            return MailResult.Failure(MailError.InvalidSender, "Sender email not specified");

        if (message.To == null || message.To.Count == 0)
            return MailResult.Failure(MailError.InvalidRecipient, "No recipients specified");

        if (string.IsNullOrWhiteSpace(message.Subject))
            return MailResult.Failure(MailError.InvalidSubject, "Subject not specified");

        if (string.IsNullOrWhiteSpace(message.TextBody) && string.IsNullOrWhiteSpace(message.HtmlBody))
            return MailResult.Failure(MailError.InvalidRecipient, "Message body is empty");

        return MailResult.Success();
    }

    /// <summary>
    /// Creates an SMTP client
    /// </summary>
    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_config.Host, _config.Port)
        {
            Credentials = new NetworkCredential(_config.Username, _config.Password),
            Timeout = _config.TimeoutSeconds * 1000,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Configure SSL/TLS
        client.EnableSsl = _config.SecurityMode != SmtpSecurityMode.None;

        if (_config.UseConnectionPooling && _config.MaxPooledConnections > 0)
        {
            client.ServicePoint.ConnectionLimit = _config.MaxPooledConnections;
        }

        return client;
    }

    /// <summary>
    /// Creates a MailMessage from a MailMessage record
    /// </summary>
    private System.Net.Mail.MailMessage CreateMailMessage(Models.MailMessage message)
    {
        var mail = new System.Net.Mail.MailMessage
        {
            From = new MailAddress(message.From, message.FromName),
            Subject = message.Subject,
            IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
        };

        // Add recipients
        foreach (var to in message.To)
            mail.To.Add(new MailAddress(to));

        // Add CC recipients
        foreach (var cc in message.Cc)
            mail.CC.Add(new MailAddress(cc));

        // Add BCC recipients
        foreach (var bcc in message.Bcc)
            mail.Bcc.Add(new MailAddress(bcc));

        // Handle body - prefer multipart if both text and HTML exist
        if (!string.IsNullOrWhiteSpace(message.TextBody) && !string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));
            mail.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));
            mail.Body = message.HtmlBody; // Fallback
        }
        else if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mail.Body = message.HtmlBody;
            mail.IsBodyHtml = true;
        }
        else if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            mail.Body = message.TextBody;
            mail.IsBodyHtml = false;
        }

        // Add attachments
        foreach (var attachmentPath in message.Attachments)
        {
            if (File.Exists(attachmentPath))
            {
                mail.Attachments.Add(new Attachment(attachmentPath));
            }
        }

        // Add headers
        foreach (var header in message.Headers)
        {
            mail.Headers.Add(header.Key, header.Value);
        }

        // Set priority
        mail.Priority = message.Priority switch
        {
            Models.MailPriority.Low => System.Net.Mail.MailPriority.Low,
            Models.MailPriority.High => System.Net.Mail.MailPriority.High,
            _ => System.Net.Mail.MailPriority.Normal
        };

        return mail;
    }
}
