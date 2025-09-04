using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridSaveData
{
    // 网格基础信息
    public string gridName;                  // 网格名称标识
    public string gridGUID;                  // 网格唯一标识
    public int gridWidth;                    // 网格宽度
    public int gridHeight;                   // 网格高度

    // 网格扩展信息
    public GridType gridType;                // 网格类型
    public string gridDescription;           // 网格描述
    public GridFeatures gridFeatures;       // 网格特性
    public GridAccessLevel accessLevel;     // 访问级别
    public int gridPriority;                 // 网格优先级
    public float sortWeight;                 // 排序权重
    public bool isDefaultGrid;               // 是否为默认网格
    public bool isGridActive;                // 网格是否激活

    // 时间戳
    public string creationTime;              // 创建时间
    public string lastModifiedTime;          // 最后修改时间
    public string saveTime;                  // 保存时间

    // 物品数据
    public List<ItemSaveData> items;         // 网格中的所有物品

    // 默认构造函数
    public GridSaveData()
    {
        items = new List<ItemSaveData>();
        saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // 兼容性构造函数
    public GridSaveData(string name, int width, int height)
    {
        gridName = name;
        gridGUID = System.Guid.NewGuid().ToString();
        gridWidth = width;
        gridHeight = height;
        gridType = GridType.Other;
        gridDescription = "";
        gridFeatures = GridFeatures.None;
        accessLevel = GridAccessLevel.Public;
        gridPriority = 0;
        sortWeight = 0f;
        isDefaultGrid = false;
        isGridActive = true;

        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        creationTime = currentTime;
        lastModifiedTime = currentTime;
        saveTime = currentTime;

        items = new List<ItemSaveData>();
    }

    // 从ItemGrid创建的构造函数
    public GridSaveData(ItemGrid grid, string registrationKey)
    {
        if (grid == null)
        {
            Debug.LogError("[GridSaveData] ItemGrid为null，无法创建保存数据");
            gridName = "unknown";
            gridGUID = "unknown";
            gridWidth = 1;
            gridHeight = 1;
            gridType = GridType.Other;
            gridDescription = "";
            gridFeatures = GridFeatures.None;
            accessLevel = GridAccessLevel.Public;
            gridPriority = 0;
            sortWeight = 0f;
            isDefaultGrid = false;
            isGridActive = true;
            
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            creationTime = currentTime;
            lastModifiedTime = currentTime;
            saveTime = currentTime;
            
            items = new List<ItemSaveData>();
            return;
        }
        
        try
        {
            // 基础信息
            gridName = grid.GridName ?? "unknown";
            gridGUID = grid.GridGUID ?? System.Guid.NewGuid().ToString();
            gridWidth = grid.CurrentWidth;
            gridHeight = grid.CurrentHeight;

            // 扩展信息
            gridType = grid.GridType;
            gridDescription = grid.GridDescription ?? "";
            gridFeatures = grid.GridFeatures;
            accessLevel = grid.AccessLevel;
            gridPriority = grid.GridPriority;
            sortWeight = grid.SortWeight;
            isDefaultGrid = grid.IsDefaultGrid;
            isGridActive = grid.IsGridActive;

            // 时间戳
            creationTime = grid.CreationTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            lastModifiedTime = grid.LastModifiedTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 初始化物品列表
            items = new List<ItemSaveData>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GridSaveData] 创建网格保存数据时发生异常: {e.Message}");
            // 设置默认值
            gridName = "error";
            gridGUID = "error";
            gridWidth = 1;
            gridHeight = 1;
            gridType = GridType.Other;
            gridDescription = "";
            gridFeatures = GridFeatures.None;
            accessLevel = GridAccessLevel.Public;
            gridPriority = 0;
            sortWeight = 0f;
            isDefaultGrid = false;
            isGridActive = true;
            
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            creationTime = currentTime;
            lastModifiedTime = currentTime;
            saveTime = currentTime;
            
            items = new List<ItemSaveData>();
        }
    }

    // 获取统计信息
    public string GetStatistics()
    {
        return $"网格 {gridName} (GUID: {gridGUID}, 类型: {gridType}, 尺寸: {gridWidth}x{gridHeight}, 物品数量: {items.Count})";
    }
}