using System;
using System.IO;

namespace ToirProf.Desktop.Services;

public static class ErrorLogService
{
    private static readonly object LockObj = new();

    public static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToirProf.Desktop",
        "errors.log");

    public static void Log(string operation, string errorType, int? statusCode, string message, string? details = null)
    {
        try
        {
            lock (LockObj)
            {
                var dir = Path.GetDirectoryName(LogFilePath)!;
                Directory.CreateDirectory(dir);
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{errorType}]" +
                            (statusCode.HasValue ? $" [HTTP {statusCode}]" : "") +
                            $" [{operation}] {message}" +
                            (string.IsNullOrWhiteSpace(details) ? "" : $" | Details: {details}") +
                            Environment.NewLine;
                File.AppendAllText(LogFilePath, entry);
            }
        }
        catch
        {
            // Не допускаем падения приложения при ошибке логирования
        }
    }

    public static string ReadRecent(int maxLines = 100)
    {
        try
        {
            if (!File.Exists(LogFilePath))
                return "Лог ошибок пока пуст.";

            var lines = File.ReadAllLines(LogFilePath);
            if (lines.Length <= maxLines)
                return string.Join(Environment.NewLine, lines);

            return string.Join(Environment.NewLine, lines[^maxLines..]);
        }
        catch (Exception ex)
        {
            return $"Ошибка чтения лога: {ex.Message}";
        }
    }
}
