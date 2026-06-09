using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

/// <summary>
/// CPU/GPU 复合数值处理器
/// 移植自 LiteMonitor 的 ComponentProcessor
/// </summary>
public class ComponentProcessor
{
    private IComputer? _computer;
    private float? _lastGoodCpuTemp;

    public void Initialize(IComputer computer) => _computer = computer;

    /// <summary>
    /// 获取 CPU 温度 — 遍历所有温度传感器取最高值
    /// 若无传感器，尝试 WMI 查询 Win32_Processor
    /// （与 LiteMonitor 一致：取 Package/Core Max/Hottest 等）
    /// </summary>
    public float? GetCpuTemp()
    {
        if (_computer != null)
        {
            float? maxTemp = null;
            foreach (var hw in _computer.Hardware)
            {
                if (hw.HardwareType != HardwareType.Cpu) continue;
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType != SensorType.Temperature || !s.Value.HasValue || s.Value.Value <= 0) continue;
                    var name = (s.Name ?? "").ToLowerInvariant();
                    if (name.Contains("distance")) continue; // Distance to TjMax is not actual temp
                    if (!maxTemp.HasValue || s.Value.Value > maxTemp.Value)
                        maxTemp = s.Value.Value;
                }
            }
            if (maxTemp.HasValue && maxTemp.Value > 0 && maxTemp.Value < 125)
            {
                _lastGoodCpuTemp = maxTemp;
                return maxTemp;
            }
        }

        // Fallback: WMI query
        var wmiTemp = GetCpuTempViaWmi();
        if (wmiTemp.HasValue && wmiTemp.Value > 0 && wmiTemp.Value < 125)
        {
            _lastGoodCpuTemp = wmiTemp;
            return wmiTemp;
        }

        return _lastGoodCpuTemp;
    }

    /// <summary>
    /// 通过 WMI Win32_Processor 获取 CPU 温度 (Windows 10+ kernel driver)
    /// 返回摄氏度值，若不可用返回 null
    /// </summary>
    private static float? GetCpuTempViaWmi()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                var raw = (uint)obj["CurrentTemperature"];
                // Convert from tenths of Kelvin to Celsius: (raw/10) - 273.15
                return raw / 10f - 273.15f;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// 获取 CPU 频率 — 取第一个有值的 Clock 传感器
    /// </summary>
    public float? GetCpuClock()
    {
        if (_computer == null) return null;
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Cpu) continue;
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Clock && s.Value.HasValue && s.Value.Value > 0)
                {
                    var name = (s.Name ?? "").ToLowerInvariant();
                    // 排除 memory/bus 等非核心时钟
                    if (!name.Contains("bus") && !name.Contains("memory") && !name.Contains("mem"))
                        return s.Value.Value;
                }
            }
        }
        return null;
    }

    public float? GetGpuClock()
    {
        if (_computer == null) return null;
        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            {
                foreach (var s in hw.Sensors)
                    if (s.SensorType == SensorType.Clock && s.Name.Contains("Core") && s.Value.HasValue)
                        return s.Value.Value;
            }
        }
        return null;
    }
}
