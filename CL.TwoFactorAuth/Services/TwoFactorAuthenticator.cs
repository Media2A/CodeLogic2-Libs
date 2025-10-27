using OtpNet;
using QRCoder;
using CL.TwoFactorAuth.Models;
using CodeLogic.Abstractions;

namespace CL.TwoFactorAuth.Services;

/// <summary>
/// Provides two-factor authentication services including TOTP generation and validation
/// </summary>
public class TwoFactorAuthenticator
{
    private readonly TwoFactorAuthConfiguration _config;
    private readonly ILogger _logger;

    public TwoFactorAuthenticator(TwoFactorAuthConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a new secret key for two-factor authentication
    /// </summary>
    /// <returns>A Base32-encoded secret key</returns>
    public string GenerateSecretKey()
    {
        var secretBytes = KeyGeneration.GenerateRandomKey();
        return Base32Encoding.ToString(secretBytes);
    }

    /// <summary>
    /// Generates a new two-factor authentication key with metadata
    /// </summary>
    public TwoFactorKey GenerateNewKey(string issuerName, string userName)
    {
        var secretKey = GenerateSecretKey();
        return new TwoFactorKey
        {
            SecretKey = secretKey,
            IssuerName = issuerName,
            UserName = userName
        };
    }

    /// <summary>
    /// Generates the current TOTP code for a given secret key
    /// </summary>
    public string GenerateTOTP(string secretKey)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretBytes, step: _config.TimeStepSeconds);
            var code = totp.ComputeTotp();
            _logger.Debug($"Generated TOTP code");
            return code;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to generate TOTP", ex);
            throw;
        }
    }

    /// <summary>
    /// Validates a TOTP code against a secret key
    /// </summary>
    public TotpValidationResult ValidateTOTP(string userProvidedCode, string secretKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userProvidedCode))
            {
                return TotpValidationResult.Invalid("Code cannot be empty");
            }

            if (userProvidedCode.Length != 6)
            {
                return TotpValidationResult.Invalid("Code must be 6 digits");
            }

            var secretBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretBytes, step: _config.TimeStepSeconds);

            if (totp.VerifyTotp(userProvidedCode, out long window, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                _logger.Debug("TOTP validation successful");
                return TotpValidationResult.Valid((int)window);
            }

            _logger.Debug("TOTP validation failed");
            return TotpValidationResult.Invalid("Invalid code");
        }
        catch (Exception ex)
        {
            _logger.Error("Error during TOTP validation", ex);
            return TotpValidationResult.Invalid($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the remaining seconds until the current TOTP code expires
    /// </summary>
    public int GetSecondsUntilCodeExpires()
    {
        var now = DateTime.UtcNow;
        var secondsIntoStep = now.Second % _config.TimeStepSeconds;
        return _config.TimeStepSeconds - secondsIntoStep;
    }

    /// <summary>
    /// Gets the current time window for TOTP
    /// </summary>
    public long GetCurrentTimeWindow()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() / _config.TimeStepSeconds;
    }
}
