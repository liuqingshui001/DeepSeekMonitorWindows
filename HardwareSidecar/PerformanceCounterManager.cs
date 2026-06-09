using System.Diagnostics;

namespace HardwareSidecar;

public class PerformanceCounterManager : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memAvailableCounter;
    private bool _initialized;

    public async Task InitializeAsync()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = _cpuCounter.NextValue(); // first call returns 0
            await Task.Delay(500);
            _memAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
            _ = _memAvailableCounter.NextValue();
            _initialized = true;
        }
        catch
        {
            _initialized = false;
        }
    }

    public float? GetCpuLoad()
    {
        if (!_initialized || _cpuCounter == null) return null;
        var val = _cpuCounter.NextValue();
        if (val < 0) val = 0;
        if (val > 100) val = 100;
        return val;
    }

    /// <summary>
    /// 获取磁盘读取速率 (KB/s) — 通过性能计数器，除以 1024 统一为 KB/s
    /// </summary>
    public float? GetDiskReadKBPerSec()
    {
        try
        {
            using var counter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _ = counter.NextValue();
            return counter.NextValue() / 1024f;
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取磁盘写入速率 (KB/s)
    /// </summary>
    public float? GetDiskWriteKBPerSec()
    {
        try
        {
            using var counter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            _ = counter.NextValue();
            return counter.NextValue() / 1024f;
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取网络上传速率 (KB/s) — 通过性能计数器
    /// </summary>
    public float? GetNetUpKBPerSec()
    {
        try
        {
            using var counter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", "_Total");
            _ = counter.NextValue();
            return counter.NextValue() / 1024f;
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取网络下载速率 (KB/s)
    /// </summary>
    public float? GetNetDownKBPerSec()
    {
        try
        {
            using var counter = new PerformanceCounter("Network Interface", "Bytes Received/sec", "_Total");
            _ = counter.NextValue();
            return counter.NextValue() / 1024f;
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取 CPU 频率 (MHz) — 通过性能计数器
    /// </summary>
    public float? GetCpuFreq()
    {
        try
        {
            using var freqCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            _ = freqCounter.NextValue();
            System.Threading.Thread.Sleep(100);
            var perf = freqCounter.NextValue();
            // 基准频率通常为 100 (即 100% = 基频)
            // 但这依赖于硬件，简化处理：读取 Win32_Processor 的 MaxClockSpeed
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT MaxClockSpeed FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                var maxMhz = (uint)obj["MaxClockSpeed"];
                return maxMhz * (perf / 100f);
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// 获取 CPU 温度 (°C) — 通过 WMI 热区 (ASUS 笔记本等适用)
    /// </summary>
    public float? GetCpuTemp()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            float? maxTemp = null;
            foreach (var obj in searcher.Get())
            {
                var raw = (uint)obj["CurrentTemperature"];
                var celsius = raw / 10f - 273.15f;
                if (!maxTemp.HasValue || celsius > maxTemp.Value)
                    maxTemp = celsius;
            }
            return maxTemp;
        }
        catch { return null; }
    }

    public (float? Load, float? UsedGB, float? TotalGB) GetMemoryData()
    {
        if (!_initialized) return (null, null, null);
        try
        {
            var availableMB = _memAvailableCounter?.NextValue() ?? 0;
            // Total physical memory via WMI
            var totalGB = GetTotalPhysicalMemoryGB();
            if (totalGB <= 0) return (null, null, null);
            var usedGB = totalGB - availableMB / 1024.0;
            var load = (float)(usedGB / totalGB * 100);
            return (load, (float)Math.Round(usedGB, 1), (float)Math.Round(totalGB, 1));
        }
        catch { return (null, null, null); }
    }

    private static double GetTotalPhysicalMemoryGB()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                var kb = (ulong)obj["TotalVisibleMemorySize"];
                return kb / (1024.0 * 1024.0);
            }
        }
        catch { }
        return 16.0; // fallback
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memAvailableCounter?.Dispose();
    }
}
