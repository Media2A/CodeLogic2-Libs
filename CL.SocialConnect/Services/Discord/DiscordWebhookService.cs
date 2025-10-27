using CL.SocialConnect.Models;
using CL.SocialConnect.Models.Discord;
using CodeLogic.Abstractions;
using System.Text.Json;

namespace CL.SocialConnect.Services.Discord;

/// <summary>
/// Service for sending messages via Discord webhooks
/// </summary>
public class DiscordWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the DiscordWebhookService
    /// </summary>
    public DiscordWebhookService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Sends a message via Discord webhook
    /// </summary>
    public async Task<SocialResult> SendMessageAsync(
        string webhookUrl,
        DiscordWebhookMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return SocialResult.Failure(SocialError.InvalidWebhookUrl, "Webhook URL cannot be null or empty");

            var json = JsonSerializer.Serialize(message);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.Warning($"Discord webhook failed with status {response.StatusCode}: {errorContent}");
                return SocialResult.Failure(SocialError.WebhookDeliveryFailed, $"HTTP {response.StatusCode}");
            }

            _logger.Debug("Discord webhook message sent successfully");
            return SocialResult.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.Error("Network error sending Discord webhook", ex);
            return SocialResult.Failure(SocialError.NetworkError, ex.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Discord webhook send was cancelled");
            return SocialResult.Failure(SocialError.RequestTimeout, "Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error("Error sending Discord webhook", ex);
            return SocialResult.Failure(SocialError.Unknown, ex.Message);
        }
    }

    /// <summary>
    /// Sends multiple messages via Discord webhook
    /// </summary>
    public async Task<SocialResult> SendMessagesAsync(
        string webhookUrl,
        IEnumerable<DiscordWebhookMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        _logger.Info($"Sending {messageList.Count} messages via Discord webhook");

        foreach (var message in messageList)
        {
            var result = await SendMessageAsync(webhookUrl, message, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result;

            // Small delay to avoid rate limiting
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return SocialResult.Success();
    }
}
