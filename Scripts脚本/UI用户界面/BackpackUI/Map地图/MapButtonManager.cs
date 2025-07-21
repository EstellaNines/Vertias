using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapButtonManager : MonoBehaviour
{
    [Header("全局精灵配置")]
    [FieldLabel("悬停时的精灵")] public Sprite hoverSprite;         // 悬停时的精灵

    [FieldLabel("按下时的精灵")] public Sprite pressedSprite;       // 按下时的精灵

    [FieldLabel("Lock 默认精灵")] public Sprite lockDefaultSprite;   // Lock 默认精灵
    [FieldLabel("Lock 悬停时的精灵")] public Sprite lockHoverSprite;     // Lock 悬停时的精灵
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

    private void Awake()
    {
        // 自动收集Lock图标引用
        CollectLockImageReferences();
        ApplyConfigurationToAllButtons();
    }

    private void OnValidate()
    {
        // 在Inspector中修改全局解锁状态时自动更新所有按钮（无论是否在运行时）
        SetAllMapsUnlockedState(globalUnlockedState);
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
                button.pressedSprite = pressedSprite;
                button.lockDefaultSprite = lockDefaultSprite;
                button.lockHoverSprite = lockHoverSprite;
                button.lockPressedSprite = lockPressedSprite;

                button.defaultTextColor = defaultTextColor;
                button.hoverTextColor = hoverTextColor;
                button.pressedTextColor = pressedTextColor;
                button.unlockedTextColor = unlockedTextColor;

                // button.SetUnlockedState(globalUnlockedState);
            }
        }
    }

    public void AddButton(MapButton button)
    {
        if (!buttons.Contains(button))
        {
            buttons.Add(button);
            // 添加对应的Lock图标引用
            Image lockImg = button != null ? button.GetLockImage() : null;
            lockImages.Add(lockImg);
            ApplyConfigurationToAllButtons();
        }
    }

    public void RemoveButton(MapButton button)
    {
        int index = buttons.IndexOf(button);
        if (index >= 0)
        {
            buttons.RemoveAt(index);
            // 移除对应的Lock图标引用
            if (index < lockImages.Count)
            {
                lockImages.RemoveAt(index);
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