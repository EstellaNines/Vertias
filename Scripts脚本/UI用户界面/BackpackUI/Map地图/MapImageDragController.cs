using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapImageDragController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("地图拖动设置")]
    [SerializeField] private Image mapImage; // 地图的Image组件
    [SerializeField] private RectTransform mapContainer; // 地图容器的RectTransform
    [SerializeField] private float longPressTime = 0.3f; // 长按时间阈值
    [SerializeField] private float dragSensitivity = 1.0f; // 拖动灵敏度
    [SerializeField] private bool enableInertia = true; // 是否启用惯性
    [SerializeField] private float inertiaDecay = 0.95f; // 惯性衰减系数

    [Header("边界限制设置")]
    [SerializeField] private bool enableBoundary = true; // 是否启用边界限制
    [SerializeField] private RectTransform boundaryRect; // 边界矩形（通常是父容器）
    [SerializeField] private Vector2 boundaryPadding = Vector2.zero; // 边界内边距

    [Header("视觉反馈设置")]
    [SerializeField] private bool enableVisualFeedback = true; // 是否启用视觉反馈
    [SerializeField] private Color normalColor = Color.white; // 正常颜色
    [SerializeField] private Color dragColor = new Color(0.9f, 0.9f, 0.9f, 1f); // 拖动时颜色

    // 私有变量
    private bool isLongPressing = false;
    private bool isDragging = false;
    private Vector2 lastPointerPosition;
    private Vector2 velocity = Vector2.zero;
    private Coroutine longPressCoroutine;
    private Coroutine inertiaCoroutine;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;

    // 边界计算相关
    private Vector2 minPosition;
    private Vector2 maxPosition;

    // 事件
    public System.Action OnMapDragStart;
    public System.Action OnMapDragEnd;
    public System.Action<Vector2> OnMapDragUpdate;

    private void Start()
    {
        InitializeComponents();
        CalculateBoundaries();
        SetupVisualFeedback();
    }

    private void InitializeComponents()
    {
        // 如果没有指定地图Image，尝试获取当前对象的Image组件
        if (mapImage == null)
        {
            mapImage = GetComponent<Image>();
        }

        // 如果没有指定地图容器，使用地图Image的RectTransform
        if (mapContainer == null && mapImage != null)
        {
            mapContainer = mapImage.rectTransform;
        }

        // 如果没有指定边界矩形，使用父对象
        if (boundaryRect == null && mapContainer != null)
        {
            boundaryRect = mapContainer.parent as RectTransform;
        }

        // 获取父Canvas用于坐标转换
        parentCanvas = GetComponentInParent<Canvas>();

        // 获取或添加CanvasGroup组件用于视觉反馈
        if (enableVisualFeedback)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 确保Image组件可以接收射线检测
        if (mapImage != null)
        {
            mapImage.raycastTarget = true;
        }
    }

    private void CalculateBoundaries()
    {
        if (!enableBoundary || mapContainer == null || boundaryRect == null)
            return;

        // 计算地图可移动的边界范围
        Vector2 containerSize = mapContainer.rect.size;
        Vector2 boundarySize = boundaryRect.rect.size;

        // 计算最大和最小位置（修正边界计算逻辑）
        float maxX = Mathf.Max(0, (containerSize.x - boundarySize.x) * 0.5f - boundaryPadding.x);
        float maxY = Mathf.Max(0, (containerSize.y - boundarySize.y) * 0.5f - boundaryPadding.y);

        minPosition = new Vector2(-maxX, -maxY);
        maxPosition = new Vector2(maxX, maxY);

        Debug.Log($"地图边界计算: 容器尺寸={containerSize}, 边界尺寸={boundarySize}, 移动范围=({minPosition}, {maxPosition})");
    }

    private void SetupVisualFeedback()
    {
        if (enableVisualFeedback && mapImage != null)
        {
            mapImage.color = normalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 只响应左键
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        Debug.Log("开始按下地图");

        // 停止惯性移动
        StopInertia();

        // 记录初始位置（转换为本地坐标）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapContainer, eventData.position, eventData.pressEventCamera, out lastPointerPosition);

        // 开始长按检测
        longPressCoroutine = StartCoroutine(LongPressDetection());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 只响应左键
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        Debug.Log("松开地图");

        // 停止长按检测
        StopLongPressDetection();

        // 结束拖动
        if (isDragging)
        {
            EndDrag();
        }

        // 重置状态
        isLongPressing = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 移除长按限制，直接允许拖动
        isDragging = true;

        Debug.Log("开始拖动地图");

        // 应用视觉反馈
        ApplyVisualFeedback(true);

        // 触发拖动开始事件
        OnMapDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 只有在拖动状态下才处理拖动
        if (!isDragging)
            return;

        // 转换屏幕坐标到本地坐标
        Vector2 currentPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapContainer, eventData.position, eventData.pressEventCamera, out currentPointerPosition);

        // 计算拖动偏移
        Vector2 deltaPosition = (currentPointerPosition - lastPointerPosition) * dragSensitivity;

        // 更新速度（用于惯性）
        velocity = deltaPosition;

        // 移动地图
        MoveMap(deltaPosition);

        // 更新上一次位置
        lastPointerPosition = currentPointerPosition;

        // 触发拖动更新事件
        OnMapDragUpdate?.Invoke(deltaPosition);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            EndDrag();
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        Debug.Log("结束拖动地图");

        // 恢复视觉反馈
        ApplyVisualFeedback(false);

        // 触发拖动结束事件
        OnMapDragEnd?.Invoke();

        // 开始惯性移动
        if (enableInertia && velocity.magnitude > 0.1f)
        {
            inertiaCoroutine = StartCoroutine(InertiaMovement());
        }
    }

    private IEnumerator LongPressDetection()
    {
        yield return new WaitForSeconds(longPressTime);
        isLongPressing = true;
        Debug.Log("检测到长按");
    }

    private void StopLongPressDetection()
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private void StopInertia()
    {
        if (inertiaCoroutine != null)
        {
            StopCoroutine(inertiaCoroutine);
            inertiaCoroutine = null;
        }
        velocity = Vector2.zero;
    }

    private IEnumerator InertiaMovement()
    {
        while (velocity.magnitude > 0.1f)
        {
            // 应用惯性移动
            MoveMap(velocity * Time.deltaTime * 60f);

            // 衰减速度
            velocity *= inertiaDecay;

            yield return null;
        }

        velocity = Vector2.zero;
    }

    private void MoveMap(Vector2 deltaPosition)
    {
        if (mapContainer == null)
            return;

        // 计算新位置
        Vector2 newPosition = mapContainer.anchoredPosition + deltaPosition;

        // 应用边界限制
        if (enableBoundary)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x);
            newPosition.y = Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y);
        }

        // 应用新位置
        mapContainer.anchoredPosition = newPosition;
    }

    private void ApplyVisualFeedback(bool isDragging)
    {
        if (!enableVisualFeedback)
            return;

        if (mapImage != null)
        {
            mapImage.color = isDragging ? dragColor : normalColor;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isDragging ? 0.8f : 1.0f;
        }
    }

    // 公共方法：设置地图位置
    public void SetMapPosition(Vector2 position)
    {
        if (mapContainer == null)
            return;

        if (enableBoundary)
        {
            position.x = Mathf.Clamp(position.x, minPosition.x, maxPosition.x);
            position.y = Mathf.Clamp(position.y, minPosition.y, maxPosition.y);
        }

        mapContainer.anchoredPosition = position;
    }

    // 公共方法：获取地图位置
    public Vector2 GetMapPosition()
    {
        return mapContainer != null ? mapContainer.anchoredPosition : Vector2.zero;
    }

    // 公共方法：重置地图位置
    public void ResetMapPosition()
    {
        SetMapPosition(Vector2.zero);
    }

    // 公共方法：重新计算边界
    public void RecalculateBoundaries()
    {
        CalculateBoundaries();
    }

    // 公共方法：启用/禁用拖动
    public void SetDragEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (!enabled)
        {
            // 停止所有拖动相关的协程
            StopLongPressDetection();
            StopInertia();

            if (isDragging)
            {
                EndDrag();
            }
        }
    }

    private void OnValidate()
    {
        // 确保参数在合理范围内
        longPressTime = Mathf.Max(0, longPressTime);
        dragSensitivity = Mathf.Max(0, dragSensitivity);
        inertiaDecay = Mathf.Clamp01(inertiaDecay);
    }

    private void OnRectTransformDimensionsChange()
    {
        // 当UI尺寸改变时重新计算边界
        if (enableBoundary)
        {
            CalculateBoundaries();
        }
    }
}