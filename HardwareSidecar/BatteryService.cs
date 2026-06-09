using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public static class BatteryService
{
    public static float? GetChargeLevel(IComputer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Battery) continue;
            foreach (var s in hw.Sensors)
                if (s.SensorType == SensorType.Load && s.Value.HasValue)
                    return s.Value.Value;
        }
        return null;
    }

    public static float? GetPower(IComputer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Battery) continue;
            foreach (var s in hw.Sensors)
                if (s.SensorType == SensorType.Power && s.Value.HasValue)
                    return s.Value.Value;
        }
        return null;
    }
}
