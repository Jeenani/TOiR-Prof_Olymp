using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public static class SettingsService
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ToirProf.Desktop");
    private static readonly string SettingsPath = Path.Combine(Folder, "settings.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static DesktopSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(SettingsPath), JsonOpts) ?? new();
        }
        catch (Exception ex) { ErrorLogService.Log("SettingsService.Load", "FileError", null, ex.Message, ex.ToString()); }
        return new();
    }

    public static void Save(DesktopSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOpts));
        }
        catch (Exception ex) { ErrorLogService.Log("SettingsService.Save", "FileError", null, ex.Message, ex.ToString()); }
    }
}

public static class ErrorLogService
{
    private static readonly object Sync = new();
    public static string LogFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ToirProf.Desktop", "errors.log");

    public static void Log(string operation, string errorType, int? statusCode, string message, string? details = null)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{errorType}]" +
                           (statusCode.HasValue ? $" [HTTP {statusCode}]" : "") +
                           $" [{operation}] {message}" +
                           (string.IsNullOrWhiteSpace(details) ? "" : $" | Details: {details}") + Environment.NewLine;
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch { }
    }

    public static string ReadRecent(int maxLines = 100)
    {
        try
        {
            if (!File.Exists(LogFilePath)) return "Лог ошибок пока пуст.";
            var lines = File.ReadAllLines(LogFilePath);
            return lines.Length <= maxLines ? string.Join(Environment.NewLine, lines) : string.Join(Environment.NewLine, lines[^maxLines..]);
        }
        catch (Exception ex) { return $"Ошибка чтения лога: {ex.Message}"; }
    }
}

public static class DraftQueueService
{
    private static readonly string DraftsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ToirProf.Desktop", "drafts.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static List<OfflineWorkOrderDraft> LoadAll()
    {
        try
        {
            if (File.Exists(DraftsPath))
                return JsonSerializer.Deserialize<List<OfflineWorkOrderDraft>>(File.ReadAllText(DraftsPath), JsonOpts) ?? new();
        }
        catch (Exception ex) { ErrorLogService.Log("DraftQueueService.LoadAll", "FileError", null, ex.Message, ex.ToString()); }
        return new();
    }

    public static void SaveAll(List<OfflineWorkOrderDraft> drafts)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DraftsPath)!);
            File.WriteAllText(DraftsPath, JsonSerializer.Serialize(drafts, JsonOpts));
        }
        catch (Exception ex) { ErrorLogService.Log("DraftQueueService.SaveAll", "FileError", null, ex.Message, ex.ToString()); }
    }

    public static void Enqueue(OfflineWorkOrderDraft draft) { var list = LoadAll(); list.Add(draft); SaveAll(list); }
    public static void Remove(string id) { var list = LoadAll(); list.RemoveAll(d => d.Id == id); SaveAll(list); }
}
