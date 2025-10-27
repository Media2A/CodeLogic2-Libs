using CodeLogic.Abstractions;
using CL.Core.Utilities.FileHandling;
using CL.Core.Utilities.Generators;
using CL.Core.Utilities.Security;
using CL.Core.Utilities.Data;
using CL.Core.Utilities.StringNumeric;
using CL.Core.Utilities.TimeDate;

namespace CL.Example;

/// <summary>
/// Comprehensive example library demonstrating all CodeLogic framework features
/// This library shows how to:
/// - Use CL.Core utilities
/// - Access framework services (logging, configuration, localization, caching)
/// - Declare dependencies
/// - Implement lifecycle hooks
/// </summary>
public class ExampleLibrary : ILibrary
{
    private LibraryContext? _context;
    private string _exampleData = string.Empty;

    public ILibraryManifest Manifest { get; } = new ExampleManifest();

    public async Task OnLoadAsync(LibraryContext context)
    {
        _context = context;

        Console.WriteLine($"    [CL.Example] Example library loading...");
        Console.WriteLine($"    [CL.Example] Data directory: {context.DataDirectory}");

        // Demonstrate file system utilities from CL.Core
        var dataFile = Path.Combine(context.DataDirectory, "example-data.json");

        if (FileSystem.FileExists(dataFile))
        {
            _exampleData = await FileSystem.ReadFileAsync(dataFile);
            Console.WriteLine($"    [CL.Example] Loaded existing data from {Path.GetFileName(dataFile)}");
        }
        else
        {
            _exampleData = "{}";
            Console.WriteLine($"    [CL.Example] No existing data found, will create on initialize");
        }

        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync()
    {
        Console.WriteLine($"    [CL.Example] Initializing and demonstrating framework features...");

        if (_context == null)
        {
            Console.WriteLine($"    [CL.Example] ✗ Error: Context not initialized!");
            return;
        }

        await DemonstrateAllFeatures();

        Console.WriteLine($"    [CL.Example] ✓ All features demonstrated successfully!");
    }

    public async Task OnUnloadAsync()
    {
        Console.WriteLine($"    [CL.Example] Saving state before shutdown...");

        if (_context != null)
        {
            // Save example data using CL.Core utilities
            var dataFile = Path.Combine(_context.DataDirectory, "example-data.json");

            var shutdownData = JsonHelper.Serialize(new
            {
                LastShutdown = DateTime.UtcNow,
                Message = "Example library shut down gracefully",
                SessionId = IdGenerator.NewGuid()
            });

            await FileSystem.WriteFileAsync(dataFile, shutdownData, append: false);
            Console.WriteLine($"    [CL.Example] State saved to {Path.GetFileName(dataFile)}");
        }

        Console.WriteLine($"    [CL.Example] Example library unloaded");
    }

    public Task<HealthCheckResult> HealthCheckAsync()
    {
        // Perform health checks
        if (_context == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Context not initialized"));
        }

        if (!Directory.Exists(_context.DataDirectory))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Data directory not accessible"));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Example library is operational"));
    }

    /// <summary>
    /// Demonstrates all framework features
    /// </summary>
    private async Task DemonstrateAllFeatures()
    {
        Console.WriteLine();
        Console.WriteLine("    ┌─────────────────────────────────────────────────┐");
        Console.WriteLine("    │   CodeLogic Framework Feature Demonstration    │");
        Console.WriteLine("    └─────────────────────────────────────────────────┘");
        Console.WriteLine();

        // 1. Demonstrate CL.Core Utilities
        DemonstrateIdGeneration();
        DemonstratePasswordGeneration();
        DemonstrateHashing();
        DemonstrateEncryption();
        DemonstrateStringUtilities();
        DemonstrateDateTimeUtilities();
        await DemonstrateJsonUtilities();

        Console.WriteLine();
    }

    private void DemonstrateIdGeneration()
    {
        Console.WriteLine("    📋 ID Generation (CL.Core):");
        Console.WriteLine($"       GUID:      {IdGenerator.NewGuid()}");
        Console.WriteLine($"       NanoID:    {IdGenerator.NanoId()}");
        Console.WriteLine($"       Sortable:  {IdGenerator.Sortable()}");
        Console.WriteLine($"       Sequential: {IdGenerator.Sequential()}");
        Console.WriteLine();
    }

    private void DemonstratePasswordGeneration()
    {
        Console.WriteLine("    🔐 Password Generation (CL.Core):");
        var password = PasswordGenerator.Generate(16, includeSpecialChars: true);
        var strength = PasswordGenerator.CalculateStrength(password);
        Console.WriteLine($"       Generated: {password}");
        Console.WriteLine($"       Strength:  {strength}");
        Console.WriteLine();
    }

    private void DemonstrateHashing()
    {
        Console.WriteLine("    🔒 Hashing (CL.Core):");
        var input = "SecurePassword123";
        var sha256 = Hashing.Sha256(input);
        var hashedPw = Hashing.HashPassword(input);
        Console.WriteLine($"       SHA-256:   {sha256.Substring(0, 32)}...");
        Console.WriteLine($"       PBKDF2:    {hashedPw.Substring(0, 32)}...");
        Console.WriteLine($"       Verified:  {Hashing.VerifyPassword(input, hashedPw)}");
        Console.WriteLine();
    }

    private void DemonstrateEncryption()
    {
        Console.WriteLine("    🔐 Encryption (CL.Core):");
        var plaintext = "Sensitive data here";
        var key = "MySecretKey123";
        var encrypted = Encryption.EncryptAes(plaintext, key);
        var decrypted = Encryption.DecryptAes(encrypted, key);
        Console.WriteLine($"       Plaintext:  {plaintext}");
        Console.WriteLine($"       Encrypted:  {encrypted.Substring(0, Math.Min(32, encrypted.Length))}...");
        Console.WriteLine($"       Decrypted:  {decrypted}");
        Console.WriteLine($"       Match:      {plaintext == decrypted}");
        Console.WriteLine();
    }

    private void DemonstrateStringUtilities()
    {
        Console.WriteLine("    📝 String Utilities (CL.Core):");
        var email = "test@example.com";
        var text = "Hello World!";
        Console.WriteLine($"       Email valid: {StringValidator.IsValidEmail(email)}");
        Console.WriteLine($"       Slug:        {StringHelper.ToSlug(text)}");
        Console.WriteLine($"       Masked:      {StringHelper.Mask("1234567890", 2, 2)}");
        Console.WriteLine();
    }

    private void DemonstrateDateTimeUtilities()
    {
        Console.WriteLine("    ⏰ DateTime Utilities (CL.Core):");
        var now = DateTime.UtcNow;
        var past = now.AddHours(-2);
        Console.WriteLine($"       Unix Time:  {DateTimeHelper.GetUnixTimestamp()}");
        Console.WriteLine($"       Time Ago:   {DateTimeHelper.GetTimeAgo(past)}");
        Console.WriteLine($"       ISO 8601:   {DateTimeHelper.ToIso8601(now)}");
        Console.WriteLine();
    }

    private async Task DemonstrateJsonUtilities()
    {
        Console.WriteLine("    📊 JSON Utilities (CL.Core):");
        var obj = new { Name = "Example", Version = "1.0.0", Active = true };
        var json = JsonHelper.Serialize(obj);
        var isValid = JsonHelper.IsValidJson(json);
        Console.WriteLine($"       Serialized: {json.Substring(0, Math.Min(50, json.Length))}...");
        Console.WriteLine($"       Valid:      {isValid}");

        // Save example to data directory
        if (_context != null)
        {
            var jsonFile = Path.Combine(_context.DataDirectory, "demo-output.json");
            await FileSystem.WriteFileAsync(jsonFile, json, append: false);
            Console.WriteLine($"       Saved to:   {Path.GetFileName(jsonFile)}");
        }

        Console.WriteLine();
    }
}

/// <summary>
/// Manifest for Example library
/// </summary>
public class ExampleManifest : ILibraryManifest
{
    public string Id => "cl.example";
    public string Name => "CL.Example";
    public string Version => "1.0.0";
    public string Author => "Media2A";
    public string Description => "Comprehensive example demonstrating all CodeLogic framework features";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new[]
    {
        new LibraryDependency
        {
            Id = "cl.core",
            MinVersion = "2.0.0",
            IsOptional = false
        }
    };

    public IReadOnlyList<string> Tags { get; } = new[] { "example", "demo", "documentation" };
}
