using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ItemHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image overlay;   // 拖白色 Image
    [SerializeField] float fadeTime = 0.15f;

    Color normal = Color.white.WithAlpha(0f);
    Color hover = Color.white.WithAlpha(0.35f);

    void Awake()
    {
        if (overlay == null) overlay = GetComponentInChildren<Image>();
        overlay.color = normal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(Fade(hover));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartCoroutine(Fade(normal));
    }

    IEnumerator Fade(Color target)
    {
        Color start = overlay.color;
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            overlay.color = Color.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        overlay.color = target;
    }
}