using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class MapButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("状态精灵")]
    [FieldLabel("悬停时的精灵")] public Sprite hoverSprite;         // 悬停时的精灵

    [FieldLabel("按下时的精灵")] public Sprite pressedSprite;       // 按下时的精灵

    [FieldLabel("Lock 默认精灵")] public Sprite lockDefaultSprite;   // Lock 默认精灵
    [FieldLabel("Lock 悬停时的精灵")] public Sprite lockHoverSprite;     // Lock 悬停时的精灵
    [FieldLabel("Lock 按下时的精灵")] public Sprite lockPressedSprite;   // Lock 按下时的精灵

    [Header("状态颜色")]
    [FieldLabel("默认文本颜色")] public Color defaultTextColor = Color.white;
    [FieldLabel("悬停文本颜色")] public Color hoverTextColor = Color.yellow;
    [FieldLabel("按下文本颜色")] public Color pressedTextColor = Color.red;
    [FieldLabel("解锁文本颜色")] public Color unlockedTextColor = Color.green;

    [Header("状态控制")]
    [FieldLabel("是否已解锁地图")] public bool isUnlocked = false;    // 是否已解锁地图（统一控制解锁状态和Lock图标显示）

    [Header("Lock图标引用")]
    [FieldLabel("Lock图标对象")] public Image lockImage;  // Lock图标的Image组件引用

    private Image image;
    private Sprite originalSprite;
    private TextMeshProUGUI textComponent;
    private Color originalTextColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        originalSprite = image.sprite;

        // 如果没有手动指定Lock图标，尝试自动获取子对象 Lock
        if (lockImage == null)
        {
            Transform lockTransform = transform.Find("Lock");
            if (lockTransform != null)
            {
                lockImage = lockTransform.GetComponent<Image>();
            }
        }

        // 自动获取子对象 TMP 文字
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            originalTextColor = textComponent.color;
        }

        UpdateLockState();
        UpdateTextColor();
    }

    private void OnValidate()
    {
        // 在Inspector中修改值时自动更新状态（无论是否在运行时）
        UpdateLockState();
        UpdateTextColor();
    }

    private void UpdateLockState()
    {
        if (lockImage != null)
        {
            // 只有当地图已解锁时，Lock图标才隐藏
            lockImage.gameObject.SetActive(!isUnlocked);

            if (!isUnlocked)
            {
                lockImage.sprite = lockDefaultSprite;
            }
        }
    }

    private void UpdateTextColor()
    {
        if (textComponent == null) return;

        if (isUnlocked)
        {
            textComponent.color = unlockedTextColor;
        }
        else
        {
            textComponent.color = defaultTextColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            // 解锁状态下的悬停效果
            if (textComponent != null)
            {
                textComponent.color = hoverTextColor;
            }
        }
        else
        {
            // 未解锁状态下的悬停效果
            if (hoverSprite != null)
            {
                image.sprite = hoverSprite;
                if (lockImage != null && lockHoverSprite != null)
                {
                    lockImage.sprite = lockHoverSprite;
                }
                if (textComponent != null)
                {
                    textComponent.color = hoverTextColor;
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            // 解锁状态下恢复解锁颜色
            if (textComponent != null)
            {
                textComponent.color = unlockedTextColor;
            }
        }
        else
        {
            // 未解锁状态下恢复默认状态
            image.sprite = originalSprite;
            if (lockImage != null && lockDefaultSprite != null)
            {
                lockImage.sprite = lockDefaultSprite;
            }
            if (textComponent != null)
            {
                textComponent.color = defaultTextColor;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            // 解锁状态下的点击效果 - 可以添加地图跳转逻辑
            if (textComponent != null)
            {
                textComponent.color = pressedTextColor;
            }
            // 这里可以添加实际的地图跳转或其他功能
            Debug.Log($"点击了已解锁的地图按钮: {gameObject.name}");
        }
        else
        {
            // 未解锁状态下的点击效果
            if (pressedSprite != null)
            {
                image.sprite = pressedSprite;
                if (lockImage != null && lockPressedSprite != null)
                {
                    lockImage.sprite = lockPressedSprite;
                }
                if (textComponent != null)
                {
                    textComponent.color = pressedTextColor;
                }
            }
            // 可以添加提示信息，告知玩家地图未解锁
            Debug.Log($"地图未解锁: {gameObject.name}");
        }
    }

    // 公共方法：设置解锁状态
    public void SetUnlockedState(bool unlocked)
    {
        isUnlocked = unlocked;
        UpdateLockState();
        UpdateTextColor();
    }

    // 公共方法：获取解锁状态
    public bool IsUnlocked()
    {
        return isUnlocked;
    }

    // 公共方法：获取Lock图标引用
    public Image GetLockImage()
    {
        return lockImage;
    }

    // 公共方法：设置Lock图标引用
    public void SetLockImage(Image lockImg)
    {
        lockImage = lockImg;
        UpdateLockState();
    }

    // 公共方法：直接控制Lock图标显示/隐藏
    public void SetLockImageVisible(bool visible)
    {
        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(visible);
        }
    }
}