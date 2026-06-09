using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly int _intervalMs;
    private readonly SensorMap _sensorMap = new();
    private readonly HardwareValueProvider _valueProvider = new();
    private readonly NetworkManager _networkManager = new();
    private readonly DiskManager _diskManager = new();
    private readonly ComponentProcessor _componentProcessor = new();
    private readonly PerformanceCounterManager _perfCounterManager = new();
    private long _sequence;

    public HardwareMonitorService(int intervalMs)
    {
        _intervalMs = intervalMs;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true
        };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
            _valueProvider.Initialize(_computer);
            _componentProcessor.Initialize(_computer);
            await _perfCounterManager.InitializeAsync();
            _sensorMap.Rebuild(_computer);

            // Debug: dump all sensors with their values
            var sb = new System.Text.StringBuilder();
            foreach (var hw in _computer.Hardware)
            {
                sb.Append($"{hw.HardwareType}/{hw.Name}[");
                foreach (var s in hw.Sensors)
                {
                    sb.Append($"{s.Name}={s.Value},");
                }
                sb.Append("] ");
            }
            OutputFormatter.WriteError("info", $"Debug sensors: {sb}");


            OutputFormatter.WriteError("info", "HardwareMonitorService started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var tickStart = Environment.TickCount64;
                    _valueProvider.OnUpdateTickStarted();
                    _computer.Accept(new UpdateVisitor());
                    _sensorMap.EnsureFresh(_computer);

                    var sensors = new Dictionary<string, double>();

                    // CPU — 使用 ComponentProcessor（LiteMonitor 方式）
                    AddIfNotNull(sensors, "CPU.Load", _valueProvider.GetValue("CPU.Load") ?? _perfCounterManager.GetCpuLoad());
                    AddIfNotNull(sensors, "CPU.Temp", _componentProcessor.GetCpuTemp() ?? _perfCounterManager.GetCpuTemp());
                    AddIfNotNull(sensors, "CPU.Clock", _componentProcessor.GetCpuClock() ?? _perfCounterManager.GetCpuFreq());
                    AddIfNotNull(sensors, "CPU.Power", _valueProvider.GetValue("CPU.Power"));

                    // GPU
                    AddIfNotNull(sensors, "GPU.Load", _valueProvider.GetValue("GPU.Load"));
                    AddIfNotNull(sensors, "GPU.Temp", _valueProvider.GetValue("GPU.Temp"));
                    AddIfNotNull(sensors, "GPU.Clock", _componentProcessor.GetGpuClock());
                    AddIfNotNull(sensors, "GPU.Power", _valueProvider.GetValue("GPU.Power"));
                    AddIfNotNull(sensors, "GPU.MemUsed", _valueProvider.GetValue("GPU.MemoryUsed"));

                    // Memory
                    var (memLoad, memUsed, _) = _perfCounterManager.GetMemoryData();
                    if (memLoad.HasValue)
                    {
                        sensors["MEM.Load"] = Math.Round(memLoad.Value, 1);
                        sensors["MEM.UsedGB"] = Math.Round(memUsed ?? 0, 1);
                    }
                    else
                    {
                        AddIfNotNull(sensors, "MEM.Load", _valueProvider.GetValue("MEM.Load"));
                        AddIfNotNull(sensors, "MEM.UsedGB", _valueProvider.GetValue("MEM.UsedGB"));
                    }

                    // Disk — LHM 优先，性能计数器备用（统一 KB/s）
                    AddIfNotNull(sensors, "DISK.Read", _diskManager.GetReadSpeed(_computer) ?? _perfCounterManager.GetDiskReadKBPerSec());
                    AddIfNotNull(sensors, "DISK.Write", _diskManager.GetWriteSpeed(_computer) ?? _perfCounterManager.GetDiskWriteKBPerSec());
                    AddIfNotNull(sensors, "DISK.Temp", _diskManager.GetTemperature(_computer));

                    // Network — LHM 优先，性能计数器备用（统一 KB/s）
                    AddIfNotNull(sensors, "NET.Up", _networkManager.GetUploadSpeed(_computer) ?? _perfCounterManager.GetNetUpKBPerSec());
                    AddIfNotNull(sensors, "NET.Down", _networkManager.GetDownloadSpeed(_computer) ?? _perfCounterManager.GetNetDownKBPerSec());

                    // 使用 OutputFormatter 输出
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var json = OutputFormatter.Serialize(timestamp, sensors, _sequence++);
                    Console.WriteLine(json);

                    // 动态重建传感器映射（每10分钟）
                    if (_sequence % (600000 / _intervalMs) == 0)
                    {
                        _sensorMap.Rebuild(_computer);
                    }

                    var elapsed = Environment.TickCount64 - tickStart;
                    var remaining = _intervalMs - (int)elapsed;
                    await Task.Delay(remaining > 0 ? remaining : 1, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    OutputFormatter.WriteError("error", ex.Message);
                    await Task.Delay(Math.Min(_intervalMs * 5, 30000), ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _computer.Close();
            FpsCounter.ForceKillZombies();
        }
    }

    private static void AddIfNotNull(Dictionary<string, double> dict, string key, float? value)
    {
        if (value.HasValue)
            dict[key] = Math.Round(value.Value, 1);
    }

    private static void AddIfPositive(Dictionary<string, double> dict, string key, float? value)
    {
        if (value.HasValue && value.Value > 0)
            dict[key] = Math.Round(value.Value, 1);
    }

    public void Dispose()
    {
        _computer.Close();
        _valueProvider.Dispose();
        _networkManager.Dispose();
        _diskManager.Dispose();
        _perfCounterManager.Dispose();
    }
}

internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);
    public void VisitHardware(IHardware hardware) { hardware.Update(); foreach (var sub in hardware.SubHardware) sub.Accept(this); }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}
