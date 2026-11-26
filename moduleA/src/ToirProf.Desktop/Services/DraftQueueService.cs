using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public static class DraftQueueService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToirProf.Desktop",
        "drafts.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static List<OfflineWorkOrderDraft> LoadAll()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<OfflineWorkOrderDraft>>(json, JsonOptions) ?? new();
            }
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("DraftQueueService.LoadAll", "FileError", null, ex.Message, ex.ToString());
        }
        return new();
    }

    public static void SaveAll(List<OfflineWorkOrderDraft> drafts)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(drafts, JsonOptions));
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("DraftQueueService.SaveAll", "FileError", null, ex.Message, ex.ToString());
        }
    }

    public static void Enqueue(OfflineWorkOrderDraft draft)
    {
        var list = LoadAll();
        list.Add(draft);
        SaveAll(list);
    }

    public static void Remove(string id)
    {
        var list = LoadAll();
        list.RemoveAll(d => d.Id == id);
        SaveAll(list);
    }
}
