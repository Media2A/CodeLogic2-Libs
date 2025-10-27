namespace CL.SystemStats.Models;

/// <summary>
/// Represents CPU information and capabilities
/// </summary>
public record CpuInfo
{
    /// <summary>
    /// Gets the CPU model name
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// Gets the number of physical cores
    /// </summary>
    public required int PhysicalCores { get; init; }

    /// <summary>
    /// Gets the number of logical processors
    /// </summary>
    public required int LogicalProcessors { get; init; }

    /// <summary>
    /// Gets the base frequency in MHz
    /// </summary>
    public double? BaseFrequencyMhz { get; init; }

    /// <summary>
    /// Gets the max frequency in MHz
    /// </summary>
    public double? MaxFrequencyMhz { get; init; }

    /// <summary>
    /// Gets the current frequency in MHz
    /// </summary>
    public double? CurrentFrequencyMhz { get; init; }

    /// <summary>
    /// Gets the CPU architecture
    /// </summary>
    public string? Architecture { get; init; }

    /// <summary>
    /// Gets the CPU vendor
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Gets the CPU family
    /// </summary>
    public string? Family { get; init; }

    /// <summary>
    /// Gets when the CPU info was retrieved
    /// </summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents CPU usage statistics
/// </summary>
public record CpuStats
{
    /// <summary>
    /// Gets the overall CPU usage percentage (0-100)
    /// </summary>
    public required double OverallUsagePercent { get; init; }

    /// <summary>
    /// Gets the per-core CPU usage percentages
    /// </summary>
    public required IReadOnlyList<double> PerCoreUsagePercent { get; init; }

    /// <summary>
    /// Gets the CPU temperature in Celsius (if available)
    /// </summary>
    public double? TemperatureCelsius { get; init; }

    /// <summary>
    /// Gets the CPU context switches per second
    /// </summary>
    public double? ContextSwitchesPerSecond { get; init; }

    /// <summary>
    /// Gets the CPU interrupts per second
    /// </summary>
    public double? InterruptsPerSecond { get; init; }

    /// <summary>
    /// Gets when the stats were captured
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the average core usage
    /// </summary>
    public double GetAveragePerCoreUsage() =>
        PerCoreUsagePercent.Count > 0 ? PerCoreUsagePercent.Average() : 0;
}
