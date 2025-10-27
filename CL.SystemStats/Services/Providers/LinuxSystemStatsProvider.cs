namespace CL.SystemStats.Services.Providers;

using System.Diagnostics;
using System.Text.RegularExpressions;
using Abstractions;
using Models;

/// <summary>
/// Linux-specific system statistics provider using /proc filesystem
/// </summary>
internal class LinuxSystemStatsProvider : ISystemStatsProvider
{
    private const string ProcPath = "/proc";
    private const string SysPath = "/sys";
    private readonly SystemStatsConfiguration _config;
    private readonly Dictionary<int, (long userTime, long systemTime)> _processTimeCache = [];
    private long _lastBootTimeTicks;

    /// <summary>
    /// Initializes a new instance of the LinuxSystemStatsProvider class
    /// </summary>
    public LinuxSystemStatsProvider(SystemStatsConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            // Verify /proc filesystem is available
            if (!Directory.Exists(ProcPath))
                throw new InvalidOperationException($"{ProcPath} filesystem not found");

            // Get boot time
            _lastBootTimeTicks = await GetBootTimeAsync();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize Linux system stats provider", ex);
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync()
    {
        try
        {
            var cpuinfoPath = Path.Combine(ProcPath, "cpuinfo");
            if (!File.Exists(cpuinfoPath))
                return Task.FromResult(SystemStatsResult<CpuInfo>.Failure(
                    SystemStatsError.FileNotFound,
                    $"File {cpuinfoPath} not found"));

            var lines = File.ReadAllLines(cpuinfoPath);
            var cpuInfo = ParseCpuInfo(lines);

            if (cpuInfo == null)
                return Task.FromResult(SystemStatsResult<CpuInfo>.Failure(
                    SystemStatsError.ParseError,
                    "Failed to parse CPU information"));

            return Task.FromResult(SystemStatsResult<CpuInfo>.Success(cpuInfo));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<CpuInfo>.Failure(
                SystemStatsError.ProcFilesystemError,
                $"Failed to retrieve CPU info: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync()
    {
        try
        {
            var statPath = Path.Combine(ProcPath, "stat");
            if (!File.Exists(statPath))
                return Task.FromResult(SystemStatsResult<CpuStats>.Failure(
                    SystemStatsError.FileNotFound,
                    $"File {statPath} not found"));

            var lines = File.ReadAllLines(statPath);
            var cpuStats = ParseCpuStats(lines);

            if (cpuStats == null)
                return Task.FromResult(SystemStatsResult<CpuStats>.Failure(
                    SystemStatsError.ParseError,
                    "Failed to parse CPU statistics"));

            return Task.FromResult(SystemStatsResult<CpuStats>.Success(cpuStats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<CpuStats>.Failure(
                SystemStatsError.ProcFilesystemError,
                $"Failed to retrieve CPU stats: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync()
    {
        try
        {
            // Linux doesn't provide detailed memory configuration in standard /proc
            // Using physical memory as total
            long totalMemory = (long)Environment.OSVersion.Platform; // Placeholder

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = "-c \"grep MemTotal /proc/meminfo | awk '{print $2 * 1024}'\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (long.TryParse(output, out long parsedMemory))
                totalMemory = parsedMemory;

            var memoryInfo = new MemoryInfo
            {
                TotalBytes = totalMemory
            };

            return Task.FromResult(SystemStatsResult<MemoryInfo>.Success(memoryInfo));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<MemoryInfo>.Failure(
                SystemStatsError.ProcFilesystemError,
                $"Failed to retrieve memory info: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync()
    {
        try
        {
            var meminfoPath = Path.Combine(ProcPath, "meminfo");
            if (!File.Exists(meminfoPath))
                return Task.FromResult(SystemStatsResult<MemoryStats>.Failure(
                    SystemStatsError.FileNotFound,
                    $"File {meminfoPath} not found"));

            var memStats = ParseMemoryStats(File.ReadAllLines(meminfoPath));

            if (memStats == null)
                return Task.FromResult(SystemStatsResult<MemoryStats>.Failure(
                    SystemStatsError.ParseError,
                    "Failed to parse memory statistics"));

            return Task.FromResult(SystemStatsResult<MemoryStats>.Success(memStats));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SystemStatsResult<MemoryStats>.Failure(
                SystemStatsError.ProcFilesystemError,
                $"Failed to retrieve memory stats: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync()
    {
        try
        {
            var uptimePath = Path.Combine(ProcPath, "uptime");
            if (!File.Exists(uptimePath))
                return SystemStatsResult<TimeSpan>.Failure(
                    SystemStatsError.FileNotFound,
                    $"File {uptimePath} not found");

            var content = await File.ReadAllTextAsync(uptimePath);
            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && double.TryParse(parts[0], out double uptime))
            {
                return SystemStatsResult<TimeSpan>.Success(TimeSpan.FromSeconds(uptime));
            }

            return SystemStatsResult<TimeSpan>.Failure(
                SystemStatsError.ParseError,
                "Failed to parse uptime");
        }
        catch (Exception ex)
        {
            return SystemStatsResult<TimeSpan>.Failure(
                SystemStatsError.ProcFilesystemError,
                $"Failed to retrieve system uptime: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId)
    {
        try
        {
            var statPath = Path.Combine(ProcPath, processId.ToString(), "stat");
            if (!File.Exists(statPath))
                return Task.FromResult(SystemStatsResult<ProcessStats>.Failure(
                    SystemStatsError.FileNotFound,
                    $"Process {processId} not found"));

            var stats = ParseProcessStat(File.ReadAllText(statPath), processId);

            if (stats == null)
                return Task.FromResult(SystemStatsResult<ProcessStats>.Failure(
                    SystemStatsError.ParseError,
                    "Failed to parse process stats"));

            return Task.FromResult(SystemStatsResult<ProcessStats>.Success(stats));
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
            var procDir = new DirectoryInfo(ProcPath);
            var statsList = new List<ProcessStats>();

            foreach (var dir in procDir.GetDirectories())
            {
                if (int.TryParse(dir.Name, out int pid))
                {
                    var statPath = Path.Combine(dir.FullName, "stat");
                    if (File.Exists(statPath))
                    {
                        var stats = ParseProcessStat(File.ReadAllText(statPath), pid);
                        if (stats != null)
                            statsList.Add(stats);
                    }
                }
            }

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(statsList.AsReadOnly()));
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
            var allProcessesResult = GetAllProcessesAsync().Result;

            if (!allProcessesResult.IsSuccess)
                return Task.FromResult(allProcessesResult);

            var topProcesses = allProcessesResult.Value!
                .OrderByDescending(p => p.CpuUsagePercent)
                .Take(topCount)
                .ToList()
                .AsReadOnly();

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(topProcesses));
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
            var allProcessesResult = GetAllProcessesAsync().Result;

            if (!allProcessesResult.IsSuccess)
                return Task.FromResult(allProcessesResult);

            var topProcesses = allProcessesResult.Value!
                .OrderByDescending(p => p.MemoryBytes)
                .Take(topCount)
                .ToList()
                .AsReadOnly();

            return Task.FromResult(SystemStatsResult<IReadOnlyList<ProcessStats>>.Success(topProcesses));
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

            // Count running processes
            var allProcs = Directory.GetDirectories(ProcPath)
                .Where(d => int.TryParse(new DirectoryInfo(d).Name, out _))
                .Count();

            var snapshot = new SystemSnapshot
            {
                CpuInfo = cpuInfoResult.Value!,
                CpuStats = cpuStatsResult.Value!,
                MemoryInfo = memInfoResult.Value!,
                MemoryStats = memStatsResult.Value!,
                SystemUptime = uptimeResult.Value!,
                ProcessCount = allProcs
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
        _processTimeCache.Clear();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Parses CPU information from /proc/cpuinfo
    /// </summary>
    private static CpuInfo? ParseCpuInfo(string[] lines)
    {
        string? modelName = null;
        int physicalCores = Environment.ProcessorCount;
        int logicalProcessors = Environment.ProcessorCount;
        string? vendor = null;
        string? family = null;
        double? baseFreq = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                modelName ??= line.Split(':')[1].Trim();
            else if (line.StartsWith("cpu cores", StringComparison.OrdinalIgnoreCase))
                int.TryParse(line.Split(':')[1].Trim(), out physicalCores);
            else if (line.StartsWith("cpu family", StringComparison.OrdinalIgnoreCase))
                family ??= line.Split(':')[1].Trim();
            else if (line.StartsWith("vendor_id", StringComparison.OrdinalIgnoreCase))
                vendor ??= line.Split(':')[1].Trim();
            else if (line.StartsWith("cpu MHz", StringComparison.OrdinalIgnoreCase) && baseFreq == null)
            {
                if (double.TryParse(line.Split(':')[1].Trim(), out var freq))
                    baseFreq = freq;
            }
        }

        if (string.IsNullOrEmpty(modelName))
            return null;

        return new CpuInfo
        {
            ModelName = modelName,
            PhysicalCores = physicalCores,
            LogicalProcessors = logicalProcessors,
            BaseFrequencyMhz = baseFreq,
            MaxFrequencyMhz = baseFreq,
            CurrentFrequencyMhz = baseFreq,
            Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            Vendor = vendor ?? "Unknown",
            Family = family
        };
    }

    /// <summary>
    /// Parses CPU statistics from /proc/stat
    /// </summary>
    private static CpuStats? ParseCpuStats(string[] lines)
    {
        var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(cpuLine))
            return null;

        var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
            return null;

        // Parse per-core CPU usage
        var perCoreUsage = new List<double>();
        int coreIndex = 0;
        while (true)
        {
            var coreLine = lines.FirstOrDefault(l => l.StartsWith($"cpu{coreIndex}", StringComparison.Ordinal) && !l.StartsWith("cpu ", StringComparison.Ordinal));
            if (coreLine == null)
                break;

            var coreParts = coreLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (coreParts.Length >= 5)
            {
                double usage = CalculateCpuUsage(coreParts);
                perCoreUsage.Add(usage);
            }

            coreIndex++;
        }

        double overallUsage = CalculateCpuUsage(parts);

        return new CpuStats
        {
            OverallUsagePercent = Math.Max(0, Math.Min(100, overallUsage)),
            PerCoreUsagePercent = perCoreUsage.AsReadOnly()
        };
    }

    /// <summary>
    /// Parses memory statistics from /proc/meminfo
    /// </summary>
    private static MemoryStats? ParseMemoryStats(string[] lines)
    {
        var memDict = new Dictionary<string, long>();

        foreach (var line in lines)
        {
            var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && long.TryParse(parts[1].Split()[0], out long value))
                memDict[parts[0].Trim()] = value * 1024; // Convert from KB to bytes
        }

        if (!memDict.TryGetValue("MemTotal", out long total))
            return null;

        memDict.TryGetValue("MemAvailable", out long available);
        memDict.TryGetValue("MemFree", out long free);
        memDict.TryGetValue("Cached", out long cached);
        memDict.TryGetValue("Buffers", out long buffered);

        long used = total - free;
        double usagePercent = (used / (double)total) * 100;

        return new MemoryStats
        {
            TotalBytes = total,
            UsedBytes = used,
            FreeBytes = free,
            AvailableBytes = available,
            UsagePercent = Math.Max(0, Math.Min(100, usagePercent)),
            CachedBytes = cached > 0 ? cached : null,
            BufferedBytes = buffered > 0 ? buffered : null
        };
    }

    /// <summary>
    /// Parses process statistics from /proc/[pid]/stat
    /// </summary>
    private ProcessStats? ParseProcessStat(string content, int processId)
    {
        try
        {
            // Format: pid (comm) state ppid pgrp session tty_nr tpgid flags minflt cminflt majflt cmajflt utime stime cutime cstime priority nice num_threads itrealvalue starttime
            var lastParen = content.LastIndexOf(')');
            if (lastParen < 0)
                return null;

            var beforeParen = content[..lastParen];
            var afterParen = content[(lastParen + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string processName = beforeParen.Substring(beforeParen.IndexOf('(') + 1);

            if (afterParen.Length < 20)
                return null;

            string state = afterParen[0].Trim();
            _ = long.TryParse(afterParen[19], out long startTime);
            _ = int.TryParse(afterParen[15], out int numThreads);

            var statusPath = Path.Combine(ProcPath, processId.ToString(), "status");
            long memoryBytes = 0;

            if (File.Exists(statusPath))
            {
                var statusLines = File.ReadAllLines(statusPath);
                foreach (var line in statusLines)
                {
                    if (line.StartsWith("VmRSS:", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = line.Split(':')[1].Split()[0];
                        if (long.TryParse(value, out long vmrss))
                            memoryBytes = vmrss * 1024;
                        break;
                    }
                }
            }

            return new ProcessStats
            {
                ProcessId = processId,
                ProcessName = processName,
                Priority = state,
                CpuUsagePercent = 0,
                MemoryBytes = memoryBytes,
                ThreadCount = numThreads > 0 ? numThreads : null,
                StartTime = UnixTimeStampToDateTime(startTime / 100)
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates CPU usage percentage from /proc/stat parts
    /// </summary>
    private static double CalculateCpuUsage(string[] parts)
    {
        if (parts.Length < 5)
            return 0;

        if (!long.TryParse(parts[1], out long user) ||
            !long.TryParse(parts[2], out long nice) ||
            !long.TryParse(parts[3], out long system) ||
            !long.TryParse(parts[4], out long idle))
            return 0;

        long total = user + nice + system + idle;
        long busy = total - idle;

        return total > 0 ? (busy / (double)total) * 100 : 0;
    }

    /// <summary>
    /// Gets boot time from /proc/stat
    /// </summary>
    private static async Task<long> GetBootTimeAsync()
    {
        try
        {
            var statPath = Path.Combine(ProcPath, "stat");
            var lines = await File.ReadAllLinesAsync(statPath);
            var bootLine = lines.FirstOrDefault(l => l.StartsWith("btime", StringComparison.OrdinalIgnoreCase));

            if (bootLine != null && long.TryParse(bootLine.Split()[1], out long btime))
                return btime * 10000000; // Convert to ticks

            return DateTime.UtcNow.Ticks;
        }
        catch
        {
            return DateTime.UtcNow.Ticks;
        }
    }

    /// <summary>
    /// Converts Unix timestamp to DateTime
    /// </summary>
    private static DateTime? UnixTimeStampToDateTime(long unixTimeStamp)
    {
        try
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }
        catch
        {
            return null;
        }
    }
}
