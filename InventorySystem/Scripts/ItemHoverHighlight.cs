using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ItemHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image overlay;
    [SerializeField] private float fadeTime = 0.1f; // 悬停高亮的淡入/淡出时长（秒）
    private DraggableItem draggableItem; // 拖拽组件，用于在拖拽时屏蔽悬停高亮

    private void Awake()
    {
        if (overlay == null)
        {
            overlay = GetComponentInChildren<Image>();
        }
        
        // 获取 DraggableItem 组件引用
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
        // 正在拖拽时不响应指针进入（避免与拖拽交互冲突）
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
        // 正在拖拽时不响应指针离开
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

    IEnumerator Fade(float targetAlpha) // 淡入淡出协程
    {
        Color start = overlay.color;
        Color target = new Color(start.r, start.g, start.b, targetAlpha); // 目标颜色（仅修改 alpha）
        
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            overlay.color = Color.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        overlay.color = target;
    }
}