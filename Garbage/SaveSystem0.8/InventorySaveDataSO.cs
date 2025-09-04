// using System;
// using UnityEngine;
// using VInspector;

// // 背包保存数据配置资源
// // 用于管理背包保存系统的配置参数和设置
// [CreateAssetMenu(fileName = "InventorySaveData", menuName = "Inventory/Save Data Config", order = 1)]
// public class InventorySaveDataSO : ScriptableObject
// {
//     [Header("保存配置")]
//     [FieldLabel("保存键名")]
//     [SerializeField] private string saveKey = "InventoryData";
    
//     [FieldLabel("自动保存间隔(秒)")]
//     [SerializeField] private float autoSaveInterval = 15f;
    
//     [FieldLabel("启用自动保存")]
//     [SerializeField] private bool enableAutoSave = true;
    
//     [FieldLabel("启用调试日志")]
//     [SerializeField] private bool enableDebugLog = false;
    
//     [Header("数据验证")]
//     [FieldLabel("最大保存数据大小(KB)")]
//     [SerializeField] private int maxSaveDataSizeKB = 1024;
    
//     [FieldLabel("保存数据版本")]
//     [SerializeField] private string saveDataVersion = "1.0";
    
//     [Header("错误处理")]
//     [FieldLabel("最大重试次数")]
//     [SerializeField] private int maxRetryCount = 3;
    
//     [FieldLabel("重试间隔(秒)")]
//     [SerializeField] private float retryInterval = 1f;
    
//     [FieldLabel("启用备份保存")]
//     [SerializeField] private bool enableBackupSave = true;
    
//     // 保存键名属性
//     public string SaveKey => saveKey;
    
//     // 自动保存间隔属性
//     public float AutoSaveInterval => autoSaveInterval;
    
//     // 启用自动保存属性
//     public bool EnableAutoSave => enableAutoSave;
    
//     // 启用调试日志属性
//     public bool EnableDebugLog => enableDebugLog;
    
//     // 最大保存数据大小属性
//     public int MaxSaveDataSizeKB => maxSaveDataSizeKB;
    
//     // 保存数据版本属性
//     public string SaveDataVersion => saveDataVersion;
    
//     // 最大重试次数属性
//     public int MaxRetryCount => maxRetryCount;
    
//     // 重试间隔属性
//     public float RetryInterval => retryInterval;
    
//     // 启用备份保存属性
//     public bool EnableBackupSave => enableBackupSave;
    
//     // 获取备份保存键名
//     public string GetBackupSaveKey()
//     {
//         return saveKey + "_Backup";
//     }
    
//     // 验证配置参数是否有效
//     public bool ValidateConfig()
//     {
//         // 检查保存键名是否为空
//         if (string.IsNullOrEmpty(saveKey))
//         {
//             if (enableDebugLog)
//                 Debug.LogError("[InventorySaveDataSO] 保存键名不能为空");
//             return false;
//         }
        
//         // 检查自动保存间隔是否合理
//         if (enableAutoSave && autoSaveInterval <= 0)
//         {
//             if (enableDebugLog)
//                 Debug.LogError("[InventorySaveDataSO] 自动保存间隔必须大于0");
//             return false;
//         }
        
//         // 检查最大数据大小是否合理
//         if (maxSaveDataSizeKB <= 0)
//         {
//             if (enableDebugLog)
//                 Debug.LogError("[InventorySaveDataSO] 最大保存数据大小必须大于0");
//             return false;
//         }
        
//         // 检查重试配置是否合理
//         if (maxRetryCount < 0 || retryInterval < 0)
//         {
//             if (enableDebugLog)
//                 Debug.LogError("[InventorySaveDataSO] 重试配置参数不能为负数");
//             return false;
//         }
        
//         return true;
//     }
    
//     // 重置为默认配置
//     public void ResetToDefault()
//     {
//         saveKey = "InventoryData";
//         autoSaveInterval = 30f;
//         enableAutoSave = true;
//         enableDebugLog = false;
//         maxSaveDataSizeKB = 1024;
//         saveDataVersion = "1.0";
//         maxRetryCount = 3;
//         retryInterval = 1f;
//         enableBackupSave = true;
        
//         if (enableDebugLog)
//             Debug.Log("[InventorySaveDataSO] 配置已重置为默认值");
//     }
    
//     // Unity编辑器验证方法
//     private void OnValidate()
//     {
//         // 确保自动保存间隔不小于1秒
//         if (autoSaveInterval < 1f)
//             autoSaveInterval = 1f;
            
//         // 确保最大数据大小不小于1KB
//         if (maxSaveDataSizeKB < 1)
//             maxSaveDataSizeKB = 1;
            
//         // 确保重试次数不为负数
//         if (maxRetryCount < 0)
//             maxRetryCount = 0;
            
//         // 确保重试间隔不为负数
//         if (retryInterval < 0)
//             retryInterval = 0;
//     }
    
//     // 获取配置信息字符串(用于调试)
//     public override string ToString()
//     {
//         return $"InventorySaveDataSO [SaveKey: {saveKey}, AutoSave: {enableAutoSave}, Interval: {autoSaveInterval}s, Version: {saveDataVersion}]";
//     }
// }