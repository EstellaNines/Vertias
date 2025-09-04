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
    [SerializeField] private bool autoGridSizeAdjustment = true;  // 是否自动调整网格尺寸



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
        
        // 如果禁用了自动尺寸调整（比如在装备槽中），则跳过尺寸调整
        if (!autoGridSizeAdjustment)
        {
            Debug.Log($"[Item] 跳过物品 {name} 的自动尺寸调整（装备槽模式）");
            return;
        }
        
        Vector2 size = new Vector2(GetWidth() * ItemGrid.tileSizeWidth, GetHeight() * ItemGrid.tileSizeHeight);
        rectTransform.sizeDelta = size;
        
        // 调整所有子组件以适应网格尺寸
        AdjustChildrenForGridSize(size);

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
                
                // 使用通用方法恢复文本组件的尺寸和字体大小
                Vector2 itemSize = rectTransform.sizeDelta;
                textRect.sizeDelta = InventorySystem.ItemPrefabConstants.ItemTextDefaults.CalculateTextSize(itemSize);
                
                var textComponent = textRect.GetComponent<TMPro.TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.fontSize = InventorySystem.ItemPrefabConstants.ItemTextDefaults.CalculateFontSize(itemSize);
                }
            }
        }
        
        Debug.Log($"[Item] 物品 {name} 已调整为网格尺寸: {size}，旋转: {rotationAngle}度");
    }
    
    /// <summary>
    /// 调整子组件以适应网格尺寸
    /// </summary>
    /// <param name="gridSize">网格尺寸</param>
    private void AdjustChildrenForGridSize(Vector2 gridSize)
    {
        // 调整所有子组件的尺寸以匹配网格要求
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            Transform child = rectTransform.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            
            if (childRect != null)
            {
                string childName = child.name;
                
                if (childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemBackground || 
                    childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemIcon || 
                    childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemHighlight)
                {
                    // 背景、图标和高亮需要与主物品同步为网格尺寸
                    childRect.sizeDelta = gridSize;
                    Debug.Log($"[Item] 调整子组件 {childName} 为网格尺寸: {gridSize}");
                }
                // 文本组件在主方法中单独处理
            }
        }
    }

    // 重置物品状态（清除网格引用和位置）
    public void ResetGridState()
    {
        onGridReference = null;
        onGridPosition = Vector2Int.zero;
    }
    
    /// <summary>
    /// 设置是否自动调整网格尺寸
    /// </summary>
    /// <param name="enabled">是否启用自动调整</param>
    public void SetAutoGridSizeAdjustment(bool enabled)
    {
        autoGridSizeAdjustment = enabled;
        Debug.Log($"[Item] 物品 {name} 自动网格尺寸调整: {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取物品的真实视觉尺寸（基于原始图片）
    /// </summary>
    /// <returns>真实视觉尺寸</returns>
    public Vector2 GetRealVisualSize()
    {
        if (itemDataReader?.ItemData?.itemIcon != null)
        {
            Sprite itemSprite = itemDataReader.ItemData.itemIcon;
            return new Vector2(itemSprite.rect.width, itemSprite.rect.height);
        }
        
        // 如果没有图片数据，返回当前尺寸
        if (rectTransform != null)
        {
            return rectTransform.sizeDelta;
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// 恢复物品的真实视觉尺寸
    /// </summary>
    public void RestoreRealVisualSize()
    {
        if (rectTransform == null) return;
        
        Vector2 realSize = GetRealVisualSize();
        if (realSize != Vector2.zero)
        {
            // 恢复主物品尺寸
            rectTransform.sizeDelta = realSize;
            
            // 恢复所有子组件的尺寸
            RestoreChildrenRealSize(realSize);
            
            Debug.Log($"[Item] 恢复物品 {name} 真实尺寸: {realSize}");
        }
    }
    
    /// <summary>
    /// 恢复子组件的真实尺寸
    /// </summary>
    /// <param name="realSize">真实尺寸</param>
    private void RestoreChildrenRealSize(Vector2 realSize)
    {
        if (rectTransform == null) return;
        
        // 恢复所有子组件的原始尺寸
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            Transform child = rectTransform.GetChild(i);
            RectTransform childRect = child.GetComponent<RectTransform>();
            
            if (childRect != null)
            {
                string childName = child.name;
                
                if (childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemBackground || 
                    childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemIcon || 
                    childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemHighlight)
                {
                    // 背景、图标和高亮恢复为与主物品相同的尺寸
                    childRect.sizeDelta = realSize;
                }
                else if (childName == InventorySystem.ItemPrefabConstants.ChildNames.ItemText)
                {
                    // 使用通用方法恢复文本组件的位置和尺寸
                    childRect.anchoredPosition = InventorySystem.ItemPrefabConstants.ItemTextDefaults.CalculateTextPosition(realSize);
                    childRect.sizeDelta = InventorySystem.ItemPrefabConstants.ItemTextDefaults.CalculateTextSize(realSize);
                    
                    // 恢复字体大小
                    var textComponent = childRect.GetComponent<TMPro.TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.fontSize = InventorySystem.ItemPrefabConstants.ItemTextDefaults.CalculateFontSize(realSize);
                    }
                }
            }
        }
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
