using System.CommandLine;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace HardwareSidecar;

public class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;

    public static async Task<int> Main(string[] args)
    {
        // 隐藏控制台窗口（stdout 仍通过管道工作）
        ShowWindow(GetConsoleWindow(), SW_HIDE);

        var intervalArg = new Option<int>("--interval", () => 1000, "采集间隔(ms)");
        var root = new RootCommand("硬件传感器采集 Sidecar") { intervalArg };
        root.SetHandler(async (int intervalMs) =>
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            // 监听 stdin "exit\n" 命令
            _ = Task.Run(async () =>
            {
                try
                {
                    while (Console.ReadLine() is { } line)
                    {
                        if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            await cts.CancelAsync();
                            break;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // stdin 已关闭，正常退出
                }
            });

            var monitor = new HardwareMonitorService(intervalMs);
            await monitor.RunAsync(cts.Token);
        }, intervalArg);
        return await root.InvokeAsync(args);
    }
}
