using UnityEngine;
using UnityEngine.UI;

// 将屏幕空间 UI 跟随到一个世界坐标（如玩家头顶）
// 用法：挂在屏幕空间 Canvas 下的某个 UI 节点（如 PlayerUI/Reload），
// 在 Inspector 绑定 target（玩家 Transform）与 offset（头顶偏移）。
public class UIFollowWorldTarget : MonoBehaviour
{
    [Header("自动绑定")]
    public bool autoFindTarget = true;
    public string playerTag = "Player";   // 优先按Tag查找
    public string targetChildPath = "";    // 例如 "HeadAnchor"，留空使用玩家根
    public bool rebindOnEnable = true;      // 重新启用时重试绑定

    [Header("跟随目标与偏移")]
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 1.8f, 0f); // 头顶偏移

    [Header("相机与 Canvas（可选）")]
    public Camera targetCamera;       // 若不设，自动取 Camera.main
    public Canvas rootCanvas;         // 若不设，自动向上查找

    [Header("屏幕边界裁剪")]
    public bool clampToScreen = true;
    public Vector2 padding = new Vector2(10f, 10f);

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        TryAutoBindTarget();
    }

    void OnEnable()
    {
        if (rebindOnEnable)
        {
            TryAutoBindTarget();
        }
    }

    void LateUpdate()
    {
        if (target == null || rectTransform == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                return;
            }
        }

        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.WorldSpace)
        {
            // 该脚本用于 Screen Space Canvas，若为 WorldSpace 则无需此脚本
            return;
        }

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        // 不可见时隐藏（可按需关闭）
        bool behindCamera = screenPos.z < 0f;
        if (behindCamera)
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }
        if (!rectTransform.gameObject.activeSelf)
        {
            rectTransform.gameObject.SetActive(true);
        }

        Vector2 anchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screenPos,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCamera,
            out anchored);

        if (clampToScreen && rootCanvas.transform is RectTransform canvasRect)
        {
            Vector2 half = canvasRect.rect.size * 0.5f;
            anchored.x = Mathf.Clamp(anchored.x, -half.x + padding.x, half.x - padding.x);
            anchored.y = Mathf.Clamp(anchored.y, -half.y + padding.y, half.y - padding.y);
        }

        rectTransform.anchoredPosition = anchored;
    }

    public void ForceRebind()
    {
        target = null;
        TryAutoBindTarget();
    }

    private void TryAutoBindTarget()
    {
        if (!autoFindTarget || target != null)
        {
            return;
        }

        GameObject go = null;

        // 1) 优先：按Tag查找
        if (!string.IsNullOrEmpty(playerTag))
        {
            try
            {
                go = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch
            {
                // 若工程未定义该Tag，忽略异常
            }
        }

        // 2) 兜底：按名称包含"Player"查找
        if (go == null)
        {
            Transform[] all = GameObject.FindObjectsOfType<Transform>();
            foreach (var t in all)
            {
                if (t != null && (t.name == "Player" || t.name.Contains("Player")))
                {
                    go = t.gameObject;
                    break;
                }
            }
        }

        if (go != null)
        {
            if (!string.IsNullOrEmpty(targetChildPath))
            {
                var child = go.transform.Find(targetChildPath);
                target = child != null ? child : go.transform;
            }
            else
            {
                target = go.transform;
            }
        }
    }
}


