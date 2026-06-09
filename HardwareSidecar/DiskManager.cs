using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class DiskManager : IDisposable
{
    public float? GetReadSpeed(IComputer computer) => GetThroughput(computer, "read");
    public float? GetWriteSpeed(IComputer computer) => GetThroughput(computer, "write");
    public float? GetTemperature(IComputer computer) => GetTemp(computer);

    private static float? GetThroughput(IComputer computer, string direction)
    {
        foreach (var hw in computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Storage) continue;
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Throughput && s.Value.HasValue)
                {
                    var name = s.Name.ToLowerInvariant();
                    if ((direction == "read" && name.Contains("read")) ||
                        (direction == "write" && name.Contains("write")))
                        return s.Value.Value / 1024f; // LHM B/s → KB/s
                }
            }
        }
        return null;
    }

    private static float? GetTemp(IComputer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Storage) continue;
            foreach (var s in hw.Sensors)
                if (s.SensorType == SensorType.Temperature && s.Value.HasValue)
                    return s.Value.Value;
        }
        return null;
    }

    public void Dispose() { }
}
