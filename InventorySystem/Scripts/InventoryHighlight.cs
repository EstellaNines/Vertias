// ==========================================================================
// 物品放置高亮指示器
// 用于在物品放置时显示高亮：绿色表示可放置，红色表示不可放置
// ==========================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour
{
    [Header("高亮组件")]
    [SerializeField] private RectTransform highlighter;   // 高亮框 RectTransform
    [SerializeField] private Image highlightImage;        // 高亮框 Image 组件

    [Header("颜色设置")]
    [SerializeField] private Color canPlaceColor = new Color(0f, 1f, 0f, 0.5f); // 可放置颜色（绿色）
    [SerializeField] private Color cannotPlaceColor = new Color(1f, 0f, 0f, 0.5f); // 不可放置颜色（红色）

    [Header("自动保护设置")]
    [SerializeField] private bool enableAutoProtection = true; // 启用自动保护机制
    [SerializeField] private float parentCheckInterval = 0.1f; // 父级检查间隔（秒）

    // 自动保护相关字段
    private Transform safeParent;                           // 安全的父级对象
    private Transform currentGridParent;                    // 当前网格父级
    private bool isMonitoringParent = false;                // 是否正在监控父级
    private Coroutine parentMonitorCoroutine;               // 父级监控协程

    private void Awake()
    {
        // 若未指定组件，则自动获取当前对象上的组件
        if (highlighter == null)
            highlighter = GetComponent<RectTransform>();

        if (highlightImage == null)
            highlightImage = GetComponent<Image>();

        // 初始化安全父级
        InitializeSafeParent();

        // 初始时隐藏高亮
        Show(false);
    }

    #region 尺寸与位置
    /// <summary>根据物品设置高亮尺寸</summary>
    public void SetSize(Item targetItem)
    {
        if (highlighter == null || targetItem == null) return;

        Vector2 size = new Vector2
        {
            x = targetItem.GetWidth() * ItemGrid.tileSizeWidth,
            y = targetItem.GetHeight() * ItemGrid.tileSizeHeight
        };
        highlighter.sizeDelta = size;
    }

    /// <summary>直接以宽高设置高亮尺寸</summary>
    public void SetSize(int width, int height)
    {
        if (highlighter == null) return;

        Vector2 size = new Vector2
        {
            x = width * ItemGrid.tileSizeWidth,
            y = height * ItemGrid.tileSizeHeight
        };
        highlighter.sizeDelta = size;
    }

    /// <summary>设置高亮位置，使用物品当前网格位置</summary>
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(
            targetItem,
            targetItem.OnGridPosition.x,
            targetItem.OnGridPosition.y);
        highlighter.localPosition = pos;
    }

    /// <summary>设置高亮位置（指定格子坐标）</summary>
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null) return;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;
    }

    /// <summary>简化版本的位置设置，与 ItemGrid 计算规则一致</summary>
    public void SetPositionSimple(ItemGrid targetGrid, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null) return;

        Vector2 highlightSize = highlighter.sizeDelta;

        float leftTopX = posX * ItemGrid.tileSizeWidth;
        float leftTopY = posY * ItemGrid.tileSizeHeight;

        Vector2 position = new Vector2
        {
            x = leftTopX + highlightSize.x * 0.5f,
            y = -(leftTopY + highlightSize.y * 0.5f)
        };
        highlighter.localPosition = position;
    }
    #endregion

    #region 显示控制
    /// <summary>显示或隐藏高亮</summary>
    public void Show(bool show)
    {
        if (highlighter != null)
        {
            highlighter.gameObject.SetActive(show);
            
            // 当隐藏高亮时，自动返回到安全父级
            if (!show && enableAutoProtection)
            {
                ReturnToSafeParentOnHide();
            }
        }
    }

    /// <summary>获取高亮是否显示</summary>
    public bool IsShowing() =>
        highlighter != null && highlighter.gameObject.activeInHierarchy;
    #endregion

    #region 父级与颜色
    /// <summary>设置高亮的父级</summary>
    public void SetParent(ItemGrid targetGrid)
    {
        if (highlighter == null || targetGrid == null) return;
        
        Transform gridTransform = targetGrid.GetComponent<RectTransform>();
        highlighter.SetParent(gridTransform, false);
        
        // 更新当前网格父级并开始监控
        UpdateGridParent(gridTransform);
    }

    /// <summary>设置高亮颜色（可放置/不可放置）</summary>
    public void SetCanPlace(bool canPlace)
    {
        if (highlightImage == null) return;
        highlightImage.color = canPlace ? canPlaceColor : cannotPlaceColor;
    }

    /// <summary>设置自定义颜色</summary>
    public void SetColor(Color color)
    {
        if (highlightImage == null) return;
        highlightImage.color = color;
    }

    /// <summary>重置高亮为默认状态</summary>
    public void Reset()
    {
        Show(false);
        if (highlightImage != null)
            highlightImage.color = canPlaceColor;
            
        // 重置时确保返回到安全父级
        if (enableAutoProtection)
        {
            EnsureInSafeParent();
        }
    }
    #endregion

    #region 装备槽专用
    /// <summary>为装备槽设置高亮</summary>
    /// <param name="equipmentSlot">目标装备槽</param>
    /// <param name="canEquip">是否可以装备</param>
    public void SetEquipmentSlotHighlight(InventorySystem.EquipmentSlot equipmentSlot, bool canEquip)
    {
        if (highlighter == null || equipmentSlot == null) return;

        // 设置父级为装备槽
        highlighter.SetParent(equipmentSlot.transform);

        // 设置高亮尺寸为装备槽尺寸
        RectTransform slotRect = equipmentSlot.GetComponent<RectTransform>();
        if (slotRect != null)
            highlighter.sizeDelta = slotRect.sizeDelta;

        // 设置高亮位置为装备槽中心
        highlighter.localPosition = Vector3.zero;
        highlighter.anchorMin = new Vector2(0.5f, 0.5f);
        highlighter.anchorMax = new Vector2(0.5f, 0.5f);
        highlighter.pivot = new Vector2(0.5f, 0.5f);

        // 设置高亮颜色并显示
        SetCanPlace(canEquip);
        Show(true);
    }
    #endregion

    #region 冲突提示
    /// <summary>设置重叠冲突效果</summary>
    public void SetOverlapWarning(bool hasOverlap)
    {
        if (highlightImage == null) return;
        highlightImage.color = hasOverlap ? cannotPlaceColor : canPlaceColor;
    }

    /// <summary>设置重叠冲突效果（带闪烁）</summary>
    public void SetOverlapWarningWithBlink(bool hasOverlap)
    {
        if (highlightImage == null) return;

        StopAllCoroutines();

        if (hasOverlap)
            StartCoroutine(BlinkWarning());
        else
            highlightImage.color = canPlaceColor;
    }

    /// <summary>闪烁提示的协程</summary>
    private IEnumerator BlinkWarning()
    {
        float blinkInterval = 0.3f; // 闪烁间隔
        Color transparentRed = new Color(
            cannotPlaceColor.r,
            cannotPlaceColor.g,
            cannotPlaceColor.b,
            0.2f);

        while (true)
        {
            highlightImage.color = cannotPlaceColor;   // 纯红色
            yield return new WaitForSeconds(blinkInterval);

            highlightImage.color = transparentRed;     // 半透明红色
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    #endregion

    #region 自动保护机制
    /// <summary>初始化安全父级</summary>
    private void InitializeSafeParent()
    {
        // 查找InventoryController作为安全父级
        InventoryController inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController != null)
        {
            safeParent = inventoryController.transform;
            Debug.Log($"InventoryHighlight: 找到安全父级 - {safeParent.name}");
        }
        else
        {
            // 如果找不到InventoryController，使用Canvas作为安全父级
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                safeParent = canvas.transform;
                Debug.Log($"InventoryHighlight: 使用Canvas作为安全父级 - {safeParent.name}");
            }
            else
            {
                // 最后备选：使用当前父级
                safeParent = transform.parent;
                Debug.LogWarning("InventoryHighlight: 使用当前父级作为安全父级");
            }
        }
    }

    /// <summary>更新网格父级并开始监控</summary>
    private void UpdateGridParent(Transform newGridParent)
    {
        // 如果新的父级是网格，开始监控
        if (newGridParent != null && newGridParent.GetComponent<ItemGrid>() != null)
        {
            currentGridParent = newGridParent;
            StartParentMonitoring();
        }
        else
        {
            // 如果不是网格父级，停止监控
            StopParentMonitoring();
            currentGridParent = null;
        }
    }

    /// <summary>开始父级监控</summary>
    private void StartParentMonitoring()
    {
        if (!enableAutoProtection) return;

        // 停止之前的监控
        StopParentMonitoring();

        isMonitoringParent = true;
        parentMonitorCoroutine = StartCoroutine(MonitorParentCoroutine());
    }

    /// <summary>停止父级监控</summary>
    private void StopParentMonitoring()
    {
        isMonitoringParent = false;
        
        if (parentMonitorCoroutine != null)
        {
            StopCoroutine(parentMonitorCoroutine);
            parentMonitorCoroutine = null;
        }
    }

    /// <summary>父级监控协程</summary>
    private IEnumerator MonitorParentCoroutine()
    {
        while (isMonitoringParent && currentGridParent != null)
        {
            // 检查父级网格是否仍然存在
            if (currentGridParent == null || currentGridParent.gameObject == null)
            {
                // 父级已被销毁，执行自动保护
                ExecuteAutoProtection("父级网格已被销毁");
                yield break;
            }

            // 检查父级网格是否即将被销毁（非活跃状态可能表示即将销毁）
            if (!currentGridParent.gameObject.activeInHierarchy)
            {
                // 父级网格已变为非活跃状态，可能即将销毁
                ExecuteAutoProtection("父级网格已变为非活跃状态");
                yield break;
            }

            // 等待下一次检查
            yield return new WaitForSeconds(parentCheckInterval);
        }
    }

    /// <summary>执行自动保护</summary>
    private void ExecuteAutoProtection(string reason)
    {
        Debug.Log($"InventoryHighlight: 触发自动保护 - {reason}");

        // 停止监控
        StopParentMonitoring();

        // 隐藏高亮
        Show(false);

        // 返回到安全父级
        ReturnToSafeParent();

        // 重置状态
        Reset();
    }

    /// <summary>返回到安全父级</summary>
    private void ReturnToSafeParent()
    {
        if (highlighter == null || safeParent == null) return;

        try
        {
            highlighter.SetParent(safeParent, false);
            StopParentMonitoring(); // 停止监控
            currentGridParent = null;
            Debug.Log($"InventoryHighlight: 已返回到安全父级 - {safeParent.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InventoryHighlight: 返回安全父级时发生错误 - {e.Message}");
        }
    }

    /// <summary>隐藏时返回到安全父级（温和版本）</summary>
    private void ReturnToSafeParentOnHide()
    {
        // 只在当前父级是网格时才返回
        if (highlighter == null || safeParent == null) return;
        
        // 检查当前是否在网格下
        if (currentGridParent != null && highlighter.parent == currentGridParent)
        {
            try
            {
                highlighter.SetParent(safeParent, false);
                StopParentMonitoring(); // 停止监控
                currentGridParent = null;
                Debug.Log($"InventoryHighlight: 隐藏时返回到安全父级 - {safeParent.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventoryHighlight: 隐藏时返回安全父级发生错误 - {e.Message}");
            }
        }
    }

    /// <summary>手动触发自动保护（供外部调用）</summary>
    public void TriggerAutoProtection()
    {
        if (enableAutoProtection)
        {
            ExecuteAutoProtection("手动触发");
        }
    }

    /// <summary>强制返回到安全父级（供外部调用）</summary>
    public void ForceReturnToSafeParent()
    {
        if (enableAutoProtection)
        {
            Show(false); // 先隐藏
            EnsureInSafeParent(); // 然后确保在安全父级
            Debug.Log("InventoryHighlight: 外部强制返回到安全父级");
        }
    }

    /// <summary>设置自动保护开关</summary>
    public void SetAutoProtectionEnabled(bool enabled)
    {
        enableAutoProtection = enabled;
        
        if (!enabled)
        {
            StopParentMonitoring();
        }
        else if (currentGridParent != null)
        {
            StartParentMonitoring();
        }
    }

    /// <summary>获取当前是否在监控父级</summary>
    public bool IsMonitoringParent()
    {
        return isMonitoringParent;
    }

    /// <summary>获取当前网格父级</summary>
    public Transform GetCurrentGridParent()
    {
        return currentGridParent;
    }

    /// <summary>获取安全父级</summary>
    public Transform GetSafeParent()
    {
        return safeParent;
    }

    /// <summary>确保高亮在安全父级下</summary>
    private void EnsureInSafeParent()
    {
        if (highlighter == null || safeParent == null) return;
        
        // 如果当前不在安全父级下，则移动到安全父级
        if (highlighter.parent != safeParent)
        {
            try
            {
                highlighter.SetParent(safeParent, false);
                StopParentMonitoring(); // 停止监控
                currentGridParent = null;
                Debug.Log($"InventoryHighlight: 确保在安全父级下 - {safeParent.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventoryHighlight: 确保安全父级时发生错误 - {e.Message}");
            }
        }
    }
    #endregion

    private void OnDestroy()
    {
        // 组件销毁时停止所有协程
        StopParentMonitoring();
    }
}
