namespace HardwareSidecar;

public class FpsCounter : IDisposable
{
    public static readonly FpsCounter Current = new();
    private bool _disposed;

    /// <summary>
    /// 返回当前 FPS。简化实现，后续可对接 PresentMon ETW。
    /// </summary>
    public float? GetFps() => null;

    public static void ForceKillZombies() { }

    public void Dispose() { _disposed = true; }
}
