using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class DiskManager : IDisposable
{
    public float? GetReadSpeed(IComputer computer) => GetThroughput(computer, "read");
    public float? GetWriteSpeed(IComputer computer) => GetThroughput(computer, "write");
    public float? GetTemperature(IComputer computer) => GetTemp(computer);

    private static float? GetThroughput(IComputer computer, string direction)
    {
        float total = 0f;
        bool found = false;
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
                    {
                        total += s.Value.Value;
                        found = true;
                    }
                }
            }
        }
        return found ? total / 1024f : null; // LHM B/s (sum of all disks) → KB/s
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
