namespace CL.TwoFactorAuth.Models;

/// <summary>
/// Represents a two-factor authentication key with associated metadata
/// </summary>
public record TwoFactorKey
{
    private string? _secretKey;
    private string? _issuerName;
    private string? _userName;

    /// <summary>
    /// Gets the secret key in Base32 format
    /// </summary>
    public required string SecretKey
    {
        get => _secretKey ?? string.Empty;
        init
        {
            _secretKey = value;
            UpdateProvisioningUri();
        }
    }

    /// <summary>
    /// Gets the issuer name (typically application name)
    /// </summary>
    public required string IssuerName
    {
        get => _issuerName ?? string.Empty;
        init
        {
            _issuerName = value;
            UpdateProvisioningUri();
        }
    }

    /// <summary>
    /// Gets the user identifier or email
    /// </summary>
    public required string UserName
    {
        get => _userName ?? string.Empty;
        init
        {
            _userName = value;
            UpdateProvisioningUri();
        }
    }

    /// <summary>
    /// Gets the provisioning URI for QR code generation
    /// </summary>
    public string ProvisioningUri { get; private set; } = string.Empty;

    private void UpdateProvisioningUri()
    {
        if (!string.IsNullOrEmpty(_secretKey) && !string.IsNullOrEmpty(_issuerName) && !string.IsNullOrEmpty(_userName))
        {
            ProvisioningUri = $"otpauth://totp/{Uri.EscapeDataString(_issuerName)}:{Uri.EscapeDataString(_userName)}" +
                           $"?secret={_secretKey}&issuer={Uri.EscapeDataString(_issuerName)}";
        }
    }
}

/// <summary>
/// Represents validation results for TOTP codes
/// </summary>
public record TotpValidationResult
{
    /// <summary>
    /// Gets whether the code is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the verification window that matched (if valid)
    /// </summary>
    public int? MatchedWindow { get; init; }

    /// <summary>
    /// Gets an error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static TotpValidationResult Valid(int? window = null) =>
        new() { IsValid = true, MatchedWindow = window };

    public static TotpValidationResult Invalid(string errorMessage = "Invalid code") =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
