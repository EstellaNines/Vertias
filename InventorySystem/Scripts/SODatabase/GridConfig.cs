using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

// 保存系统配置
[System.Serializable]
public class SaveSystemConfig
{
    [Header("保存路径配置")]
    public string saveRootPath = "SaveData";           // 保存根目录
    public string inventorySavePath = "Inventory";     // 背包保存路径
    public string playerDataPath = "PlayerData";       // 玩家数据路径
    public string configBackupPath = "Backup";         // 配置备份路径

    [Header("文件格式配置")]
    public string saveFileExtension = ".json";         // 保存文件扩展名
    public bool useCompression = false;                // 是否使用压缩
    public bool useEncryption = false;                 // 是否使用加密

    [Header("备份配置")]
    public bool autoBackup = true;                     // 自动备份
    public int maxBackupCount = 5;                     // 最大备份数量
    public float backupInterval = 300f;                // 备份间隔（秒）

    // 获取完整保存路径
    public string GetFullSavePath(string subPath = "")
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveRootPath);
        if (!string.IsNullOrEmpty(subPath))
        {
            fullPath = Path.Combine(fullPath, subPath);
        }
        return fullPath;
    }

    // 获取背包保存路径
    public string GetInventorySavePath()
    {
        return GetFullSavePath(inventorySavePath);
    }

    // 获取玩家数据保存路径
    public string GetPlayerDataSavePath()
    {
        return GetFullSavePath(playerDataPath);
    }

    // 获取备份路径
    public string GetBackupPath()
    {
        return GetFullSavePath(configBackupPath);
    }
}

// 版本控制配置
[System.Serializable]
public class VersionControlConfig
{
    [Header("版本信息")]
    public int majorVersion = 1;                       // 主版本号
    public int minorVersion = 0;                       // 次版本号
    public int patchVersion = 0;                       // 补丁版本号
    public string buildDate = "";                       // 构建日期

    [Header("兼容性配置")]
    public List<string> compatibleVersions = new List<string>(); // 兼容的版本列表
    public bool strictVersionCheck = false;            // 严格版本检查
    public bool allowDowngrade = false;                // 允许降级

    public VersionControlConfig()
    {
        buildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 获取版本字符串
    public string GetVersionString()
    {
        return $"{majorVersion}.{minorVersion}.{patchVersion}";
    }

    // 检查版本兼容性
    public bool IsCompatibleWith(string version)
    {
        if (!strictVersionCheck) return true;

        return compatibleVersions.Contains(version) || version == GetVersionString();
    }

    // 比较版本
    public int CompareVersion(string otherVersion)
    {
        string[] parts = otherVersion.Split('.');
        if (parts.Length != 3) return -1;

        if (!int.TryParse(parts[0], out int otherMajor) ||
            !int.TryParse(parts[1], out int otherMinor) ||
            !int.TryParse(parts[2], out int otherPatch))
        {
            return -1;
        }

        if (majorVersion != otherMajor) return majorVersion.CompareTo(otherMajor);
        if (minorVersion != otherMinor) return minorVersion.CompareTo(otherMinor);
        return patchVersion.CompareTo(otherPatch);
    }
}

// 数据迁移配置
[System.Serializable]
public class DataMigrationConfig
{
    [Header("迁移配置")]
    public bool enableAutoMigration = true;            // 启用自动迁移
    public bool createBackupBeforeMigration = true;    // 迁移前创建备份
    public bool validateAfterMigration = true;         // 迁移后验证数据

    [Header("迁移规则")]
    public List<string> migrationScripts = new List<string>(); // 迁移脚本列表
    public bool skipFailedMigrations = false;          // 跳过失败的迁移
    public int maxMigrationRetries = 3;                // 最大重试次数

    [Header("数据清理")]
    public bool cleanupOldData = false;                // 清理旧数据
    public int dataRetentionDays = 30;                 // 数据保留天数
    public bool compressOldBackups = true;             // 压缩旧备份

    // 添加迁移脚本
    public void AddMigrationScript(string scriptName)
    {
        if (!migrationScripts.Contains(scriptName))
        {
            migrationScripts.Add(scriptName);
        }
    }

    // 移除迁移脚本
    public void RemoveMigrationScript(string scriptName)
    {
        migrationScripts.Remove(scriptName);
    }

    // 检查是否需要迁移
    public bool NeedsMigration(string fromVersion, string toVersion)
    {
        return enableAutoMigration && fromVersion != toVersion;
    }
}

[CreateAssetMenu(fileName = "GridConfig", menuName = "Inventory System/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 64f;
    public Vector2 spacing = new Vector2(2f, 2f);
    
    [Header("背包网格配置 - 向后兼容")]
    public int inventoryWidth = 10;     // 背包宽度（向后兼容）
    public int inventoryHeight = 12;    // 背包高度（向后兼容）

    [Header("Visual Settings")]
    public Color gridLineColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color invalidColor = Color.red;
    public float gridLineWidth = 1f;

    [Header("Interaction Settings")]
    public bool enableGridSnap = true;
    public bool showGridLines = true;
    public bool enableHighlight = true;

    [Header("扩展配置")]
    [SerializeField] private SaveSystemConfig saveSystemConfig = new SaveSystemConfig();
    [SerializeField] private VersionControlConfig versionControlConfig = new VersionControlConfig();
    [SerializeField] private DataMigrationConfig dataMigrationConfig = new DataMigrationConfig();

    [Header("配置元数据")]
    [SerializeField] private string configVersion = "1.0.0";
    [SerializeField] private string lastModified = "";
    [SerializeField] private bool isModified = false;

    // 属性访问器
    public SaveSystemConfig SaveSystemConfig => saveSystemConfig;
    public VersionControlConfig VersionControlConfig => versionControlConfig;
    public DataMigrationConfig DataMigrationConfig => dataMigrationConfig;
    public string ConfigVersion => configVersion;
    public string LastModified => lastModified;
    public bool IsModified => isModified;

    private void OnEnable()
    {
        // 初始化配置
        if (saveSystemConfig == null)
            saveSystemConfig = new SaveSystemConfig();
        if (versionControlConfig == null)
            versionControlConfig = new VersionControlConfig();
        if (dataMigrationConfig == null)
            dataMigrationConfig = new DataMigrationConfig();

        // 更新最后修改时间
        UpdateLastModified();
    }

    private void OnValidate()
    {
        // 标记为已修改
        MarkAsModified();

        // 验证配置数据
        ValidateConfiguration();
    }

    // 更新最后修改时间
    public void UpdateLastModified()
    {
        lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 标记为已修改
    public void MarkAsModified()
    {
        isModified = true;
        UpdateLastModified();
    }

    // 重置修改标记
    public void ResetModifiedFlag()
    {
        isModified = false;
    }

    // 更新配置版本
    public void UpdateConfigVersion(string newVersion)
    {
        configVersion = newVersion;
        MarkAsModified();
    }

    // 验证配置
    public bool ValidateConfiguration()
    {
        bool isValid = true;
        
        // 验证网格设置
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogWarning("GridConfig: 网格宽度和高度必须大于0");
            isValid = false;
        }
        
        // 验证背包网格设置（向后兼容）
        if (inventoryWidth <= 0 || inventoryHeight <= 0)
        {
            Debug.LogWarning("GridConfig: 背包网格宽度和高度必须大于0");
            isValid = false;
        }
        
        if (cellSize <= 0)
        {
            Debug.LogWarning("GridConfig: 单元格大小必须大于0");
            isValid = false;
        }

        // 验证保存系统配置
        if (saveSystemConfig != null)
        {
            if (string.IsNullOrEmpty(saveSystemConfig.saveRootPath))
            {
                Debug.LogWarning("GridConfig: 保存根路径不能为空");
                isValid = false;
            }

            if (saveSystemConfig.maxBackupCount < 0)
            {
                Debug.LogWarning("GridConfig: 最大备份数量不能为负数");
                isValid = false;
            }
        }

        return isValid;
    }

    // 重置为默认配置
    [ContextMenu("重置为默认配置")]
    public void ResetToDefault()
    {
        gridWidth = 10;
        gridHeight = 10;
        cellSize = 64f;
        spacing = new Vector2(2f, 2f);
        
        // 重置背包网格配置（向后兼容）
        inventoryWidth = 10;
        inventoryHeight = 12;
        
        gridLineColor = Color.white;
        highlightColor = Color.yellow;
        invalidColor = Color.red;
        gridLineWidth = 1f;
        
        enableGridSnap = true;
        showGridLines = true;
        enableHighlight = true;

        saveSystemConfig = new SaveSystemConfig();
        versionControlConfig = new VersionControlConfig();
        dataMigrationConfig = new DataMigrationConfig();

        configVersion = "1.0.0";
        MarkAsModified();

        Debug.Log("GridConfig: 已重置为默认配置");
    }

    // 导出配置信息
    public string ExportConfigInfo()
    {
        var info = new System.Text.StringBuilder();
        info.AppendLine($"=== GridConfig 配置信息 ===");
        info.AppendLine($"配置版本: {configVersion}");
        info.AppendLine($"最后修改: {lastModified}");
        info.AppendLine($"是否已修改: {isModified}");
        info.AppendLine();

        info.AppendLine($"网格设置:");
        info.AppendLine($"  尺寸: {gridWidth} x {gridHeight}");
        info.AppendLine($"  单元格大小: {cellSize}");
        info.AppendLine($"  间距: {spacing}");
        info.AppendLine();
        
        info.AppendLine($"背包网格设置（向后兼容）:");
        info.AppendLine($"  背包尺寸: {inventoryWidth} x {inventoryHeight}");
        info.AppendLine();

        info.AppendLine($"保存系统配置:");
        info.AppendLine($"  根路径: {saveSystemConfig.saveRootPath}");
        info.AppendLine($"  背包路径: {saveSystemConfig.inventorySavePath}");
        info.AppendLine($"  自动备份: {saveSystemConfig.autoBackup}");
        info.AppendLine($"  最大备份数: {saveSystemConfig.maxBackupCount}");
        info.AppendLine();

        info.AppendLine($"版本控制配置:");
        info.AppendLine($"  版本: {versionControlConfig.GetVersionString()}");
        info.AppendLine($"  构建日期: {versionControlConfig.buildDate}");
        info.AppendLine($"  严格版本检查: {versionControlConfig.strictVersionCheck}");
        info.AppendLine();

        info.AppendLine($"数据迁移配置:");
        info.AppendLine($"  启用自动迁移: {dataMigrationConfig.enableAutoMigration}");
        info.AppendLine($"  迁移前备份: {dataMigrationConfig.createBackupBeforeMigration}");
        info.AppendLine($"  清理旧数据: {dataMigrationConfig.cleanupOldData}");

        return info.ToString();
    }

    // 测试配置验证
    [ContextMenu("测试配置验证")]
    public void TestConfigValidation()
    {
        bool isValid = ValidateConfiguration();
        Debug.Log($"GridConfig: 配置验证结果 - {(isValid ? "通过" : "失败")}");
        Debug.Log(ExportConfigInfo());
    }

    // 测试路径生成
    [ContextMenu("测试路径生成")]
    public void TestPathGeneration()
    {
        Debug.Log($"GridConfig: 路径测试");
        Debug.Log($"完整保存路径: {saveSystemConfig.GetFullSavePath()}");
        Debug.Log($"背包保存路径: {saveSystemConfig.GetInventorySavePath()}");
        Debug.Log($"玩家数据路径: {saveSystemConfig.GetPlayerDataSavePath()}");
        Debug.Log($"备份路径: {saveSystemConfig.GetBackupPath()}");
    }

    public Vector2 GetCellSize()
    {
        return new Vector2(cellSize, cellSize);
    }

    public Vector2 GetGridSize()
    {
        return new Vector2(gridWidth * (cellSize + spacing.x) - spacing.x,
                          gridHeight * (cellSize + spacing.y) - spacing.y);
    }
}