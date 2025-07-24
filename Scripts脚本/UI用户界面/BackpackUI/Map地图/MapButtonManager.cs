using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MapButtonManager : MonoBehaviour
{
    public static MapButtonManager Instance { get; private set; }

    [Header("全局精灵配置")]
    [FieldLabel("悬停/按下时的精灵")] public Sprite hoverSprite;         // 悬停/按下时的精灵
    [FieldLabel("按下时的精灵")] public Sprite pressedSprite;       // 按下时的精灵
    [FieldLabel("Lock 默认精灵")] public Sprite lockDefaultSprite;   // Lock 默认精灵
    [FieldLabel("Lock 悬停/按下时的精灵")] public Sprite lockHoverSprite;     // Lock 悬停/按下时的精灵
    [FieldLabel("Lock 按下时的精灵")] public Sprite lockPressedSprite;   // Lock 按下时的精灵

    [Header("全局颜色配置")]
    [FieldLabel("默认文本颜色")] public Color defaultTextColor = Color.white;
    [FieldLabel("悬停文本颜色")] public Color hoverTextColor = Color.yellow;
    [FieldLabel("按下文本颜色")] public Color pressedTextColor = Color.red;
    [FieldLabel("解锁文本颜色")] public Color unlockedTextColor = Color.green;

    [Header("全局解锁控制")]
    [SerializeField] private bool globalUnlockedState = false;  // 全局解锁状态

    [Header("按钮列表")]
    [FieldLabel("地图按钮列表")] public List<MapButton> buttons = new List<MapButton>();

    [Header("Lock图标引用列表")]
    [FieldLabel("Lock图标列表")] public List<Image> lockImages = new List<Image>();  // 每个按钮对应的Lock图标引用

    [Header("地图ID配置")]
    [FieldLabel("按钮对应的地图ID列表")] public List<int> buttonMapIds = new List<int>();  // 每个按钮对应的地图ID

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 自动收集Lock图标引用
        CollectLockImageReferences();
        // 应用配置到所有按钮
        ApplyConfigurationToAllButtons();
        // 设置按钮ID
        SetButtonMapIds();

        // 新增：调试所有按钮状态
        DebugAllButtonStates();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 为所有按钮设置对应的地图ID
    private void SetButtonMapIds()
    {
        for (int i = 0; i < buttons.Count && i < buttonMapIds.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].SetMapId(buttonMapIds[i]);
                Debug.Log($"按钮 {buttons[i].name} 设置地图ID: {buttonMapIds[i]}");
            }
        }
    }

    // 添加按钮时同时设置ID
    public void AddButton(MapButton button, int mapId = 0)
    {
        if (!buttons.Contains(button))
        {
            buttons.Add(button);
            buttonMapIds.Add(mapId);

            // 添加对应的Lock图标引用
            Image lockImg = button != null ? button.GetLockImage() : null;
            lockImages.Add(lockImg);

            // 设置按钮的地图ID
            if (button != null)
            {
                button.SetMapId(mapId);
            }

            ApplyConfigurationToAllButtons();
        }
    }

    // 移除按钮时同时移除ID（保留这个版本，移除重复的）
    public void RemoveButton(MapButton button)
    {
        int index = buttons.IndexOf(button);
        if (index >= 0)
        {
            buttons.RemoveAt(index);

            // 移除对应的地图ID
            if (index < buttonMapIds.Count)
            {
                buttonMapIds.RemoveAt(index);
            }

            // 移除对应的Lock图标引用
            if (index < lockImages.Count)
            {
                lockImages.RemoveAt(index);
            }
        }
    }

    // 设置特定按钮的地图ID
    public void SetButtonMapId(int buttonIndex, int mapId)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Count && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].SetMapId(mapId);

            // 更新ID列表
            if (buttonIndex < buttonMapIds.Count)
            {
                buttonMapIds[buttonIndex] = mapId;
            }
            else
            {
                // 如果列表不够长，扩展列表
                while (buttonMapIds.Count <= buttonIndex)
                {
                    buttonMapIds.Add(0);
                }
                buttonMapIds[buttonIndex] = mapId;
            }
        }
    }

    // 获取特定按钮的地图ID
    public int GetButtonMapId(int buttonIndex)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Count && buttons[buttonIndex] != null)
        {
            return buttons[buttonIndex].GetMapId();
        }
        return -1;
    }

    // 根据地图ID查找按钮
    public MapButton FindButtonByMapId(int mapId)
    {
        Debug.Log($"查找地图ID: {mapId}，当前按钮数量: {buttons.Count}");

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                int buttonMapId = buttons[i].GetMapId();
                bool isUnlocked = buttons[i].IsUnlocked();
                Debug.Log($"按钮 {i}: {buttons[i].name}, 地图ID: {buttonMapId}, 解锁状态: {isUnlocked}");

                if (buttonMapId == mapId)
                {
                    Debug.Log($"找到匹配的按钮: {buttons[i].name}");
                    return buttons[i];
                }
            }
        }

        Debug.LogWarning($"未找到地图ID {mapId} 对应的按钮");
        return null;
    }

    // 新增：调试所有按钮状态的方法
    public void DebugAllButtonStates()
    {
        Debug.Log("=== 所有按钮状态调试信息 ===");
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                Debug.Log($"按钮 {i}: {buttons[i].name}, 地图ID: {buttons[i].GetMapId()}, 解锁状态: {buttons[i].IsUnlocked()}");
            }
            else
            {
                Debug.Log($"按钮 {i}: null");
            }
        }
        Debug.Log("=== 调试信息结束 ===");
    }

    // 验证所有按钮的ID是否有效
    public void ValidateButtonMapIds()
    {
        MapDisplayController displayController = FindObjectOfType<MapDisplayController>();
        if (displayController != null)
        {
            MapData[] allMapData = displayController.GetAllMapData();
            if (allMapData != null)
            {
                HashSet<int> validIds = new HashSet<int>();
                foreach (var mapData in allMapData)
                {
                    validIds.Add(mapData.id);
                }

                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i] != null)
                    {
                        int buttonMapId = buttons[i].GetMapId();
                        if (!validIds.Contains(buttonMapId))
                        {
                            Debug.LogWarning($"按钮 {buttons[i].name} 的地图ID {buttonMapId} 在JSON数据中不存在！");
                        }
                    }
                }
            }
        }
    }

    // 自动分配连续的地图ID（从1开始）
    public void AutoAssignMapIds()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int mapId = i + 1;  // 从1开始分配ID
            SetButtonMapId(i, mapId);
        }
    }

    // 自动收集Lock图标引用
    private void CollectLockImageReferences()
    {
        lockImages.Clear();
        foreach (var button in buttons)
        {
            if (button != null)
            {
                Image lockImg = button.GetLockImage();
                lockImages.Add(lockImg);
            }
            else
            {
                lockImages.Add(null);
            }
        }
    }

    public void ApplyConfigurationToAllButtons()
    {
        foreach (var button in buttons)
        {
            if (button != null)
            {
                button.hoverSprite = hoverSprite;
                button.lockDefaultSprite = lockDefaultSprite;
                button.lockHoverSprite = lockHoverSprite;

                button.defaultTextColor = defaultTextColor;
                button.hoverTextColor = hoverTextColor;
                button.pressedTextColor = pressedTextColor;
                button.unlockedTextColor = unlockedTextColor;

                // 不在这里应用全局解锁状态，避免覆盖Inspector中的设置
                // button.SetUnlockedState(globalUnlockedState);
            }
        }
    }

    // 设置所有地图的解锁状态
    public void SetAllMapsUnlockedState(bool unlocked)
    {
        globalUnlockedState = unlocked;
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].SetUnlockedState(unlocked);
            }
            // 直接控制Lock图标显示
            if (i < lockImages.Count && lockImages[i] != null)
            {
                lockImages[i].gameObject.SetActive(!unlocked);
            }
        }
    }

    // 设置特定地图的解锁状态
    public void SetSpecificMapUnlockedState(int buttonIndex, bool unlocked)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Count && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].SetUnlockedState(unlocked);
            // 直接控制对应Lock图标显示
            if (buttonIndex < lockImages.Count && lockImages[buttonIndex] != null)
            {
                lockImages[buttonIndex].gameObject.SetActive(!unlocked);
            }
        }
    }

    // 获取全局解锁状态
    public bool GetGlobalUnlockedState()
    {
        return globalUnlockedState;
    }

    // 批量设置多个地图的解锁状态
    public void SetMultipleMapsUnlockedState(List<int> buttonIndices, bool unlocked)
    {
        foreach (int index in buttonIndices)
        {
            if (index >= 0 && index < buttons.Count && buttons[index] != null)
            {
                buttons[index].SetUnlockedState(unlocked);
                // 直接控制对应Lock图标显示
                if (index < lockImages.Count && lockImages[index] != null)
                {
                    lockImages[index].gameObject.SetActive(!unlocked);
                }
            }
        }
    }

    // 直接控制所有Lock图标的显示/隐藏
    public void SetAllLockImagesVisible(bool visible)
    {
        for (int i = 0; i < lockImages.Count; i++)
        {
            if (lockImages[i] != null)
            {
                lockImages[i].gameObject.SetActive(visible);
            }
        }
    }

    // 直接控制特定Lock图标的显示/隐藏
    public void SetSpecificLockImageVisible(int index, bool visible)
    {
        if (index >= 0 && index < lockImages.Count && lockImages[index] != null)
        {
            lockImages[index].gameObject.SetActive(visible);
        }
    }

    // 获取特定Lock图标引用
    public Image GetLockImage(int index)
    {
        if (index >= 0 && index < lockImages.Count)
        {
            return lockImages[index];
        }
        return null;
    }

    // 手动刷新Lock图标引用列表
    public void RefreshLockImageReferences()
    {
        CollectLockImageReferences();
    }
}
