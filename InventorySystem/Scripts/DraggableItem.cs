using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖拽物品组件
/// </summary>
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("拖拽设置")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Transform originalParent;
    private ItemDataReader itemDataReader; // 引用物品数据读取器

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemDataReader = GetComponent<ItemDataReader>();

        // 如果没有CanvasGroup组件，自动添加
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 查找Canvas和GraphicRaycaster
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 记录原始位置和父对象
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // 设置为半透明并禁用射线检测
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // 移动到Canvas的最顶层
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 跟随鼠标移动
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复透明度和射线检测
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;

        // 检查是否有有效的放置目标
        bool validDrop = false;

        // 检查是否放置在装备槽位上
        if (eventData.pointerEnter != null)
        {
            EquipmentSlot equipmentSlot = eventData.pointerEnter.GetComponent<EquipmentSlot>();
            if (equipmentSlot != null)
            {
                validDrop = true; // 装备槽位会处理放置逻辑
            }
        }

        // 如果没有有效放置目标，返回原位置
        if (!validDrop)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// 设置拖拽是否启用
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetDragEnabled(bool enabled)
    {
        canvasGroup.blocksRaycasts = enabled;
    }

    /// <summary>
    /// 获取关联的物品数据读取器
    /// </summary>
    /// <returns>物品数据读取器</returns>
    public ItemDataReader GetItemDataReader()
    {
        return itemDataReader;
    }

    /// <summary>
    /// 获取原始位置
    /// </summary>
    /// <returns>原始位置</returns>
    public Vector2 GetOriginalPosition()
    {
        return originalPosition;
    }

    /// <summary>
    /// 获取原始父对象
    /// </summary>
    /// <returns>原始父对象</returns>
    public Transform GetOriginalParent()
    {
        return originalParent;
    }
}