using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGridInventory : MonoBehaviour
{
    [Header("网格配置")]
    [FieldLabel("网格配置文件")] public GridConfig gridConfig;
    [FieldLabel("使用自定义尺寸")] public bool useCustomSize = false;
    
    // 自定义尺寸（当useCustomSize为true时使用）
    [FieldLabel("自定义宽度")] public int customWidth = 10;
    [FieldLabel("自定义高度")] public int customHeight = 12;
    [FieldLabel("自定义单元格大小")] public float customCellSize = 80f;

    // 获取组件
    RectTransform rectTransform;
    InventorySystemItem[,] Itemslot;
    
    // 属性访问器
    public int Width => useCustomSize ? customWidth : (gridConfig?.inventoryWidth ?? 10);
    public int Height => useCustomSize ? customHeight : (gridConfig?.inventoryHeight ?? 12);
    public float CellSize => useCustomSize ? customCellSize : (gridConfig?.cellSize ?? 80f);

    // 只保留基础的网格数据管理功能
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Resize(Width, Height);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 确保值在合理范围内
        customWidth = Mathf.Max(1, customWidth);
        customHeight = Mathf.Max(1, customHeight);
        customCellSize = Mathf.Max(1f, customCellSize);

        // 在编辑模式下延迟更新以避免SendMessage警告
        if (Application.isPlaying)
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                UpdateGridSize();
            }
        }
        else
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    if (rectTransform == null)
                        rectTransform = GetComponent<RectTransform>();

                    if (rectTransform != null)
                    {
                        UpdateGridSize();
                    }
                }
            };
        }
    }
#endif

    public void Resize(int newWidth, int newHeight)
    {
        Itemslot = new InventorySystemItem[newWidth, newHeight];
        UpdateGridSize();
    }

    private void UpdateGridSize()
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(Width * CellSize, Height * CellSize);
        }
    }

    // 只保留基础的数据访问方法
    public InventorySystemItem GetItem(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            return Itemslot[x, y];
        return null;
    }

    public void SetItem(int x, int y, InventorySystemItem item)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            Itemslot[x, y] = item;
    }

    // 提供只读属性
    public RectTransform RectTransform => rectTransform;
    
    // 安全的配置修改方法 - 移到类内部
    public bool TryUpdateGridConfig(int width, int height, float cellSize, bool forceUpdate = false)
    {
        // 验证参数
        if (width <= 0 || height <= 0 || cellSize <= 0)
        {
            Debug.LogWarning("无效的网格配置参数");
            return false;
        }
        
        // 检查是否需要更新
        if (!forceUpdate && gridConfig != null && 
            gridConfig.inventoryWidth == width && 
            gridConfig.inventoryHeight == height && 
            Mathf.Approximately(gridConfig.cellSize, cellSize))
        {
            return false; // 无需更新
        }
        
        // 执行更新
        if (gridConfig != null)
        {
            gridConfig.inventoryWidth = width;
            gridConfig.inventoryHeight = height;
            gridConfig.cellSize = cellSize;
            
            Resize(width, height);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gridConfig);
#endif
            
            Debug.Log($"网格配置已更新: {width}x{height}, 单元格大小: {cellSize}");
            return true;
        }
        
        return false;
    }
}
