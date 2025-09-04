using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

[System.Serializable]
public class InventorySaveData
{
    // 多网格支持
    public List<GridSaveData> grids;         // 所有网格数据
    
    // 装备数据 - 重新定义为装备槽类型映射
    public Dictionary<string, ItemSaveData> equippedItems;  // key: EquipmentSlotType.ToString(), value: ItemSaveData
    
    // 装备系统元数据
    public EquipmentMetadata equipmentMetadata;
    
    // 统计信息
    public int totalItemCount;
    public Dictionary<int, int> categoryStats;
    
    // 构造函数 - 初始化所有集合字段
    public InventorySaveData()
    {
        grids = new List<GridSaveData>();
        equippedItems = new Dictionary<string, ItemSaveData>();
        equipmentMetadata = new EquipmentMetadata();
        categoryStats = new Dictionary<int, int>();
        totalItemCount = 0;
    }
}

/// <summary>
/// 装备系统元数据
/// 用于版本控制和数据验证
/// </summary>
[System.Serializable]
public class EquipmentMetadata
{
    public string lastEquipmentSaveTime;     // 最后装备保存时间
    public string equipmentDataVersion;      // 装备数据版本
    public int totalEquippedCount;           // 总装备数量
    public bool isEquipmentDataValid;        // 装备数据是否有效
    
    public EquipmentMetadata()
    {
        lastEquipmentSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        equipmentDataVersion = "2.0";
        totalEquippedCount = 0;
        isEquipmentDataValid = true;
    }
}