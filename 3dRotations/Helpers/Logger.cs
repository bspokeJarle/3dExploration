using System;
using System.Diagnostics;
using System.IO;

public static class Logger
{
    public static bool EnableFileLogging = false;
    private static readonly string LogFilePath = @"C:\Temp\CrashDetectionLog.txt";

    public static void Log(string message, string category = "General")
    {
        if (!EnableFileLogging)
        {
            Debug.WriteLine($"[{category}] {message}");
            return;
        }

        try
        {
            Directory.CreateDirectory(@"C:\Temp"); // Ensure folder exists

            string logLine = $"{DateTime.Now:HH:mm:ss.fff} [{category}] {message}";
            File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Logger Error] {ex.Message}");
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
