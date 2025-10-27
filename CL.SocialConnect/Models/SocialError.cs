namespace CL.SocialConnect.Models;

/// <summary>
/// Error types for social platform operations
/// </summary>
public enum SocialError
{
    /// <summary>
    /// No error
    /// </summary>
    None,

    /// <summary>
    /// Invalid or missing credentials
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// User not found on the platform
    /// </summary>
    UserNotFound,

    /// <summary>
    /// Authentication failed
    /// </summary>
    AuthenticationFailed,

    /// <summary>
    /// Invalid OAuth2 code or state
    /// </summary>
    InvalidOAuthCode,

    /// <summary>
    /// Token refresh failed
    /// </summary>
    TokenRefreshFailed,

    /// <summary>
    /// API rate limit exceeded
    /// </summary>
    RateLimited,

    /// <summary>
    /// Network error occurred
    /// </summary>
    NetworkError,

    /// <summary>
    /// API request timeout
    /// </summary>
    RequestTimeout,

    /// <summary>
    /// Invalid webhook URL
    /// </summary>
    InvalidWebhookUrl,

    /// <summary>
    /// Webhook delivery failed
    /// </summary>
    WebhookDeliveryFailed,

    /// <summary>
    /// Invalid or expired token
    /// </summary>
    InvalidToken,

    /// <summary>
    /// User is banned
    /// </summary>
    UserBanned,

    /// <summary>
    /// Operation not allowed
    /// </summary>
    OperationNotAllowed,

    /// <summary>
    /// Unknown error occurred
    /// </summary>
    Unknown
}
