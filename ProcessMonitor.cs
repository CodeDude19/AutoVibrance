using System.Diagnostics;

namespace AutoVibrance;

public class ProcessMonitor
{
    public bool IsProcessRunning(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        bool isRunning = processes.Length > 0;

        // Dispose process handles
        foreach (var process in processes)
        {
            process.Dispose();
        }

        return isRunning;
    }
}
