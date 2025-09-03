using System;
using UnityEngine;

[System.Serializable]
public class ItemSaveData
{
    // 基础标识信息
    public string itemID;                    // 物品ID
    public string globalUniqueID;            // 全局唯一ID
    public int categoryID;                   // 物品类别ID
    
    // 网格位置信息
    public Vector2Int gridPosition;          // 在网格中的位置
    public int gridWidth;                    // 物品宽度
    public int gridHeight;                   // 物品高度
    
    // 运行时数据
    public int stackCount;                   // 堆叠数量
    public float durability;                 // 耐久度
    public int usageCount;                   // 使用次数
    public bool isEquipped;                  // 是否已装备
    
    // 注意：容器数据现在由 ContainerSaveManager 单独处理以避免循环引用
    // 不再在 ItemSaveData 中存储容器数据
    
    // 构造函数
    public ItemSaveData() { }
    
    public ItemSaveData(ItemDataReader itemReader, Vector2Int position)
    {
        // 检查参数有效性
        if (itemReader == null || itemReader.ItemData == null)
        {
            Debug.LogError("[ItemSaveData] ItemDataReader或ItemData为null，无法创建保存数据");
            // 初始化为默认值，避免null引用
            itemID = "unknown";
            globalUniqueID = "unknown";
            categoryID = 0;
            gridPosition = Vector2Int.zero;
            gridWidth = 1;
            gridHeight = 1;
            stackCount = 0;
            durability = 0f;
            usageCount = 0;
            isEquipped = false;

            return;
        }
        
        var itemData = itemReader.ItemData;
        
        // 基础信息
        itemID = itemData.id.ToString(); // 转换为字符串
        globalUniqueID = itemData.GlobalId.ToString(); // 使用GlobalId属性
        categoryID = (int)itemData.category;
        
        // 位置信息
        gridPosition = position;
        gridWidth = itemData.width;
        gridHeight = itemData.height;
        
        // 运行时数据 - 使用ItemDataReader的属性
        stackCount = itemReader.CurrentStack;
        durability = itemReader.CurrentDurability;
        usageCount = itemReader.CurrentUsageCount;
        isEquipped = false; // 网格中的物品默认未装备
        
        // 注意：容器数据由ContainerSaveManager单独处理以避免循环引用
    }
}