using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class HardwareValueProvider : IDisposable
{
    private readonly SensorMap _sensorMap = new();
    private IComputer? _computer;
    private Dictionary<string, float?> _tickCache = new();
    private bool _tickActive;

    public void Initialize(IComputer computer)
    {
        _computer = computer;
        _sensorMap.Rebuild(computer);
    }

    public void OnUpdateTickStarted()
    {
        _tickCache.Clear();
        _tickActive = true;
    }

    public float? GetValue(string key)
    {
        if (_tickActive && _tickCache.TryGetValue(key, out var cached))
            return cached;

        if (_computer == null) return null;
        _sensorMap.EnsureFresh(_computer);
        if (!_sensorMap.TryGetSensor(key, out var sensor) || sensor == null)
            return null;

        var result = sensor.Value;

        if (_tickActive)
            _tickCache[key] = result;

        return result;
    }

    public void Dispose() { _sensorMap.Clear(); }
}
