namespace CL.TwoFactorAuth.Models;

/// <summary>
/// Configuration settings for two-factor authentication
/// </summary>
public class TwoFactorAuthConfiguration
{
    /// <summary>
    /// Gets or sets the time step in seconds for TOTP (default: 30)
    /// </summary>
    public int TimeStepSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the window size for code validation tolerance (default: 1)
    /// </summary>
    public int WindowSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the QR code module size in pixels (default: 20)
    /// </summary>
    public int QrCodeModuleSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the error correction level for QR codes (default: Q)
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QrErrorCorrectionLevel.Q;
}

/// <summary>
/// QR code error correction levels
/// </summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>
    /// Level L - 7% error correction
    /// </summary>
    L,

    /// <summary>
    /// Level M - 15% error correction
    /// </summary>
    M,

    /// <summary>
    /// Level Q - 25% error correction
    /// </summary>
    Q,

    /// <summary>
    /// Level H - 30% error correction
    /// </summary>
    H
}

/// <summary>
/// Result of a 2FA operation
/// </summary>
public record TwoFactorResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the result message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets any additional data from the operation
    /// </summary>
    public object? Data { get; init; }

    public static TwoFactorResult Succeeded(string? message = null, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static TwoFactorResult Failed(string message) =>
        new() { Success = false, Message = message };
}
