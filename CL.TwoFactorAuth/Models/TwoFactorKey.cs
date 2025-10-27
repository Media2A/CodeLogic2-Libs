namespace CL.TwoFactorAuth.Models;

/// <summary>
/// Represents a two-factor authentication key with associated metadata
/// </summary>
public record TwoFactorKey
{
    /// <summary>
    /// Gets the secret key in Base32 format
    /// </summary>
    public required string SecretKey { get; init; }

    /// <summary>
    /// Gets the issuer name (typically application name)
    /// </summary>
    public required string IssuerName { get; init; }

    /// <summary>
    /// Gets the user identifier or email
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Gets the provisioning URI for QR code generation
    /// </summary>
    public string ProvisioningUri { get; init; }

    public TwoFactorKey()
    {
        ProvisioningUri = GenerateProvisioningUri();
    }

    private string GenerateProvisioningUri()
    {
        return $"otpauth://totp/{Uri.EscapeDataString(IssuerName)}:{Uri.EscapeDataString(UserName)}" +
               $"?secret={SecretKey}&issuer={Uri.EscapeDataString(IssuerName)}";
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
