using System;
using System.IO;
using System.Text.Json;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public static class SettingsService
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToirProf.Desktop");

    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static DesktopSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<DesktopSettings>(json, JsonOptions) ?? new DesktopSettings();
            }
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("SettingsService.Load", "FileError", null, ex.Message, ex.ToString());
        }
        return new DesktopSettings();
    }

    public static void Save(DesktopSettings settings)
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("SettingsService.Save", "FileError", null, ex.Message, ex.ToString());
        }
    }
}
