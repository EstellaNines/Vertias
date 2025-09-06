using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 简化版TextMeshPro打字机效果
/// 避免复杂的网格操作，专注于文本显示
/// </summary>
public class SimpleTMPTypewriter : MonoBehaviour
{
    [Header("=== 基础设置 ===")]
    [FieldLabel("文本组件")]
    [SerializeField] private TextMeshProUGUI textComponent;
    [FieldLabel("打字速度")]
    [Tooltip("每个字符显示的间隔时间 - 数值越大打字越慢，数值越小打字越快")]
    [SerializeField] private float typingSpeed = 0.05f;
    [FieldLabel("自动开始")]
    [SerializeField] private bool playOnStart = false;
    [FieldLabel("可以跳过")]
    [SerializeField] private bool canSkip = true;
    
    [Header("=== 音效设置 ===")]
    [FieldLabel("音频源")]
    [SerializeField] private AudioSource audioSource;
    [FieldLabel("打字音效")]
    [SerializeField] private AudioClip typingSound;
    [FieldLabel("音效音量")]
    [SerializeField] private float soundVolume = 0.5f;
    
    [Header("=== 调试设置 ===")]
    [FieldLabel("显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 私有变量
    private string targetText;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    
    // 事件
    public System.Action OnTypingComplete;
    
    void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (playOnStart && textComponent != null && !string.IsNullOrEmpty(textComponent.text))
        {
            StartTypewriter();
        }
    }
    
    void Update()
    {
        // 检测跳过输入
        if (canSkip && isTyping && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            SkipTypewriter();
        }
    }
    
    /// <summary>
    /// 开始打字机效果
    /// </summary>
    public void StartTypewriter()
    {
        if (textComponent == null) return;
        
        string text = textComponent.text;
        if (string.IsNullOrEmpty(text)) return;
        
        StartTypewriter(text);
    }
    
    /// <summary>
    /// 使用指定文本开始打字机效果
    /// </summary>
    public void StartTypewriter(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("SimpleTMPTypewriter: 文本内容为空");
            }
            return;
        }
        
        // 停止之前的动画
        StopTypewriter();
        
        targetText = text;
        textComponent.text = "";
        
        // 开始打字效果
        typingCoroutine = StartCoroutine(TypewriterCoroutine());
        
        if (showDebugInfo)
        {
            Debug.Log($"SimpleTMPTypewriter: 开始打字机效果，文本长度: {targetText.Length}");
        }
    }
    
    /// <summary>
    /// 停止打字效果
    /// </summary>
    public void StopTypewriter()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }
    
    /// <summary>
    /// 跳过打字效果
    /// </summary>
    public void SkipTypewriter()
    {
        if (!isTyping) return;
        
        StopTypewriter();
        if (textComponent != null && !string.IsNullOrEmpty(targetText))
        {
            textComponent.text = targetText;
        }
        OnTypingComplete?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("SimpleTMPTypewriter: 跳过打字机效果");
        }
    }
    
    /// <summary>
    /// 打字机协程
    /// </summary>
    private IEnumerator TypewriterCoroutine()
    {
        isTyping = true;
        
        for (int i = 0; i <= targetText.Length; i++)
        {
            // 更新显示的文本
            textComponent.text = targetText.Substring(0, i);
            
            // 播放音效
            if (i > 0 && i <= targetText.Length)
            {
                PlayTypingSound(targetText[i - 1]);
            }
            
            // 等待下一个字符
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // 打字完成
        isTyping = false;
        OnTypingComplete?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("SimpleTMPTypewriter: 打字机效果完成");
        }
    }
    
    /// <summary>
    /// 播放打字音效
    /// </summary>
    private void PlayTypingSound(char character)
    {
        if (audioSource == null || typingSound == null) return;
        
        // 跳过空格和换行符的音效
        if (character == ' ' || character == '\n' || character == '\r') return;
        
        audioSource.PlayOneShot(typingSound, soundVolume);
    }
    
    /// <summary>
    /// 设置打字速度（运行时调整）
    /// </summary>
    public void SetTypingSpeed(float speed)
    {
        typingSpeed = Mathf.Max(0.001f, speed);
        
        if (showDebugInfo)
        {
            Debug.Log($"SimpleTMPTypewriter: 设置打字速度为 {typingSpeed:F3}");
        }
    }
    
    /// <summary>
    /// 获取当前打字速度
    /// </summary>
    public float GetTypingSpeed() => typingSpeed;
    
    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsTyping => isTyping;
    
    /// <summary>
    /// 获取目标文本
    /// </summary>
    public string TargetText => targetText;
    
    /// <summary>
    /// 立即显示完整文本
    /// </summary>
    public void ShowFullTextImmediately()
    {
        StopTypewriter();
        if (textComponent != null && !string.IsNullOrEmpty(targetText))
        {
            textComponent.text = targetText;
        }
    }
    
    void OnDestroy()
    {
        StopTypewriter();
    }
    
    // 编辑器辅助方法
    #if UNITY_EDITOR
    [ContextMenu("测试打字机效果")]
    private void TestTypewriter()
    {
        if (Application.isPlaying)
        {
            StartTypewriter();
        }
        else
        {
            Debug.Log("请在运行时测试打字机效果");
        }
    }
    
    [ContextMenu("跳过打字机效果")]
    private void TestSkip()
    {
        if (Application.isPlaying)
        {
            SkipTypewriter();
        }
    }
    #endif
}
