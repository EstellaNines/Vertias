using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 保存系统数据管理器
/// 负责管理保存数据的监控、统计和操作
/// </summary>
public class SaveSystemDataManager
{
    private static SaveSystemDataManager instance;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static SaveSystemDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SaveSystemDataManager();
            }
            return instance;
        }
    }

    // 保存操作统计
    private int saveOperationCount = 0;
    private int loadOperationCount = 0;
    private List<float> saveTimings = new List<float>();
    private List<float> loadTimings = new List<float>();
    private DateTime lastSaveTime = DateTime.MinValue;
    private DateTime lastLoadTime = DateTime.MinValue;

    // 事件定义
    public event System.Action<string> OnSaveStarted;
    public event System.Action<string, float> OnSaveCompleted;
    public event System.Action<string, string> OnSaveError;
    public event System.Action<string> OnLoadStarted;
    public event System.Action<string, float> OnLoadCompleted;
    public event System.Action<string, string> OnLoadError;
    public event System.Action OnSaveDataChanged;

    private SaveSystemDataManager()
    {
        // 从PlayerPrefs加载统计数据
        LoadStatistics();

        // 注册Unity编辑器事件
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~SaveSystemDataManager()
    {
        SaveStatistics();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    /// <summary>
    /// 记录保存操作开始
    /// </summary>
    /// <param name="filePath">保存文件路径</param>
    public void RecordSaveStart(string filePath)
    {
        OnSaveStarted?.Invoke(filePath);
    }

    /// <summary>
    /// 记录保存操作完成
    /// </summary>
    /// <param name="filePath">保存文件路径</param>
    /// <param name="duration">保存耗时（毫秒）</param>
    public void RecordSaveComplete(string filePath, float duration)
    {
        saveOperationCount++;
        saveTimings.Add(duration);
        lastSaveTime = DateTime.Now;

        // 保持最近100次记录
        if (saveTimings.Count > 100)
        {
            saveTimings.RemoveAt(0);
        }

        SaveStatistics();
        OnSaveCompleted?.Invoke(filePath, duration);
        OnSaveDataChanged?.Invoke();
    }

    /// <summary>
    /// 记录保存操作错误
    /// </summary>
    /// <param name="filePath">保存文件路径</param>
    /// <param name="error">错误信息</param>
    public void RecordSaveError(string filePath, string error)
    {
        OnSaveError?.Invoke(filePath, error);
        Debug.LogError($"保存文件失败 {filePath}: {error}");
    }

    /// <summary>
    /// 记录加载操作开始
    /// </summary>
    /// <param name="filePath">加载文件路径</param>
    public void RecordLoadStart(string filePath)
    {
        OnLoadStarted?.Invoke(filePath);
    }

    /// <summary>
    /// 记录加载操作完成
    /// </summary>
    /// <param name="filePath">加载文件路径</param>
    /// <param name="duration">加载耗时（毫秒）</param>
    public void RecordLoadComplete(string filePath, float duration)
    {
        loadOperationCount++;
        loadTimings.Add(duration);
        lastLoadTime = DateTime.Now;

        // 保持最近100次记录
        if (loadTimings.Count > 100)
        {
            loadTimings.RemoveAt(0);
        }

        SaveStatistics();
        OnLoadCompleted?.Invoke(filePath, duration);
        OnSaveDataChanged?.Invoke();
    }

    /// <summary>
    /// 记录加载操作错误
    /// </summary>
    /// <param name="filePath">加载文件路径</param>
    /// <param name="error">错误信息</param>
    public void RecordLoadError(string filePath, string error)
    {
        OnLoadError?.Invoke(filePath, error);
        Debug.LogError($"加载文件失败 {filePath}: {error}");
    }

    /// <summary>
    /// 获取保存操作统计
    /// </summary>
    public SaveSystemMonitorDisplay.SaveSystemStatistics GetStatistics()
    {
        var statistics = new SaveSystemMonitorDisplay.SaveSystemStatistics
        {
            saveOperationCount = saveOperationCount,
            loadOperationCount = loadOperationCount,
            lastSaveTime = lastSaveTime,
            lastLoadTime = lastLoadTime,
            averageSaveTime = saveTimings.Count > 0 ? saveTimings.Average() : 0f,
            averageLoadTime = loadTimings.Count > 0 ? loadTimings.Average() : 0f
        };

        // 扫描保存文件获取其他统计信息
        UpdateFileStatistics(statistics);

        return statistics;
    }

    /// <summary>
    /// 更新文件统计信息
    /// </summary>
    private void UpdateFileStatistics(SaveSystemMonitorDisplay.SaveSystemStatistics statistics)
    {
        string saveDirectory = GetSaveDirectory();
        if (!Directory.Exists(saveDirectory))
        {
            return;
        }

        try
        {
            string[] allFiles = Directory.GetFiles(saveDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSaveFile(f))
                .ToArray();

            statistics.totalSaveFiles = allFiles.Length;
            statistics.validSaveFiles = 0;
            statistics.corruptedSaveFiles = 0;
            statistics.totalSaveDataSize = 0;

            foreach (string file in allFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    statistics.totalSaveDataSize += fileInfo.Length;

                    if (ValidateSaveFile(file))
                    {
                        statistics.validSaveFiles++;
                    }
                    else
                    {
                        statistics.corruptedSaveFiles++;
                    }
                }
                catch
                {
                    statistics.corruptedSaveFiles++;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"更新文件统计信息时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否为保存文件
    /// </summary>
    private bool IsSaveFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".json" || extension == ".dat" || extension == ".save" || extension == ".xml";
    }

    /// <summary>
    /// 验证保存文件
    /// </summary>
    private bool ValidateSaveFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);

            if (filePath.EndsWith(".json"))
            {
                // 简单的JSON格式验证
                return !string.IsNullOrEmpty(content) &&
                       (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["));
            }

            return !string.IsNullOrEmpty(content);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取保存目录
    /// </summary>
    public string GetSaveDirectory()
    {
        // 优先使用项目中定义的保存路径
        string customSavePath = PlayerPrefs.GetString("CustomSavePath", "");
        if (!string.IsNullOrEmpty(customSavePath) && Directory.Exists(customSavePath))
        {
            return customSavePath;
        }

        // 默认使用Unity的持久化数据路径
        return Path.Combine(Application.persistentDataPath, "SaveData");
    }

    /// <summary>
    /// 设置自定义保存目录
    /// </summary>
    public void SetCustomSaveDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            PlayerPrefs.SetString("CustomSavePath", path);
            PlayerPrefs.Save();
            OnSaveDataChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"指定的保存目录不存在: {path}");
        }
    }

    /// <summary>
    /// 重置统计数据
    /// </summary>
    public void ResetStatistics()
    {
        saveOperationCount = 0;
        loadOperationCount = 0;
        saveTimings.Clear();
        loadTimings.Clear();
        lastSaveTime = DateTime.MinValue;
        lastLoadTime = DateTime.MinValue;

        SaveStatistics();
        OnSaveDataChanged?.Invoke();
    }

    /// <summary>
    /// 保存统计数据到PlayerPrefs
    /// </summary>
    private void SaveStatistics()
    {
        PlayerPrefs.SetInt("SaveOperationCount", saveOperationCount);
        PlayerPrefs.SetInt("LoadOperationCount", loadOperationCount);

        if (saveTimings.Count > 0)
        {
            PlayerPrefs.SetFloat("AverageSaveTime", saveTimings.Average());
        }

        if (loadTimings.Count > 0)
        {
            PlayerPrefs.SetFloat("AverageLoadTime", loadTimings.Average());
        }

        if (lastSaveTime != DateTime.MinValue)
        {
            PlayerPrefs.SetString("LastSaveTime", lastSaveTime.ToBinary().ToString());
        }

        if (lastLoadTime != DateTime.MinValue)
        {
            PlayerPrefs.SetString("LastLoadTime", lastLoadTime.ToBinary().ToString());
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从PlayerPrefs加载统计数据
    /// </summary>
    private void LoadStatistics()
    {
        saveOperationCount = PlayerPrefs.GetInt("SaveOperationCount", 0);
        loadOperationCount = PlayerPrefs.GetInt("LoadOperationCount", 0);

        // 加载时间记录
        string lastSaveTimeStr = PlayerPrefs.GetString("LastSaveTime", "");
        if (!string.IsNullOrEmpty(lastSaveTimeStr) && long.TryParse(lastSaveTimeStr, out long lastSaveBinary))
        {
            lastSaveTime = DateTime.FromBinary(lastSaveBinary);
        }

        string lastLoadTimeStr = PlayerPrefs.GetString("LastLoadTime", "");
        if (!string.IsNullOrEmpty(lastLoadTimeStr) && long.TryParse(lastLoadTimeStr, out long lastLoadBinary))
        {
            lastLoadTime = DateTime.FromBinary(lastLoadBinary);
        }
    }

    /// <summary>
    /// 播放模式状态改变处理
    /// </summary>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                // 进入播放模式时可以开始监控保存操作
                break;

            case PlayModeStateChange.ExitingPlayMode:
                // 退出播放模式时保存统计数据
                SaveStatistics();
                break;
        }
    }

    /// <summary>
    /// 创建保存目录（如果不存在）
    /// </summary>
    public void EnsureSaveDirectoryExists()
    {
        string saveDirectory = GetSaveDirectory();
        if (!Directory.Exists(saveDirectory))
        {
            try
            {
                Directory.CreateDirectory(saveDirectory);
                Debug.Log($"创建保存目录: {saveDirectory}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建保存目录失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取性能报告
    /// </summary>
    public string GetPerformanceReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 保存系统性能报告 ===");
        sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // 获取统计信息
        var statistics = GetStatistics();

        sb.AppendLine("=== 文件统计 ===");
        sb.AppendLine($"总文件数: {statistics.totalSaveFiles}");
        sb.AppendLine($"有效文件数: {statistics.validSaveFiles}");
        sb.AppendLine($"损坏文件数: {statistics.corruptedSaveFiles}");
        sb.AppendLine($"总数据大小: {FormatFileSize(statistics.totalSaveDataSize)}");
        sb.AppendLine();

        sb.AppendLine("=== 操作统计 ===");
        sb.AppendLine($"保存操作次数: {saveOperationCount}");
        sb.AppendLine($"加载操作次数: {loadOperationCount}");

        if (saveTimings.Count > 0)
        {
            sb.AppendLine($"平均保存时间: {saveTimings.Average():F2} ms");
            sb.AppendLine($"最快保存时间: {saveTimings.Min():F2} ms");
            sb.AppendLine($"最慢保存时间: {saveTimings.Max():F2} ms");
        }

        if (loadTimings.Count > 0)
        {
            sb.AppendLine($"平均加载时间: {loadTimings.Average():F2} ms");
            sb.AppendLine($"最快加载时间: {loadTimings.Min():F2} ms");
            sb.AppendLine($"最慢加载时间: {loadTimings.Max():F2} ms");
        }

        if (lastSaveTime != DateTime.MinValue)
        {
            sb.AppendLine($"最后保存时间: {lastSaveTime:yyyy-MM-dd HH:mm:ss}");
        }

        if (lastLoadTime != DateTime.MinValue)
        {
            sb.AppendLine($"最后加载时间: {lastLoadTime:yyyy-MM-dd HH:mm:ss}");
        }

        sb.AppendLine();
        sb.AppendLine("=== 文件详情 ===");

        var fileInfos = GetAllSaveFileInfos();
        foreach (var file in fileInfos)
        {
            sb.AppendLine($"文件: {file.fileName}");
            sb.AppendLine($"  路径: {file.filePath}");
            sb.AppendLine($"  大小: {FormatFileSize(file.fileSize)}");
            sb.AppendLine($"  修改时间: {file.lastModified:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  数据类型: {GetDataTypeDisplayName(file.dataType)}");
            sb.AppendLine($"  状态: {(file.isValid ? "有效" : "损坏")}");

            if (!string.IsNullOrEmpty(file.errorMessage))
            {
                sb.AppendLine($"  错误: {file.errorMessage}");
            }

            sb.AppendLine($"  校验和: {file.checksum}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    /// <summary>
    /// 获取数据类型显示名称
    /// </summary>
    private string GetDataTypeDisplayName(SaveDataType dataType)
    {
        switch (dataType)
        {
            case SaveDataType.PlayerData:
                return "玩家数据";
            case SaveDataType.InventoryData:
                return "物品数据";
            case SaveDataType.GameProgress:
                return "游戏进度";
            case SaveDataType.Settings:
                return "设置数据";
            default:
                return "未知类型";
        }
    }

    /// <summary>
    /// 保存数据类型枚举
    /// </summary>
    public enum SaveDataType
    {
        Unknown,
        PlayerData,
        InventoryData,
        GameProgress,
        Settings
    }

    /// <summary>
    /// 保存文件信息结构
    /// </summary>
    public struct SaveFileInfo
    {
        public string fileName;
        public string filePath;
        public DateTime lastModified;
        public long fileSize;
        public SaveDataType dataType;
        public bool isValid;
        public string checksum;
        public string errorMessage;
    }

    /// <summary>
    /// 验证结果结构
    /// </summary>
    public struct ValidationResult
    {
        public int validCount;
        public int invalidCount;
        public List<string> errors;
    }

    /// <summary>
    /// 获取所有保存文件信息
    /// </summary>
    public List<SaveFileInfo> GetAllSaveFileInfos()
    {
        var fileInfos = new List<SaveFileInfo>();
        string saveDirectory = GetSaveDirectory();

        if (!Directory.Exists(saveDirectory))
        {
            return fileInfos;
        }

        try
        {
            string[] allFiles = Directory.GetFiles(saveDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => IsSaveFile(f))
                .ToArray();

            foreach (string filePath in allFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var saveFileInfo = new SaveFileInfo
                    {
                        fileName = fileInfo.Name,
                        filePath = filePath,
                        lastModified = fileInfo.LastWriteTime,
                        fileSize = fileInfo.Length,
                        dataType = DetermineDataType(filePath),
                        isValid = ValidateSaveFile(filePath),
                        checksum = CalculateFileChecksum(filePath)
                    };

                    if (!saveFileInfo.isValid)
                    {
                        saveFileInfo.errorMessage = "文件格式无效或已损坏";
                    }

                    fileInfos.Add(saveFileInfo);
                }
                catch (Exception ex)
                {
                    var errorFileInfo = new SaveFileInfo
                    {
                        fileName = Path.GetFileName(filePath),
                        filePath = filePath,
                        lastModified = DateTime.MinValue,
                        fileSize = 0,
                        dataType = SaveDataType.Unknown,
                        isValid = false,
                        checksum = "",
                        errorMessage = $"读取文件失败: {ex.Message}"
                    };

                    fileInfos.Add(errorFileInfo);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"扫描保存文件时发生错误: {ex.Message}");
        }

        return fileInfos;
    }

    /// <summary>
    /// 确定数据类型
    /// </summary>
    private SaveDataType DetermineDataType(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();

        if (fileName.Contains("player") || fileName.Contains("character"))
            return SaveDataType.PlayerData;
        if (fileName.Contains("inventory") || fileName.Contains("item"))
            return SaveDataType.InventoryData;
        if (fileName.Contains("progress") || fileName.Contains("level") || fileName.Contains("quest"))
            return SaveDataType.GameProgress;
        if (fileName.Contains("setting") || fileName.Contains("config") || fileName.Contains("option"))
            return SaveDataType.Settings;

        return SaveDataType.Unknown;
    }

    /// <summary>
    /// 计算文件校验和
    /// </summary>
    private string CalculateFileChecksum(string filePath)
    {
        try
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        catch
        {
            return "无法计算";
        }
    }

    /// <summary>
    /// 验证所有保存文件
    /// </summary>
    public ValidationResult ValidateAllSaveFiles()
    {
        var result = new ValidationResult
        {
            validCount = 0,
            invalidCount = 0,
            errors = new List<string>()
        };

        var fileInfos = GetAllSaveFileInfos();

        foreach (var fileInfo in fileInfos)
        {
            if (fileInfo.isValid)
            {
                result.validCount++;
            }
            else
            {
                result.invalidCount++;
                if (!string.IsNullOrEmpty(fileInfo.errorMessage))
                {
                    result.errors.Add($"{fileInfo.fileName}: {fileInfo.errorMessage}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 清理损坏的文件
    /// </summary>
    public int CleanupCorruptedFiles(List<string> filePaths)
    {
        int deletedCount = 0;

        foreach (string filePath in filePaths)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    deletedCount++;
                    Debug.Log($"已删除损坏文件: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除文件失败 {filePath}: {ex.Message}");
            }
        }

        return deletedCount;
    }

    /// <summary>
    /// 备份保存数据
    /// </summary>
    public bool BackupSaveData(string backupPath)
    {
        try
        {
            string saveDirectory = GetSaveDirectory();
            if (!Directory.Exists(saveDirectory))
            {
                Debug.LogWarning("保存目录不存在，无法备份");
                return false;
            }

            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);
            }

            CopyDirectory(saveDirectory, backupPath);
            Debug.Log($"保存数据已备份到: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"备份保存数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 复制目录
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        // 复制文件
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // 递归复制子目录
        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// 导出统计报告
    /// </summary>
    public bool ExportStatisticsReport(string filePath)
    {
        try
        {
            string report = GetPerformanceReport();
            File.WriteAllText(filePath, report);
            Debug.Log($"统计报告已导出到: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"导出统计报告失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        SaveStatistics();

        // 清理事件订阅
        OnSaveStarted = null;
        OnSaveCompleted = null;
        OnSaveError = null;
        OnLoadStarted = null;
        OnLoadCompleted = null;
        OnLoadError = null;
        OnSaveDataChanged = null;
    }
}