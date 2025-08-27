using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveData
{
    // 多网格支持
    public List<GridSaveData> grids;         // 所有网格数据
    
    // 装备数据
    public Dictionary<string, ItemSaveData> equippedItems;
    
    // 统计信息
    public int totalItemCount;
    public Dictionary<int, int> categoryStats;
    
    // 构造函数 - 初始化所有集合字段
    public InventorySaveData()
    {
        grids = new List<GridSaveData>();
        equippedItems = new Dictionary<string, ItemSaveData>();
        categoryStats = new Dictionary<int, int>();
        totalItemCount = 0;
    }
}