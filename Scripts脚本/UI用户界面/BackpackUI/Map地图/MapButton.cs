using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class MapButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("状态精灵")]
    [FieldLabel("悬停/按下时的精灵")] public Sprite hoverSprite;         // 悬停/按下时的精灵
    [FieldLabel("按下时的精灵")] public Sprite pressedSprite;       // 按下时的精灵
    [FieldLabel("Lock 默认精灵")] public Sprite lockDefaultSprite;   // Lock 默认精灵
    [FieldLabel("Lock 悬停/按下时的精灵")] public Sprite lockHoverSprite;     // Lock 悬停/按下时的精灵
    [FieldLabel("Lock 按下时的精灵")] public Sprite lockPressedSprite;   // Lock 按下时的精灵

    [Header("状态颜色")]
    [FieldLabel("默认文本颜色")] public Color defaultTextColor = Color.white;
    [FieldLabel("悬停文本颜色")] public Color hoverTextColor = Color.yellow;
    [FieldLabel("按下文本颜色")] public Color pressedTextColor = Color.red;
    [FieldLabel("解锁文本颜色")] public Color unlockedTextColor = Color.green;

    [Header("状态控制")]
    [FieldLabel("是否已解锁地图")] public bool isUnlocked = false;    // 是否已解锁地图

    [Header("Lock图标引用")]
    [FieldLabel("Lock图标对象")] public Image lockImage;  // Lock图标的Image组件引用

    [Header("地图信息")]
    [FieldLabel("地图ID")] public int mapId = 0;  // 当前按钮对应的地图ID

    [Header("显示控制器引用")]
    [FieldLabel("地图显示控制器")] public MapDisplayController mapDisplayController;  // 地图显示控制器引用

    private Image image;
    private Sprite originalSprite;
    private TextMeshProUGUI textComponent;
    private Color originalTextColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        originalSprite = image.sprite;
        if (lockImage == null)
        {
            Transform lockTransform = transform.Find("Lock");
            if (lockTransform != null)
            {
                lockImage = lockTransform.GetComponent<Image>();
            }
        }
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            originalTextColor = textComponent.color;
        }
        // 这里恢复为FindObjectOfType
        if (mapDisplayController == null)
        {
            mapDisplayController = FindObjectOfType<MapDisplayController>();
        }
        UpdateLockState();
        UpdateTextColor();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUnlocked)
        {
            // 解锁状态下的点击效果
            if (hoverSprite != null)
            {
                image.sprite = hoverSprite;
            }
            if (textComponent != null)
            {
                textComponent.color = pressedTextColor;
            }
            
            Debug.Log($"选择了已解锁的地图: {gameObject.name}, 地图ID: {mapId}");
        }
        else
        {
            // 未解锁状态下的点击效果
            if (hoverSprite != null)
            {
                image.sprite = hoverSprite;
                if (lockImage != null && lockHoverSprite != null)
                {
                    lockImage.sprite = lockHoverSprite;
                }
                if (textComponent != null)
                {
                    textComponent.color = pressedTextColor;
                }
            }
            Debug.Log($"选择了未解锁的地图: {gameObject.name}, 地图ID: {mapId}");
        }
        
        // MapButton的主要职责：显示地图信息，不直接切换场景
        if (mapDisplayController != null)
        {
            mapDisplayController.DisplayMapInfo(mapId);
        }
    }
    
    private void OnValidate()
    {
        // 在Inspector中修改值时自动更新状态
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
            // 解锁状态下的悬停效果 - 添加精灵变化
            if (hoverSprite != null)
            {
                image.sprite = hoverSprite;
            }
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
            // 解锁状态下恢复原始精灵和解锁颜色
            image.sprite = originalSprite;
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

    // 设置地图ID
    public void SetMapId(int id)
    {
        mapId = id;
    }
    
    // 获取地图ID
    public int GetMapId()
    {
        return mapId;
    }
    
    // 设置显示控制器引用
    public void SetMapDisplayController(MapDisplayController controller)
    {
        mapDisplayController = controller;
    }
}
