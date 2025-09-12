using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 使用 DOTween 为 TMP 文本提供从左到右的打字机效果（默认 5 秒）。
/// 将本组件挂到包含 TextMeshProUGUI 的对象或单独对象上，并绑定引用。
/// </summary>
public class TypingTextTMP : MonoBehaviour
{
    [Header("目标文本")]
    public TextMeshProUGUI targetText;

    [Header("播放配置")]
    [Tooltip("打字机总时长（秒）")]
    public float duration = 5f;
    [Tooltip("启用时自动播放（使用现有文本或下方文本域）")]
    public bool playOnEnable = false;
    [Tooltip("playOnEnable 时使用对象当前文本；若关闭则使用 textToType 文本域")]
    public bool useExistingTextOnPlay = true;
    [TextArea]
    public string textToType;

    private Tween typingTween;
    public System.Action onTypingComplete;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            if (!useExistingTextOnPlay && !string.IsNullOrEmpty(textToType))
            {
                SetText(textToType);
            }
            Play();
        }
    }

    /// <summary>
    /// 设定显示文本（不会自动播放）。
    /// </summary>
    public void SetText(string content)
    {
        EnsureTarget();
        if (targetText == null) return;
        targetText.text = content ?? string.Empty;
    }

    /// <summary>
    /// 使用当前文本播放打字机效果。
    /// </summary>
    public void Play()
    {
        EnsureTarget();
        if (targetText == null) return;

        // 结束旧动画
        if (typingTween != null && typingTween.IsActive()) typingTween.Kill();

        // 强制更新字符信息并获取总字符数（TMP 会忽略富文本标签）
        targetText.ForceMeshUpdate();
        int totalChars = targetText.textInfo.characterCount;

        // 从 0 开始逐步显示字符
        targetText.maxVisibleCharacters = 0;
        typingTween = DOTween
            .To(() => 0, v => targetText.maxVisibleCharacters = v, totalChars, Mathf.Max(0.01f, duration))
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                onTypingComplete?.Invoke();
            });
    }

    /// <summary>
    /// 使用指定文本与可选时长播放打字机效果。
    /// </summary>
    public void Play(string content, float overrideDuration = -1f)
    {
        SetText(content);
        if (overrideDuration >= 0f) duration = overrideDuration;
        Play();
    }

    /// <summary>
    /// 立即跳过动画并显示完整文本。
    /// </summary>
    public void SkipToEnd()
    {
        EnsureTarget();
        if (targetText == null) return;
        if (typingTween != null && typingTween.IsActive()) typingTween.Kill();
        targetText.ForceMeshUpdate();
        targetText.maxVisibleCharacters = targetText.textInfo.characterCount;
    }

    private void OnDisable()
    {
        if (typingTween != null && typingTween.IsActive()) typingTween.Kill();
    }

    private void EnsureTarget()
    {
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
    }
}


