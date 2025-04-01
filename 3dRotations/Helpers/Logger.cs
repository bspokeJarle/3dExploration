using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;

public static class Logger
{
    public static bool EnableFileLogging = false;
    private static readonly string LogFilePath = @"C:\Temp\CrashDetectionLog.txt";

    private static readonly List<string> _logBuffer = new();
    private static readonly object _lock = new();
    private static readonly int _flushThreshold = 100;

    static Logger()
    {
        // Optional: flush on process exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Flush();
    }

    public static void Log(string message, string category = "General")
    {
        string logLine = $"{DateTime.Now:HH:mm:ss.fff} [{category}] {message}";

        if (!EnableFileLogging)
        {
            Debug.WriteLine(logLine);
            return;
        }

        lock (_lock)
        {
            _logBuffer.Add(logLine);

            if (_logBuffer.Count >= _flushThreshold)
            {
                Flush();
            }
        }
    }

    public static void Flush()
    {
        try
        {
            lock (_lock)
            {
                if (_logBuffer.Count == 0) return;

                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.AppendAllLines(LogFilePath, _logBuffer);
                _logBuffer.Clear();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logger Error] Failed to flush log: {ex.Message}");
        }
    }

    public static void ClearLog()
    {
        try
        {
            if (File.Exists(LogFilePath))
                File.WriteAllText(LogFilePath, "");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logger Error] Failed to clear log: {ex.Message}");
        }
    }
}
