using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridSaveData
{
    // 网格基础信息
    public string gridName;                  // 网格名称标识
    public int gridWidth;                    // 网格宽度
    public int gridHeight;                   // 网格高度
    
    // 物品数据
    public List<ItemSaveData> items;         // 网格中的所有物品
    
    // 时间戳
    public string saveTime;                  // 保存时间
    
    public GridSaveData()
    {
        items = new List<ItemSaveData>();
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public GridSaveData(string name, int width, int height)
    {
        gridName = name;
        gridWidth = width;
        gridHeight = height;
        items = new List<ItemSaveData>();
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}