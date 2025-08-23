using UnityEngine;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{
    [Header("背包基础配置")]
    [SerializeField] private int backpackWidth = 10; // 背包宽度（格子数）
    [SerializeField] private int backpackHeight = 6; // 背包高度（格子数）
    [SerializeField] private Transform itemContainer; // 物品容器
    
    [Header("背包物品列表")]
    private List<GameObject> backpackItems = new List<GameObject>(); // 背包中的物品列表

    private void Start()
    {
        InitializeBackpack();
    }

    private void InitializeBackpack()
    {
        // 初始化背包物品列表
        backpackItems = new List<GameObject>();
        
        // 如果没有设置物品容器，尝试找到子对象作为容器
        if (itemContainer == null)
        {
            itemContainer = transform;
        }
        
        Debug.Log($"背包系统初始化完成 - 容量: {backpackWidth}x{backpackHeight}");
    }

    // 获取背包容量
    public int GetBackpackCapacity()
    {
        return backpackWidth * backpackHeight;
    }
    
    // 获取当前物品数量
    public int GetCurrentItemCount()
    {
        return backpackItems.Count;
    }

    // 设置背包尺寸
    public void SetBackpackSize(int width, int height)
    {
        backpackWidth = width;
        backpackHeight = height;
        Debug.Log($"背包尺寸已更新为: {backpackWidth}x{backpackHeight}");
    }

    // 清空背包
    public void ClearBackpack()
    {
        // 销毁所有背包中的物品
        for (int i = backpackItems.Count - 1; i >= 0; i--)
        {
            if (backpackItems[i] != null)
            {
                Destroy(backpackItems[i]);
            }
        }
        
        // 清空物品列表
        backpackItems.Clear();
        Debug.Log("背包已清空");
    }

    // 添加物品到背包
    public bool AddItem(GameObject item)
    {
        if (GetCurrentItemCount() >= GetBackpackCapacity())
        {
            Debug.LogWarning("背包已满，无法添加更多物品");
            return false;
        }
        
        if (item != null)
        {
            // 将物品设置为容器的子对象
            item.transform.SetParent(itemContainer);
            backpackItems.Add(item);
            Debug.Log($"物品已添加到背包: {item.name}");
            return true;
        }
        
        return false;
    }
    
    // 从背包移除物品
    public bool RemoveItem(GameObject item)
    {
        if (backpackItems.Contains(item))
        {
            backpackItems.Remove(item);
            Debug.Log($"物品已从背包移除: {item.name}");
            return true;
        }
        
        return false;
    }
    
    // 获取背包中的所有物品
    public List<GameObject> GetAllItems()
    {
        return new List<GameObject>(backpackItems);
    }
    
    // 检查背包是否已满
    public bool IsBackpackFull()
    {
        return GetCurrentItemCount() >= GetBackpackCapacity();
    }
}