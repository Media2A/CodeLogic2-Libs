namespace CL.SystemStats.Services.Providers;

using System.Diagnostics;
using System.Management;
using Abstractions;
using Models;

/// <summary>
/// Windows-specific system statistics provider using WMI and Performance Counters
/// </summary>
internal class WindowsSystemStatsProvider : ISystemStatsProvider
{
    private readonly SystemStatsConfiguration _config;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter[]? _cpuCoreCounters;
    private readonly List<double> _cpuSamples = [];

    /// <summary>
    /// Initializes a new instance of the WindowsSystemStatsProvider class
    /// </summary>
    public WindowsSystemStatsProvider(SystemStatsConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            // Initialize CPU performance counter
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _cpuCounter.NextValue(); // First call always returns 0

            // Initialize per-core CPU counters
            int coreCount = Environment.ProcessorCount;
            _cpuCoreCounters = new PerformanceCounter[coreCount];

            for (int i = 0; i < coreCount; i++)
            {
                _cpuCoreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString(), true);
                _cpuCoreCounters[i].NextValue(); // Prime the counters
            }

            await Task.Delay(100); // Give counters time to initialize
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Windows system stats provider", ex);
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            var processorInfo = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (processorInfo == null)
                return Task.FromResult(SystemStatsResult<CpuInfo>.Failure(
                    SystemStatsError.CpuInfoUnavailable,
                    "Unable to retrieve CPU information from WMI"));

            var cpuInfo = new CpuInfo
            {
                ModelName = processorInfo["Name"]?.ToString() ?? "Unknown",
                PhysicalCores = Convert.ToInt32(processorInfo["NumberOfCores"] ?? 1),
                LogicalProcessors = Convert.ToInt32(processorInfo["NumberOfLogicalProcessors"] ?? 1),
                BaseFrequencyMhz = Convert.ToDouble(processorInfo["CurrentClockSpeed"] ?? 0),
                MaxFrequencyMhz = Convert.ToDouble(processorInfo["MaxClockSpeed"] ?? 0),
                CurrentFrequencyMhz = Convert.ToDouble(processorInfo["CurrentClockSpeed"] ?? 0),
                Architecture = GetArchitecture(processorInfo),
                Vendor = ExtractVendor(processorInfo["Name"]?.ToString() ?? ""),
                Family = processorInfo["Family"]?.ToString() ?? "Unknown"
            };

            return Task.FromResult(SystemStatsResult<CpuInfo>.Success(cpuInfo));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<CpuInfo>.Failure(
                SystemStatsError.WmiQueryFailed,
                $"Failed to retrieve CPU info: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync()
    {
        try
        {
            if (_cpuCounter == null || _cpuCoreCounters == null)
                return Task.FromResult(SystemStatsResult<CpuStats>.Failure(
                    SystemStatsError.NotInitialized,
                    "Provider not initialized"));

            // Collect CPU samples
            _cpuSamples.Add(_cpuCounter.NextValue());
            if (_cpuSamples.Count > _config.CpuSamplesForAverage)
                _cpuSamples.RemoveAt(0);

            double overallUsage = _cpuSamples.Count > 0 ? _cpuSamples.Average() : 0;

            // Get per-core usage
            var perCoreUsage = _cpuCoreCounters
                .Select(counter => (double)Math.Max(0, Math.Min(100, counter.NextValue())))
                .ToList()
                .AsReadOnly();

            var cpuStats = new CpuStats
            {
                OverallUsagePercent = Math.Max(0, Math.Min(100, overallUsage)),
                PerCoreUsagePercent = perCoreUsage
            };

            return Task.FromResult(SystemStatsResult<CpuStats>.Success(cpuStats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<CpuStats>.Failure(
                SystemStatsError.WmiQueryFailed,
                $"Failed to retrieve CPU stats: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory");
            long totalMemory = 0;

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                totalMemory += Convert.ToInt64(obj["Capacity"] ?? 0);
            }

            if (totalMemory == 0)
            {
                // Fallback using WMI Operating System
                try
                {
                    using var osSearcher = new ManagementObjectSearcher("select TotalVisibleMemorySize from Win32_OperatingSystem");
                    var osInfo = osSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (osInfo != null)
                        totalMemory = Convert.ToInt64(osInfo["TotalVisibleMemorySize"] ?? 0) * 1024;
                }
                catch
                {
                    // If that fails, use the native method
                    totalMemory = GC.GetTotalMemory(false);
                }
            }

            var memoryInfo = new MemoryInfo
            {
                TotalBytes = totalMemory
            };

            return Task.FromResult(SystemStatsResult<MemoryInfo>.Success(memoryInfo));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<MemoryInfo>.Failure(
                SystemStatsError.WmiQueryFailed,
                $"Failed to retrieve memory info: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync()
    {
        try
        {
            var memInfo = GC.GetTotalMemory(false);
            using var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize, FreePhysicalMemory from Win32_OperatingSystem");
            var osInfo = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (osInfo == null)
                return Task.FromResult(SystemStatsResult<MemoryStats>.Failure(
                    SystemStatsError.WmiQueryFailed,
                    "Unable to retrieve memory statistics"));

            long totalBytes = Convert.ToInt64(osInfo["TotalVisibleMemorySize"] ?? 0) * 1024;
            long freeBytes = Convert.ToInt64(osInfo["FreePhysicalMemory"] ?? 0) * 1024;
            long usedBytes = totalBytes - freeBytes;

            double usagePercent = totalBytes > 0 ? (usedBytes / (double)totalBytes) * 100 : 0;

            var memoryStats = new MemoryStats
            {
                TotalBytes = totalBytes,
                UsedBytes = usedBytes,
                FreeBytes = freeBytes,
                AvailableBytes = freeBytes,
                UsagePercent = Math.Max(0, Math.Min(100, usagePercent))
            };

            return Task.FromResult(SystemStatsResult<MemoryStats>.Success(memoryStats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<MemoryStats>.Failure(
                SystemStatsError.WmiQueryFailed,
                $"Failed to retrieve memory stats: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select LastBootUpTime from Win32_OperatingSystem");
            var osInfo = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (osInfo == null)
                return Task.FromResult(SystemStatsResult<TimeSpan>.Failure(
                    SystemStatsError.WmiQueryFailed,
                    "Unable to retrieve system uptime"));

            string lastBootTimeStr = osInfo["LastBootUpTime"]?.ToString() ?? "";
            if (ManagementDateTimeConverter.ToDateTime(lastBootTimeStr) is DateTime lastBootTime)
            {
                TimeSpan uptime = DateTime.UtcNow - lastBootTime;
                return Task.FromResult(SystemStatsResult<TimeSpan>.Success(uptime));
            }

            return Task.FromResult(SystemStatsResult<TimeSpan>.Failure(
                SystemStatsError.ParseError,
                "Failed to parse boot time"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<TimeSpan>.Failure(
                SystemStatsError.WmiQueryFailed,
                $"Failed to retrieve system uptime: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var stats = BuildProcessStats(process);
            process.Dispose();
            return Task.FromResult(SystemStatsResult<ProcessStats>.Success(stats));
        }
        catch (ArgumentException)
        {
            return Task.FromResult(SystemStatsResult<ProcessStats>.Failure(
                SystemStatsError.ProcessStatsUnavailable,
                $"Process with ID {processId} not found"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<ProcessStats>.Failure(
                SystemStatsError.ProcessStatsUnavailable,
                $"Failed to retrieve process stats: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetAllProcessesAsync()
    {
        try
        {
            var processes = Process.GetProcesses();
            var stats = processes
                .Select(BuildProcessStats)
                .Where(s => s != null)
                .Cast<ProcessStats>()
                .ToList()
                .AsReadOnly();

            foreach (var process in processes)
                process.Dispose();

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(stats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.ProcessStatsUnavailable,
                $"Failed to retrieve processes: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByCpuAsync(int topCount)
    {
        try
        {
            var processes = Process.GetProcesses();
            var stats = processes
                .Select(BuildProcessStats)
                .Where(s => s != null)
                .Cast<ProcessStats>()
                .OrderByDescending(s => s.CpuUsagePercent)
                .Take(topCount)
                .ToList()
                .AsReadOnly();

            foreach (var process in processes)
                process.Dispose();

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(stats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.ProcessStatsUnavailable,
                $"Failed to retrieve top CPU processes: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByMemoryAsync(int topCount)
    {
        try
        {
            var processes = Process.GetProcesses();
            var stats = processes
                .Select(BuildProcessStats)
                .Where(s => s != null)
                .Cast<ProcessStats>()
                .OrderByDescending(s => s.MemoryBytes)
                .Take(topCount)
                .ToList()
                .AsReadOnly();

            foreach (var process in processes)
                process.Dispose();

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(stats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.ProcessStatsUnavailable,
                $"Failed to retrieve top memory processes: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<SystemStatsResult<SystemSnapshot>> GetSystemSnapshotAsync()
    {
        try
        {
            var cpuInfoResult = await GetCpuInfoAsync();
            var cpuStatsResult = await GetCpuStatsAsync();
            var memInfoResult = await GetMemoryInfoAsync();
            var memStatsResult = await GetMemoryStatsAsync();
            var uptimeResult = await GetSystemUptimeAsync();

            if (!cpuInfoResult.IsSuccess || !cpuStatsResult.IsSuccess ||
                !memInfoResult.IsSuccess || !memStatsResult.IsSuccess ||
                !uptimeResult.IsSuccess)
            {
                return SystemStatsResult<SystemSnapshot>.Failure(
                    SystemStatsError.Unknown,
                    "Failed to retrieve one or more system components");
            }

            var processCount = Process.GetProcesses().Length;

            var snapshot = new SystemSnapshot
            {
                CpuInfo = cpuInfoResult.Value!,
                CpuStats = cpuStatsResult.Value!,
                MemoryInfo = memInfoResult.Value!,
                MemoryStats = memStatsResult.Value!,
                SystemUptime = uptimeResult.Value!,
                ProcessCount = processCount
            };

            return SystemStatsResult<SystemSnapshot>.Success(snapshot);
        }
        catch (Exception ex)
        {
            return SystemStatsResult<SystemSnapshot>.Failure(
                SystemStatsError.Unknown,
                $"Failed to create system snapshot: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _cpuCounter?.Dispose();
        if (_cpuCoreCounters != null)
        {
            foreach (var counter in _cpuCoreCounters)
                counter?.Dispose();
        }

        _cpuSamples.Clear();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Builds process statistics from a Process object
    /// </summary>
    private static ProcessStats? BuildProcessStats(Process process)
    {
        try
        {
            return new ProcessStats
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                Priority = process.PriorityClass.ToString(),
                CpuUsagePercent = 0, // Requires Performance Counter sampling
                MemoryBytes = process.WorkingSet64,
                VirtualMemoryBytes = process.VirtualMemorySize64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                ProcessState = GetProcessState(process),
                StartTime = process.StartTime == default ? null : process.StartTime.ToUniversalTime()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the process state
    /// </summary>
    private static string GetProcessState(Process process)
    {
        try
        {
            return process.Responding ? "Running" : "NotResponding";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets CPU architecture from processor info
    /// </summary>
    private static string GetArchitecture(ManagementObject processorInfo)
    {
        var architecture = processorInfo["Architecture"]?.ToString();
        return architecture switch
        {
            "0" => "x86",
            "1" => "MIPS",
            "2" => "Alpha",
            "3" => "PowerPC",
            "5" => "ARM",
            "6" => "ia64",
            "9" => "x64",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Extracts vendor from CPU model name
    /// </summary>
    private static string ExtractVendor(string modelName)
    {
        if (modelName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            return "Intel";
        if (modelName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            return "AMD";
        if (modelName.Contains("ARM", StringComparison.OrdinalIgnoreCase))
            return "ARM";
        return "Unknown";
    }
}
