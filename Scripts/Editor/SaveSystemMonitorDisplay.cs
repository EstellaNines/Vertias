using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 保存系统监控显示组件
/// 用于在Editor窗口中显示保存数据状态、最后修改时间、数据完整性等信息
/// </summary>
public static class SaveSystemMonitorDisplay
{
    /// <summary>
    /// 保存文件信息结构
    /// </summary>
    [System.Serializable]
    public class SaveFileInfo
    {
        public string fileName;           // 文件名
        public string filePath;          // 文件路径
        public DateTime lastModified;    // 最后修改时间
        public long fileSize;            // 文件大小（字节）
        public bool isValid;             // 文件是否有效
        public string checksum;          // 文件校验和
        public SaveDataType dataType;    // 保存数据类型
        public string errorMessage;      // 错误信息（如果有）
    }

    /// <summary>
    /// 保存数据类型枚举
    /// </summary>
    public enum SaveDataType
    {
        PlayerData,      // 玩家数据
        InventoryData,   // 背包数据
        GameProgress,    // 游戏进度
        Settings,        // 设置数据
        Unknown          // 未知类型
    }

    /// <summary>
    /// 保存系统统计信息
    /// </summary>
    [System.Serializable]
    public class SaveSystemStatistics
    {
        public int totalSaveFiles;           // 总保存文件数
        public int validSaveFiles;           // 有效保存文件数
        public int corruptedSaveFiles;       // 损坏的保存文件数
        public long totalSaveDataSize;       // 总保存数据大小
        public DateTime lastSaveTime;        // 最后保存时间
        public DateTime lastLoadTime;        // 最后加载时间
        public int saveOperationCount;       // 保存操作次数
        public int loadOperationCount;       // 加载操作次数
        public float averageSaveTime;        // 平均保存时间（毫秒）
        public float averageLoadTime;        // 平均加载时间（毫秒）
    }

    // 静态变量用于缓存数据
    private static List<SaveFileInfo> cachedSaveFiles = new List<SaveFileInfo>();
    private static SaveSystemStatistics cachedStatistics = new SaveSystemStatistics();
    private static DateTime lastRefreshTime = DateTime.MinValue;
    private static readonly float refreshInterval = 2f; // 刷新间隔（秒）

    // 与SaveSystemDataManager集成的接口
    private static SaveSystemDataManager dataManager;

    /// <summary>
    /// 初始化数据管理器
    /// </summary>
    private static void InitializeDataManager()
    {
        if (dataManager == null)
        {
            dataManager = SaveSystemDataManager.Instance;
        }
    }

    // GUI样式缓存
    private static GUIStyle headerStyle;
    private static GUIStyle subHeaderStyle;
    private static GUIStyle infoStyle;
    private static GUIStyle warningStyle;
    private static GUIStyle errorStyle;
    private static GUIStyle validStyle;

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private static void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.cyan }
            };
        }

        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };
        }

        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.yellow }
            };
        }

        if (errorStyle == null)
        {
            errorStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.red }
            };
        }

        if (validStyle == null)
        {
            validStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Color.green }
            };
        }
    }

    /// <summary>
    /// 绘制保存系统监控信息
    /// </summary>
    public static void DrawSaveSystemMonitor()
    {
        InitializeStyles();

        // 检查是否需要刷新数据
        if (DateTime.Now - lastRefreshTime > TimeSpan.FromSeconds(refreshInterval))
        {
            RefreshSaveSystemData();
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("保存系统监控", headerStyle);

        // 绘制统计信息
        DrawStatisticsSection();

        EditorGUILayout.Space();

        // 绘制保存文件列表
        DrawSaveFilesSection();

        EditorGUILayout.Space();

        // 绘制控制按钮
        DrawControlButtons();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制统计信息部分
    /// </summary>
    private static void DrawStatisticsSection()
    {
        EditorGUILayout.LabelField("系统统计", subHeaderStyle);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总文件数: {cachedStatistics.totalSaveFiles}", infoStyle, GUILayout.Width(120));
        EditorGUILayout.LabelField($"有效文件: {cachedStatistics.validSaveFiles}", validStyle, GUILayout.Width(120));
        EditorGUILayout.LabelField($"损坏文件: {cachedStatistics.corruptedSaveFiles}",
            cachedStatistics.corruptedSaveFiles > 0 ? errorStyle : infoStyle, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"总大小: {FormatFileSize(cachedStatistics.totalSaveDataSize)}", infoStyle, GUILayout.Width(150));
        EditorGUILayout.LabelField($"保存次数: {cachedStatistics.saveOperationCount}", infoStyle, GUILayout.Width(120));
        EditorGUILayout.LabelField($"加载次数: {cachedStatistics.loadOperationCount}", infoStyle, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        if (cachedStatistics.lastSaveTime != DateTime.MinValue)
        {
            EditorGUILayout.LabelField($"最后保存: {cachedStatistics.lastSaveTime:yyyy-MM-dd HH:mm:ss}", infoStyle);
        }

        if (cachedStatistics.lastLoadTime != DateTime.MinValue)
        {
            EditorGUILayout.LabelField($"最后加载: {cachedStatistics.lastLoadTime:yyyy-MM-dd HH:mm:ss}", infoStyle);
        }

        if (cachedStatistics.averageSaveTime > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"平均保存时间: {cachedStatistics.averageSaveTime:F2}ms", infoStyle, GUILayout.Width(180));
            EditorGUILayout.LabelField($"平均加载时间: {cachedStatistics.averageLoadTime:F2}ms", infoStyle);
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 绘制保存文件列表部分
    /// </summary>
    private static void DrawSaveFilesSection()
    {
        EditorGUILayout.LabelField("保存文件详情", subHeaderStyle);

        if (cachedSaveFiles.Count == 0)
        {
            EditorGUILayout.LabelField("未找到保存文件", warningStyle);
            return;
        }

        // 表头
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("文件名", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("类型", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("大小", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("修改时间", EditorStyles.boldLabel, GUILayout.Width(140));
        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // 分隔线
        EditorGUILayout.Space();

        // 文件列表
        foreach (var saveFile in cachedSaveFiles.Take(10)) // 限制显示前10个文件
        {
            EditorGUILayout.BeginHorizontal();

            // 文件名（可点击选择文件）
            if (GUILayout.Button(saveFile.fileName, EditorStyles.linkLabel, GUILayout.Width(150)))
            {
                EditorUtility.RevealInFinder(saveFile.filePath);
            }

            // 数据类型
            EditorGUILayout.LabelField(GetDataTypeDisplayName(saveFile.dataType), infoStyle, GUILayout.Width(80));

            // 文件大小
            EditorGUILayout.LabelField(FormatFileSize(saveFile.fileSize), infoStyle, GUILayout.Width(80));

            // 修改时间
            EditorGUILayout.LabelField(saveFile.lastModified.ToString("MM-dd HH:mm"), infoStyle, GUILayout.Width(140));

            // 状态指示
            GUIStyle statusStyle = saveFile.isValid ? validStyle : errorStyle;
            string statusText = saveFile.isValid ? "?" : "?";
            EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();

            // 显示错误信息（如果有）
            if (!saveFile.isValid && !string.IsNullOrEmpty(saveFile.errorMessage))
            {
                EditorGUILayout.LabelField($"  错误: {saveFile.errorMessage}", errorStyle);
            }
        }

        if (cachedSaveFiles.Count > 10)
        {
            EditorGUILayout.LabelField($"... 还有 {cachedSaveFiles.Count - 10} 个文件", infoStyle);
        }
    }

    /// <summary>
    /// 绘制控制按钮
    /// </summary>
    private static void DrawControlButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新数据", GUILayout.Width(80)))
        {
            RefreshSaveSystemData();
        }

        if (GUILayout.Button("打开保存目录", GUILayout.Width(100)))
        {
            string saveDirectory = GetSaveDirectory();
            if (Directory.Exists(saveDirectory))
            {
                EditorUtility.RevealInFinder(saveDirectory);
            }
            else
            {
                EditorUtility.DisplayDialog("目录不存在", $"保存目录不存在:\n{saveDirectory}", "确定");
            }
        }

        if (GUILayout.Button("验证所有文件", GUILayout.Width(100)))
        {
            ValidateAllSaveFiles();
        }

        if (GUILayout.Button("清理损坏文件", GUILayout.Width(100)))
        {
            CleanupCorruptedFiles();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("备份保存数据", GUILayout.Width(100)))
        {
            BackupSaveData();
        }

        if (GUILayout.Button("导出报告", GUILayout.Width(80)))
        {
            ExportSaveSystemReport();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 刷新保存系统数据
    /// </summary>
    private static void RefreshSaveSystemData()
    {
        InitializeDataManager();

        cachedSaveFiles.Clear();

        // 从SaveSystemDataManager获取文件信息
        var managerFileInfos = dataManager.GetAllSaveFileInfos();

        foreach (var info in managerFileInfos)
        {
            var saveFileInfo = new SaveFileInfo
            {
                fileName = info.fileName,
                filePath = info.filePath,
                lastModified = info.lastModified,
                fileSize = info.fileSize,
                dataType = ConvertDataType(info.dataType),
                isValid = info.isValid,
                checksum = info.checksum,
                errorMessage = info.errorMessage
            };

            cachedSaveFiles.Add(saveFileInfo);
        }

        // 按修改时间排序（最新的在前）
        cachedSaveFiles = cachedSaveFiles.OrderByDescending(f => f.lastModified).ToList();

        // 从数据管理器获取统计信息
        cachedStatistics = dataManager.GetStatistics();

        lastRefreshTime = DateTime.Now;
    }

    /// <summary>
    /// 转换数据类型
    /// </summary>
    private static SaveDataType ConvertDataType(SaveSystemDataManager.SaveDataType managerDataType)
    {
        switch (managerDataType)
        {
            case SaveSystemDataManager.SaveDataType.PlayerData:
                return SaveDataType.PlayerData;
            case SaveSystemDataManager.SaveDataType.InventoryData:
                return SaveDataType.InventoryData;
            case SaveSystemDataManager.SaveDataType.GameProgress:
                return SaveDataType.GameProgress;
            case SaveSystemDataManager.SaveDataType.Settings:
                return SaveDataType.Settings;
            default:
                return SaveDataType.Unknown;
        }
    }

    // ConvertStatistics方法已移除，因为SaveSystemDataManager.GetStatistics()直接返回正确的类型

    /// <summary>
    /// 获取保存目录路径
    /// </summary>
    private static string GetSaveDirectory()
    {
        InitializeDataManager();
        return dataManager.GetSaveDirectory();
    }

    /// <summary>
    /// 确定数据类型
    /// </summary>
    private static SaveDataType DetermineDataType(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();

        if (fileName.Contains("player") || fileName.Contains("character"))
            return SaveDataType.PlayerData;
        if (fileName.Contains("inventory") || fileName.Contains("backpack"))
            return SaveDataType.InventoryData;
        if (fileName.Contains("progress") || fileName.Contains("game"))
            return SaveDataType.GameProgress;
        if (fileName.Contains("setting") || fileName.Contains("config"))
            return SaveDataType.Settings;

        return SaveDataType.Unknown;
    }

    /// <summary>
    /// 验证保存文件
    /// </summary>
    private static bool ValidateSaveFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);

            // 基本的JSON格式验证
            if (filePath.EndsWith(".json"))
            {
                JsonUtility.FromJson<object>(content);
                return true;
            }

            // 其他格式的验证可以在这里添加
            return !string.IsNullOrEmpty(content);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 计算文件校验和
    /// </summary>
    private static string CalculateFileChecksum(string filePath)
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
            return "unknown";
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 获取数据类型显示名称
    /// </summary>
    private static string GetDataTypeDisplayName(SaveDataType dataType)
    {
        switch (dataType)
        {
            case SaveDataType.PlayerData: return "玩家";
            case SaveDataType.InventoryData: return "背包";
            case SaveDataType.GameProgress: return "进度";
            case SaveDataType.Settings: return "设置";
            default: return "未知";
        }
    }

    /// <summary>
    /// 验证所有保存文件
    /// </summary>
    private static void ValidateAllSaveFiles()
    {
        InitializeDataManager();

        var result = dataManager.ValidateAllSaveFiles();

        // 刷新数据以获取最新的验证结果
        RefreshSaveSystemData();

        string message = $"验证完成:\n有效文件: {result.validCount}\n无效文件: {result.invalidCount}";
        EditorUtility.DisplayDialog("文件验证结果", message, "确定");
    }

    /// <summary>
    /// 清理损坏的文件
    /// </summary>
    private static void CleanupCorruptedFiles()
    {
        InitializeDataManager();

        var corruptedFiles = cachedSaveFiles.Where(f => !f.isValid).ToList();

        if (corruptedFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("清理完成", "没有发现损坏的文件", "确定");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog("确认清理",
            $"将删除 {corruptedFiles.Count} 个损坏的文件，此操作不可撤销。\n\n确定要继续吗？",
            "确定", "取消");

        if (confirmed)
        {
            var filePaths = corruptedFiles.Select(f => f.filePath).ToList();
            int deletedCount = dataManager.CleanupCorruptedFiles(filePaths);

            RefreshSaveSystemData();
            EditorUtility.DisplayDialog("清理完成", $"已删除 {deletedCount} 个损坏的文件", "确定");
        }
    }

    /// <summary>
    /// 备份保存数据
    /// </summary>
    private static void BackupSaveData()
    {
        InitializeDataManager();

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupPath = EditorUtility.SaveFolderPanel("选择备份目录", "", $"SaveBackup_{timestamp}");

        if (!string.IsNullOrEmpty(backupPath))
        {
            try
            {
                bool success = dataManager.BackupSaveData(backupPath);
                if (success)
                {
                    EditorUtility.DisplayDialog("备份成功", $"保存数据已备份到:\n{backupPath}", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("备份失败", "备份过程中发生错误", "确定");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("备份失败", $"备份过程中发生错误:\n{ex.Message}", "确定");
            }
        }
    }

    /// <summary>
    /// 复制目录
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// 导出保存系统报告
    /// </summary>
    private static void ExportSaveSystemReport()
    {
        InitializeDataManager();

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"SaveSystemReport_{timestamp}.txt";
        string filePath = EditorUtility.SaveFilePanel("导出保存系统报告", "", fileName, "txt");

        if (!string.IsNullOrEmpty(filePath))
        {
            bool success = dataManager.ExportStatisticsReport(filePath);
            if (success)
            {
                EditorUtility.DisplayDialog("导出成功", $"保存系统报告已导出到:\n{filePath}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("导出失败", "导出报告时发生错误", "确定");
            }
        }
    }
}