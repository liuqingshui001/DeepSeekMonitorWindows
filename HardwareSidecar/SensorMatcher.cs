using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public static class SensorMatcher
{
    public static string? Match(IHardware hardware, ISensor sensor)
    {
        var hwType = hardware.HardwareType;
        var sensorType = sensor.SensorType;
        var name = (sensor.Name ?? "").ToLowerInvariant();

        return hwType switch
        {
            HardwareType.Cpu => MatchCpu(sensorType, name),
            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => MatchGpu(sensorType, name),
            HardwareType.Memory => MatchMemory(sensorType, name),
            HardwareType.Storage => MatchStorage(sensorType, name),
            HardwareType.Network => MatchNetwork(sensorType, name),
            HardwareType.Motherboard => MatchMotherboard(sensorType, name),
            HardwareType.SuperIO => MatchSuperIO(sensorType, name),
            HardwareType.Battery => MatchBattery(sensorType, name),
            _ => null
        };
    }

    private static string? MatchCpu(SensorType type, string name)
    {
        switch (type)
        {
            // CPU Load: accept any load sensor (cpu total, package, core max, etc)
            case SensorType.Load:
                if (name.Contains("total") || name.Contains("cpu")) return "CPU.Load";
                return null;
            // CPU Temp: accept package, core max, hottest core, or just "temperature"
            case SensorType.Temperature:
                if (name.Contains("package") || name.Contains("cpu package"))
                    return "CPU.Temp";
                if (name.Contains("core max") || name.Contains("hottest"))
                    return "CPU.Temp";
                if (name.Contains("cpu") && name.Contains("temp"))
                    return "CPU.Temp";
                if (name == "temperature" || name.Contains("cpu temperature"))
                    return "CPU.Temp";
                return null;
            // CPU Clock: any clock sensor
            case SensorType.Clock:
                if (name.Contains("core") || name.Contains("bus") || name.Contains("cpu") || name.Contains("clock"))
                    return "CPU.Clock";
                return null;
            // CPU Power: package power
            case SensorType.Power:
                if (name.Contains("package") || name.Contains("cpu"))
                    return "CPU.Power";
                return null;
            default:
                return null;
        }
    }

    private static string? MatchGpu(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Load when name.Contains("core"):
                return "GPU.Load";
            case SensorType.Temperature when name.Contains("core"):
                return "GPU.Temp";
            case SensorType.Clock when name.Contains("core"):
                return "GPU.Clock";
            case SensorType.Clock when name.Contains("memory"):
                return "GPU.MemClock";
            case SensorType.Power when name.Contains("package") || name.Contains("board"):
                return "GPU.Power";
            case SensorType.Load when name.Contains("memory") || name.Contains("mem"):
                return "GPU.MemoryUsed";
            case SensorType.Fan:
                return "GPU.Fan";
            default:
                return null;
        }
    }

    private static string? MatchMemory(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Load when name.Contains("memory") || name.Contains("ram") || name.Contains("mem"):
                return "MEM.Load";
            case SensorType.Data when name.Contains("used"):
                return "MEM.UsedGB";
            case SensorType.Data when name.Contains("available"):
                return null; // We use PerformanceCounter for memory
            default:
                return null;
        }
    }

    private static string? MatchStorage(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Load when name.Contains("used") || name.Contains("activity"):
                return "DISK.Load";
            case SensorType.Load when name.Contains("read"):
                return "DISK.Read";
            case SensorType.Load when name.Contains("write"):
                return "DISK.Write";
            case SensorType.Throughput when name.Contains("read"):
                return "DISK.Read";
            case SensorType.Throughput when name.Contains("write"):
                return "DISK.Write";
            case SensorType.Temperature:
                return "DISK.Temp";
            default:
                return null;
        }
    }

    private static string? MatchNetwork(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Throughput when name.Contains("up"):
                return "NET.Up";
            case SensorType.Throughput when name.Contains("down"):
                return "NET.Down";
            case SensorType.Data when name.Contains("upload") || name.Contains("sent"):
                return "NET.UpTotal";
            case SensorType.Data when name.Contains("download") || name.Contains("received"):
                return "NET.DownTotal";
            default:
                return null;
        }
    }

    private static string? MatchMotherboard(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Temperature:
                // Accept system/motherboard/mobo/mainboard temps
                return "MOBO.Temp";
            case SensorType.Fan:
                return "MOBO.Fan";
            default:
                return null;
        }
    }

    private static string? MatchSuperIO(SensorType type, string name)
    {
        // SuperIO often has CPU and system temperatures
        switch (type)
        {
            case SensorType.Temperature when name.Contains("system") || name.Contains("motherboard") || name.Contains("mobo"):
                return "MOBO.Temp";
            case SensorType.Temperature when name.Contains("cpu"):
                return "CPU.Temp";
            case SensorType.Fan when name.Contains("cpu"):
                return "MOBO.Fan";
            case SensorType.Fan:
                return "MOBO.Fan";
            default:
                return null;
        }
    }

    private static string? MatchBattery(SensorType type, string name)
    {
        switch (type)
        {
            case SensorType.Load when name.Contains("charge") || name.Contains("level"):
                return "BAT.Percent";
            case SensorType.Power:
                return "BAT.Power";
            case SensorType.Voltage:
                return "BAT.Voltage";
            case SensorType.Current:
                return "BAT.Current";
            default:
                return null;
        }
    }
}
