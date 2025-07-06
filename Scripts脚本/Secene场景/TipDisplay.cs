using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipDisplay : MonoBehaviour
{
    [Header("显示设置")]
    [SerializeField] private float displayDuration = 3f;    // 显示持续时间（秒）
    [SerializeField] private float fadeOutDuration = 1f;    // 淡出持续时间（秒）

    [Header("UI管理")]
    [SerializeField] private List<GameObject> uiObjects = new List<GameObject>(); // 需要管理的UI对象列表

    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    private void Start()
    {
        // 开始显示所有UI
        StartDisplayAll();
    }

    private void OnDisable()
    {
        // 停止所有协程
        StopAllCoroutines();
    }

    // 开始显示所有UI对象
    public void StartDisplayAll()
    {
        foreach (GameObject uiObj in uiObjects)
        {
            if (uiObj != null)
            {
                StartDisplaySingle(uiObj);
            }
        }
    }
    // 开始显示单个UI对象
    public void StartDisplaySingle(GameObject uiObject)
    {
        if (uiObject == null) return;

        // 确保对象是激活的
        uiObject.SetActive(true);

        // 获取或添加CanvasGroup组件
        CanvasGroup canvasGroup = uiObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObject.AddComponent<CanvasGroup>();
        }

        // 设置初始透明度
        canvasGroup.alpha = 1f;

        // 开始显示协程
        Coroutine displayCoroutine = StartCoroutine(DisplaySequence(uiObject, canvasGroup));
        activeCoroutines.Add(displayCoroutine);
    }

    // 显示序列协程
    private IEnumerator DisplaySequence(GameObject uiObject, CanvasGroup canvasGroup)
    {
        // 等待显示时间
        yield return new WaitForSeconds(displayDuration);

        // 开始淡出
        yield return StartCoroutine(FadeOut(canvasGroup));

        // 淡出完成后隐藏对象
        uiObject.SetActive(false);
    }

    // 淡出效果协程
    private IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    // 立即隐藏所有UI
    public void HideAllImmediately()
    {
        StopAllCoroutines();

        foreach (GameObject uiObj in uiObjects)
        {
            if (uiObj != null)
            {
                CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
                uiObj.SetActive(false);
            }
        }
    }

    // 停止所有协程
    private void StopAllCoroutines()
    {
        foreach (Coroutine coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();
    }

    // 添加UI对象到列表
    public void AddUIObject(GameObject uiObject)
    {
        if (uiObject != null && !uiObjects.Contains(uiObject))
        {
            uiObjects.Add(uiObject);
        }
    }

    // 从列表中移除UI对象
    public void RemoveUIObject(GameObject uiObject)
    {
        if (uiObjects.Contains(uiObject))
        {
            uiObjects.Remove(uiObject);
        }
    }

    // 设置显示持续时间
    public void SetDisplayDuration(float duration)
    {
        displayDuration = Mathf.Max(0f, duration);
    }

    // 设置淡出持续时间
    public void SetFadeOutDuration(float duration)
    {
        fadeOutDuration = Mathf.Max(0.1f, duration);
    }
}
