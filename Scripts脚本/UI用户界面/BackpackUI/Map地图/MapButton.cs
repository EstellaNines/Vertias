using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class MapButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("状态精灵")]
    public Sprite hoverSprite;         // 悬停时的精灵
    public Sprite pressedSprite;       // 按下时的精灵
    public Sprite lockDefaultSprite;   // Lock 默认精灵
    public Sprite lockHoverSprite;     // Lock 悬停时的精灵
    public Sprite lockPressedSprite;   // Lock 按下时的精灵

    [Header("状态颜色")]
    public Color defaultTextColor = Color.white;
    public Color hoverTextColor = Color.yellow;
    public Color pressedTextColor = Color.red;
    public Color unlockedTextColor = Color.green;

    [Header("状态控制")]
    public bool isUnlocked = false;    // 是否已解锁地图

    private Image image;
    private Sprite originalSprite;
    private Image lockImage;
    private TextMeshProUGUI textComponent;
    private Color originalTextColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        originalSprite = image.sprite;

        // 自动获取子对象 Lock
        Transform lockTransform = transform.Find("Lock");
        if (lockTransform != null)
        {
            lockImage = lockTransform.GetComponent<Image>();
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

    private void UpdateLockState()
    {
        if (lockImage != null)
        {
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
        if (hoverSprite != null && !isUnlocked)
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

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isUnlocked)
        {
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
        if (pressedSprite != null && !isUnlocked)
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
    }
}