using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class DamageTakenRecord
{
    public float TimeSeconds;
    public string Source;
    public int Amount;

    public DamageTakenRecord() { }

    public DamageTakenRecord(float timeSeconds, string source, int amount)
    {
        TimeSeconds = Mathf.Max(0f, timeSeconds);
        Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source;
        Amount = Mathf.Max(0, amount);
    }

    public string FormatTimestamp()
    {
        int minutes = (int)(TimeSeconds / 60f);
        int seconds = (int)(TimeSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}

/// <summary>
/// 單場遊戲紀錄資料。
/// </summary>
[Serializable]
public class GameRecord
{
    public float BattleTime;
    public int TotalDamage;
    public float Dps;
    public int Skill1Uses, Skill2Uses, Skill3Uses, Skill4Uses;
    public int Skill1Dmg, Skill2Dmg, Skill3Dmg;
    public bool IsVictory;
    public string DateStr;
    public List<DamageTakenRecord> DamageTaken = new();

    [NonSerialized]
    public string StorageFileName;
}

/// <summary>
/// 遊戲紀錄儲存：JSON 檔案，存於 persistentDataPath/Records/。
/// 每筆紀錄一個獨立 .json 檔，檔名為時間戳。
/// </summary>
public static class GameRecordStore
{
    static string RecordsDir => Path.Combine(Application.persistentDataPath, "Records");
    const int MAX_RECORDS = 50;

    /// <summary>確保資料夾存在</summary>
    static void EnsureDir()
    {
        if (!Directory.Exists(RecordsDir))
            Directory.CreateDirectory(RecordsDir);
    }

    /// <summary>儲存一筆紀錄，檔名為 yyyyMMdd_HHmmss_fff.json</summary>
    public static void Save(GameRecord record)
    {
        EnsureDir();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string fileName = $"{timestamp}.json";
        string path = Path.Combine(RecordsDir, fileName);
        record.StorageFileName = fileName;
        string json = JsonUtility.ToJson(record, true);
        File.WriteAllText(path, json);
        Debug.Log($"[GameRecordStore] 紀錄已儲存：{path}");

        // 超過上限時刪除最舊的
        PruneOldRecords();
    }

    /// <summary>讀取所有紀錄，依時間由新到舊排序</summary>
    public static List<GameRecord> Load()
    {
        EnsureDir();
        var result = new List<GameRecord>();
        var files = Directory.GetFiles(RecordsDir, "*.json");
        Array.Sort(files, (a, b) => string.Compare(b, a, StringComparison.Ordinal)); // 新到舊

        foreach (var f in files)
        {
            try
            {
                string json = File.ReadAllText(f);
                var record = JsonUtility.FromJson<GameRecord>(json);
                if (record != null)
                {
                    record.StorageFileName = Path.GetFileName(f);
                    record.DamageTaken ??= new List<DamageTakenRecord>();
                    result.Add(record);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameRecordStore] 讀取失敗：{f}\n{e.Message}");
            }
        }
        return result;
    }

    public static bool Delete(GameRecord record)
    {
        return DeleteFromDirectory(record, RecordsDir);
    }

    public static bool DeleteFromDirectory(GameRecord record, string recordsDirectory)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.StorageFileName) ||
            string.IsNullOrWhiteSpace(recordsDirectory))
            return false;

        string fileName = Path.GetFileName(record.StorageFileName);
        if (!string.Equals(fileName, record.StorageFileName, StringComparison.Ordinal) ||
            !string.Equals(Path.GetExtension(fileName), ".json", StringComparison.OrdinalIgnoreCase))
            return false;

        string directoryPath = Path.GetFullPath(recordsDirectory);
        string filePath = Path.GetFullPath(Path.Combine(directoryPath, fileName));
        string directoryPrefix = directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!filePath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
            return false;

        File.Delete(filePath);
        record.StorageFileName = null;
        return true;
    }

    /// <summary>刪除所有紀錄</summary>
    public static void Clear()
    {
        EnsureDir();
        foreach (var f in Directory.GetFiles(RecordsDir, "*.json"))
            File.Delete(f);
        Debug.Log("[GameRecordStore] 所有紀錄已清除。");
    }

    /// <summary>回傳紀錄資料夾路徑（供 UI 顯示）</summary>
    public static string GetRecordsPath() => RecordsDir;

    static void PruneOldRecords()
    {
        var files = Directory.GetFiles(RecordsDir, "*.json");
        if (files.Length <= MAX_RECORDS) return;
        Array.Sort(files); // 舊到新
        int toDelete = files.Length - MAX_RECORDS;
        for (int i = 0; i < toDelete; i++)
            File.Delete(files[i]);
    }
}
