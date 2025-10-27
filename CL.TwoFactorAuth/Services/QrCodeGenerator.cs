using QRCoder;
using CL.TwoFactorAuth.Models;
using CodeLogic.Abstractions;

namespace CL.TwoFactorAuth.Services;

/// <summary>
/// Generates QR codes for two-factor authentication setup
/// </summary>
public class QrCodeGenerator
{
    private readonly TwoFactorAuthConfiguration _config;
    private readonly ILogger _logger;

    public QrCodeGenerator(TwoFactorAuthConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a QR code and returns it as a PNG byte array
    /// </summary>
    public byte[] GenerateQrCodePng(TwoFactorKey key)
    {
        try
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(
                key.ProvisioningUri,
                MapErrorCorrectionLevel(_config.ErrorCorrectionLevel));

            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(_config.QrCodeModuleSize);

            _logger.Debug($"Generated QR code for {key.UserName}");
            return qrCodeBytes;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to generate QR code PNG", ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a QR code and returns it as a Base64-encoded PNG string
    /// </summary>
    public string GenerateQrCodeBase64(TwoFactorKey key)
    {
        try
        {
            var pngBytes = GenerateQrCodePng(key);
            var base64String = Convert.ToBase64String(pngBytes);
            _logger.Debug($"Generated Base64 QR code for {key.UserName}");
            return base64String;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to generate Base64 QR code", ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a QR code as a data URI (suitable for HTML img tags)
    /// </summary>
    public string GenerateQrCodeDataUri(TwoFactorKey key)
    {
        try
        {
            var base64 = GenerateQrCodeBase64(key);
            return $"data:image/png;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to generate QR code data URI", ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a QR code and saves it to a file
    /// </summary>
    public TwoFactorResult SaveQrCodeToFile(TwoFactorKey key, string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return TwoFactorResult.Failed("File path cannot be empty");
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var pngBytes = GenerateQrCodePng(key);
            File.WriteAllBytes(filePath, pngBytes);

            _logger.Info($"Saved QR code to {filePath}");
            return TwoFactorResult.Succeeded($"QR code saved to {filePath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save QR code to {filePath}", ex);
            return TwoFactorResult.Failed($"Error saving QR code: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a QR code as a BMP byte array
    /// </summary>
    public byte[] GenerateQrCodeBmp(TwoFactorKey key)
    {
        try
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(
                key.ProvisioningUri,
                MapErrorCorrectionLevel(_config.ErrorCorrectionLevel));

            var qrCode = new BitmapByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(_config.QrCodeModuleSize);

            _logger.Debug($"Generated QR code BMP for {key.UserName}");
            return qrCodeBytes;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to generate QR code BMP", ex);
            throw;
        }
    }

    private QRCodeGenerator.ECCLevel MapErrorCorrectionLevel(QrErrorCorrectionLevel level)
    {
        return level switch
        {
            QrErrorCorrectionLevel.L => QRCodeGenerator.ECCLevel.L,
            QrErrorCorrectionLevel.M => QRCodeGenerator.ECCLevel.M,
            QrErrorCorrectionLevel.Q => QRCodeGenerator.ECCLevel.Q,
            QrErrorCorrectionLevel.H => QRCodeGenerator.ECCLevel.H,
            _ => QRCodeGenerator.ECCLevel.Q
        };
    }
}
