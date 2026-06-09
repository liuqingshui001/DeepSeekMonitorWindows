using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class SensorMap
{
    private Dictionary<string, ISensor> _map = new(StringComparer.OrdinalIgnoreCase);
    private long _lastRebuildTick;

    public void Rebuild(IComputer computer)
    {
        var newMap = new Dictionary<string, ISensor>(StringComparer.OrdinalIgnoreCase);
        foreach (var hardware in computer.Hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                var key = SensorMatcher.Match(hardware, sensor);
                if (key != null && !newMap.ContainsKey(key))
                    newMap[key] = sensor;
            }
        }
        _map = newMap;
        _lastRebuildTick = Environment.TickCount64;
    }

    public bool TryGetSensor(string key, out ISensor? sensor)
    {
        return _map.TryGetValue(key, out sensor);
    }

    public void EnsureFresh(IComputer computer)
    {
        var elapsed = Environment.TickCount64 - _lastRebuildTick;
        if (elapsed > 600_000) // 10分钟
            Rebuild(computer);
    }

    public void Clear() => _map.Clear();
}
