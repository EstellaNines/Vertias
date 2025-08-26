using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

// 物品组件 - 用于网格背包系统中的物品管理
public class Item : MonoBehaviour
{
    [Header("网格属性")]
    [SerializeField] private ItemGrid onGridReference;  // 当前所在的网格引用
    [SerializeField] private Vector2Int onGridPosition = Vector2Int.zero;  // 在网格中的位置
    [SerializeField] private bool isRotated = false;  // 是否旋转（90°）
    


    private ItemDataReader itemDataReader;  // 物品数据读取器引用
    private RectTransform rectTransform;  // RectTransform组件引用

    // 当前所在的网格引用
    public ItemGrid OnGridReference
    {
        get => onGridReference;
        set => onGridReference = value;
    }

    // 在网格中的位置
    public Vector2Int OnGridPosition
    {
        get => onGridPosition;
        set => onGridPosition = value;
    }

    // 物品数据读取器
    public ItemDataReader ItemDataReader => itemDataReader;

    // RectTransform组件
    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        // 获取组件引用
        itemDataReader = GetComponent<ItemDataReader>();
        rectTransform = GetComponent<RectTransform>();

        // 如果没有找到ItemDataReader组件，输出警告
        if (itemDataReader == null)
        {
            Debug.LogWarning($"物品 {gameObject.name} 缺少 ItemDataReader 组件！");
        }
    }

    // 获取物品宽度
    public int GetWidth()
    {
        if (itemDataReader?.ItemData == null) return 1;
        return isRotated ? itemDataReader.ItemData.height : itemDataReader.ItemData.width;
    }

    // 获取物品高度
    public int GetHeight()
    {
        if (itemDataReader?.ItemData == null) return 1;
        return isRotated ? itemDataReader.ItemData.width : itemDataReader.ItemData.height;
    }

    // 获取物品的原始尺寸
    public Vector2Int GetOriginalSize()
    {
        if (itemDataReader?.ItemData == null) return Vector2Int.one;

        return new Vector2Int(itemDataReader.ItemData.width, itemDataReader.ItemData.height);
    }

    // 获取物品当前尺寸
    public Vector2Int GetCurrentSize()
    {
        return new Vector2Int(GetWidth(), GetHeight());
    }

    // 是否已旋转
    public bool IsRotated()
    {
        return isRotated;
    }

    // 设置旋转状态
    public void SetRotated(bool rotated)
    {
        if (isRotated == rotated) return;
        isRotated = rotated;
    }

    // 切换旋转（90° 翻转宽高）
    public void ToggleRotation()
    {
        isRotated = !isRotated;
    }

    // 按网格尺寸调整可视大小与朝向（用于拖拽/放置后）
    public void AdjustVisualSizeForGrid()
    {
        if (rectTransform == null) return;
        Vector2 size = new Vector2(GetWidth() * ItemGrid.tileSizeWidth, GetHeight() * ItemGrid.tileSizeHeight);
        rectTransform.sizeDelta = size;
        
        // 旋转物品UI，但保持TMP文字组件不旋转且位置正确
        float rotationAngle = isRotated ? 90f : 0f;
        rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationAngle);
        
        // 查找TMP文字组件并调整其位置和旋转
        Transform textTransform = transform.Find("ItemText");
        if (textTransform != null)
        {
            RectTransform textRect = textTransform.GetComponent<RectTransform>();
            if (textRect != null)
            {
                // 反向旋转TMP文字组件，抵消父物体的旋转
                textTransform.localEulerAngles = new Vector3(0f, 0f, -rotationAngle);
                
                // 重新计算文字位置，确保始终在视觉右下角
                float itemWidth = size.x;
                float itemHeight = size.y;
                
                // 根据旋转状态调整文字位置，确保始终在视觉右下角
                Vector2 textPosition;
                if (isRotated)
                {
                    // 物品旋转90度时，由于物品坐标系也旋转了，需要重新计算位置
                    // 旋转后的视觉右下角在物品坐标系中的位置
                    textPosition = new Vector2(-itemHeight / 2f + 3f, -itemWidth / 2f + 3f);
                }
                else
                {
                    // 物品未旋转时，保持原始右下角位置
                    textPosition = new Vector2(itemWidth / 2f - 3f, -itemHeight / 2f + 3f);
                }
                
                textRect.anchoredPosition = textPosition;
            }
        }
    }

    // 重置物品状态（清除网格引用和位置）
    public void ResetGridState()
    {
        onGridReference = null;
        onGridPosition = Vector2Int.zero;
    }

    // 设置物品在网格中的状态
    public void SetGridState(ItemGrid grid, Vector2Int position)
    {
        onGridReference = grid;
        onGridPosition = position;
    }

    // 检查物品是否在网格中
    public bool IsOnGrid()
    {
        return onGridReference != null;
    }

    // 获取物品信息（用于调试）
    public string GetDebugInfo()
    {
        string info = $"物品: {gameObject.name}\n";
        info += $"网格位置: {onGridPosition}\n";
        info += $"当前尺寸: {GetWidth()}x{GetHeight()}\n";
        info += $"在网格中: {IsOnGrid()}\n";

        if (itemDataReader?.ItemData != null)
        {
            info += $"物品ID: {itemDataReader.ItemData.id}\n";
            info += $"物品名称: {itemDataReader.ItemData.GetDisplayName()}\n";
        }

        return info;
    }
}
