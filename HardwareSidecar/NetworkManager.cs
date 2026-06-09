using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class NetworkManager : IDisposable
{
    public float? GetUploadSpeed(IComputer computer) => GetMaxSpeed(computer, "upload");
    public float? GetDownloadSpeed(IComputer computer) => GetMaxSpeed(computer, "download");

    /// <summary>
    /// 取所有网卡中吞吐量最大的那个，返回 KB/s（LHM 原始值 B/s，除以 1024）
    /// </summary>
    private static float? GetMaxSpeed(IComputer computer, string direction)
    {
        float? maxSpeed = null;
        foreach (var hw in computer.Hardware)
        {
            if (hw.HardwareType != HardwareType.Network) continue;
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Throughput && s.Value.HasValue && s.Value.Value >= 0)
                {
                    var name = s.Name.ToLowerInvariant();
                    bool isMatch = direction == "upload"
                        ? (name.Contains("upload") || name.Contains("up") || name.Contains("sent"))
                        : (name.Contains("download") || name.Contains("down") || name.Contains("received"));

                    if (isMatch && (!maxSpeed.HasValue || s.Value.Value > maxSpeed.Value))
                        maxSpeed = s.Value.Value;
                }
            }
        }
        // LHM Throughput 单位是 B/s，统一转为 KB/s
        if (maxSpeed.HasValue)
            maxSpeed = maxSpeed.Value / 1024f;
        return maxSpeed;
    }

    public void Dispose() { }
}
