using System.Collections.Generic;
using UnityEngine;

public class MapButtonManager : MonoBehaviour
{
    [Header("全局精灵配置")]
    public Sprite hoverSprite;         // 悬停时的精灵
    public Sprite pressedSprite;       // 按下时的精灵
    public Sprite lockDefaultSprite;   // Lock 默认精灵
    public Sprite lockHoverSprite;     // Lock 悬停时的精灵
    public Sprite lockPressedSprite;   // Lock 按下时的精灵

    [Header("全局颜色配置")]
    public Color defaultTextColor = Color.white;
    public Color hoverTextColor = Color.yellow;
    public Color pressedTextColor = Color.red;
    public Color unlockedTextColor = Color.green;

    [Header("按钮列表")]
    public List<MapButton> buttons = new List<MapButton>();

    private void Awake()
    {
        ApplyConfigurationToAllButtons();
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
            }
        }
    }

    public void AddButton(MapButton button)
    {
        if (!buttons.Contains(button))
        {
            buttons.Add(button);
            ApplyConfigurationToAllButtons();
        }
    }

    public void RemoveButton(MapButton button)
    {
        if (buttons.Contains(button))
        {
            buttons.Remove(button);
        }
    }
}