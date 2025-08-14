using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ItemHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image overlay;
    [SerializeField] private float fadeTime = 0.1f; // 添加fadeTime变量
    private DraggableItem draggableItem; // 添加引用

    private void Awake()
    {
        if (overlay == null)
        {
            overlay = GetComponentInChildren<Image>();
        }
        
        // 获取DraggableItem组件引用
        draggableItem = GetComponent<DraggableItem>();
        
        if (overlay != null)
        {
            Color color = overlay.color;
            color.a = 0;
            overlay.color = color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 检查是否正在拖拽，如果是则不响应悬停事件
        if (draggableItem != null && draggableItem.isDragging)
            return;
            
        if (overlay != null)
        {
            StopAllCoroutines();
            StartCoroutine(Fade(0.3f));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 检查是否正在拖拽，如果是则不响应悬停事件
        if (draggableItem != null && draggableItem.isDragging)
            return;
            
        if (overlay != null)
        {
            StopAllCoroutines();
            StartCoroutine(Fade(0f));
        }
    }

    public void ForceHide()
    {
        if (overlay != null)
        {
            StopAllCoroutines();
            Color color = overlay.color;
            color.a = 0;
            overlay.color = color;
        }
    }

    IEnumerator Fade(float targetAlpha) // 修改参数类型为float
    {
        Color start = overlay.color;
        Color target = new Color(start.r, start.g, start.b, targetAlpha); // 创建目标颜色
        
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            overlay.color = Color.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        overlay.color = target;
    }
}