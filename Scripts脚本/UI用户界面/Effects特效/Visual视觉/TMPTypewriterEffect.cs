using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// TextMeshPro打字机效果 - 字符逐个出现
/// 支持可视文本、音效、跳过功能
/// </summary>
public class TMPTypewriterEffect : MonoBehaviour
{
    [Header("=== 组件引用 ===")]
    [FieldLabel("文本组件")]
    [SerializeField] private TextMeshProUGUI textComponent;
    [FieldLabel("音频源")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("=== 动画设置 ===")]
    [FieldLabel("打字速度")]
    [Tooltip("每个字符显示的间隔时间 - 数值越大打字越慢，数值越小打字越快")]
    [SerializeField] private float typingSpeed = 0.05f;
    [FieldLabel("标点符号延迟")]
    [Tooltip("标点符号额外停顿时间 - 数值越大停顿越久，数值越小停顿越短")]
    [SerializeField] private float punctuationDelay = 0.2f;
    [FieldLabel("是否自动开始")]
    [SerializeField] private bool playOnStart = false;
    [FieldLabel("是否可以跳过")]
    [SerializeField] private bool canSkip = true;
    
    [Header("=== 字符动画效果 ===")]
    [FieldLabel("启用字符动画")]
    [SerializeField] private bool useCharacterAnimation = true;
    [FieldLabel("动画类型")]
    [SerializeField] private AnimationType animationType = AnimationType.FadeIn;
    [FieldLabel("字符动画持续时间")]
    [Tooltip("单个字符动画效果的持续时间 - 数值越大动画越慢，数值越小动画越快")]
    [SerializeField] private float characterAnimationDuration = 0.3f;
    [FieldLabel("字符缓动效果")]
    [SerializeField] private Ease characterEase = Ease.OutBack;
    
    [Header("=== 音效设置 ===")]
    [FieldLabel("打字音效")]
    [SerializeField] private AudioClip typingSound;
    [FieldLabel("音效音量")]
    [SerializeField] private float soundVolume = 0.5f;
    [FieldLabel("是否播放音效")]
    [SerializeField] private bool playSound = true;
    
    [Header("=== 调试信息 ===")]
    [FieldLabel("是否显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 私有变量
    private string fullText;
    private string displayText;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isSkipped = false;
    
    // 字符动画类型
    public enum AnimationType
    {
        FadeIn,         // 淡入
        SlideFromLeft,  // 从左侧滑入
        SlideFromRight, // 从右侧滑入
        ScaleUp,        // 缩放出现
        PopIn           // 弹出效果
    }
    
    // 事件
    public System.Action OnTypingStart;
    public System.Action OnTypingComplete;
    public System.Action OnCharacterTyped;
    
    void Start()
    {
        InitializeComponent();
        
        if (playOnStart && !string.IsNullOrEmpty(fullText))
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
    /// 初始化组件
    /// </summary>
    private void InitializeComponent()
    {
        // 自动获取TextMeshPro组件
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        
        // 自动获取AudioSource组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // 保存初始文本
        if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
        {
            fullText = textComponent.text;
            textComponent.text = "";
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TMPTypewriterEffect初始化完成 - 文本长度: {fullText?.Length}");
        }
    }
    
    /// <summary>
    /// 开始打字机效果
    /// </summary>
    public void StartTypewriter()
    {
        if (string.IsNullOrEmpty(fullText))
        {
            Debug.LogWarning("TMPTypewriterEffect: 没有文本内容可以显示");
            return;
        }
        
        StartTypewriter(fullText);
    }
    
    /// <summary>
    /// 使用指定文本开始打字机效果
    /// </summary>
    /// <param name="text">要显示的文本</param>
    public void StartTypewriter(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TMPTypewriterEffect: 输入文本为空");
            return;
        }
        
        // 停止之前的动画
        StopTypewriter();
        
        fullText = text;
        displayText = "";
        isSkipped = false;
        
        // 清空文本显示
        if (textComponent != null)
        {
            textComponent.text = "";
        }
        
        // 开始打字协程
        typingCoroutine = StartCoroutine(TypewriterCoroutine());
        
        if (showDebugInfo)
        {
            Debug.Log($"开始打字机效果 - 文本: {text.Substring(0, Mathf.Min(20, text.Length))}...");
        }
    }
    
    /// <summary>
    /// 停止打字机效果
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
    /// 跳过打字机效果
    /// </summary>
    public void SkipTypewriter()
    {
        if (!isTyping) return;
        
        isSkipped = true;
        StopTypewriter();
        
        // 直接显示完整文本
        if (textComponent != null)
        {
            textComponent.text = fullText;
        }
        
        OnTypingComplete?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("打字机效果被跳过");
        }
    }
    
    /// <summary>
    /// 打字机协程
    /// </summary>
    private IEnumerator TypewriterCoroutine()
    {
        isTyping = true;
        OnTypingStart?.Invoke();
        
        for (int i = 0; i < fullText.Length; i++)
        {
            if (isSkipped) yield break;
            
            char currentChar = fullText[i];
            displayText += currentChar;
            
            // 更新文本显示
            textComponent.text = displayText;
            
            // 播放字符动画效果
            if (useCharacterAnimation)
            {
                yield return StartCoroutine(PlayCharacterAnimationSafely(i));
            }
            
            // 播放音效
            PlayTypingSound(currentChar);
            
            // 触发字符显示事件
            OnCharacterTyped?.Invoke();
            
            // 计算延迟时间
            float delay = CalculateDelay(currentChar);
            yield return new WaitForSeconds(delay);
        }
        
        // 打字完成
        isTyping = false;
        OnTypingComplete?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("打字机效果完成");
        }
    }
    
    /// <summary>
    /// 安全播放字符动画（带异常处理）
    /// </summary>
    private IEnumerator PlayCharacterAnimationSafely(int charIndex)
    {
        // 预先验证，避免在协程中处理异常
        bool canAnimate = false;
        try
        {
            canAnimate = ValidateCharacterAnimation(charIndex);
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"TMPTypewriterEffect: 字符动画验证失败，禁用字符动画 - {e.Message}");
            }
            useCharacterAnimation = false;
            yield break;
        }
        
        // 如果验证通过，执行动画
        if (canAnimate)
        {
            yield return StartCoroutine(PlayCharacterAnimation(charIndex));
        }
        else if (useCharacterAnimation)
        {
            // 验证失败，禁用字符动画
            useCharacterAnimation = false;
        }
    }
    
    /// <summary>
    /// 播放字符动画效果
    /// </summary>
    private IEnumerator PlayCharacterAnimation(int charIndex)
    {
        if (textComponent == null) yield break;
        
        // 预先检查，避免在try块中使用yield
        if (!ValidateCharacterAnimation(charIndex))
        {
            yield break;
        }
        
        // 获取验证后的数据
        var animationData = GetCharacterAnimationData(charIndex);
        if (animationData == null)
        {
            yield break;
        }
        
        // 执行动画，不在try块中
        yield return StartCoroutine(ExecuteCharacterAnimation(animationData));
    }
    
    /// <summary>
    /// 验证字符动画是否可以执行
    /// </summary>
    private bool ValidateCharacterAnimation(int charIndex)
    {
        try
        {
            // 强制更新文本网格
            textComponent.ForceMeshUpdate();
            
            // 获取字符信息
            TMP_TextInfo textInfo = textComponent.textInfo;
            if (textInfo == null || charIndex >= textInfo.characterCount) return false;
            
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) return false;
            
            // 安全检查顶点索引
            int vertexIndex = charInfo.vertexIndex;
            if (vertexIndex < 0) return false;
            
            // 获取网格信息
            Mesh mesh = textComponent.mesh;
            if (mesh == null) return false;
            
            Vector3[] vertices = mesh.vertices;
            if (vertices == null || vertices.Length < vertexIndex + 4) return false;
            
            return true;
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"TMPTypewriterEffect: 字符动画验证失败 - {e.Message}");
            }
            return false;
        }
    }
    
    /// <summary>
    /// 字符动画数据
    /// </summary>
    private class CharacterAnimationData
    {
        public Vector3[] vertices;
        public Vector3[] originalVertices;
        public int vertexIndex;
        
        public CharacterAnimationData(Vector3[] vertices, Vector3[] originalVertices, int vertexIndex)
        {
            this.vertices = vertices;
            this.originalVertices = originalVertices;
            this.vertexIndex = vertexIndex;
        }
    }
    
    /// <summary>
    /// 获取字符动画数据
    /// </summary>
    private CharacterAnimationData GetCharacterAnimationData(int charIndex)
    {
        try
        {
            TMP_TextInfo textInfo = textComponent.textInfo;
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            int vertexIndex = charInfo.vertexIndex;
            
            Mesh mesh = textComponent.mesh;
            Vector3[] vertices = mesh.vertices;
            
            // 保存初始顶点
            Vector3[] originalVertices = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                if (vertexIndex + i < vertices.Length)
                {
                    originalVertices[i] = vertices[vertexIndex + i];
                }
                else
                {
                    return null;
                }
            }
            
            return new CharacterAnimationData(vertices, originalVertices, vertexIndex);
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"TMPTypewriterEffect: 获取字符动画数据失败 - {e.Message}");
            }
            return null;
        }
    }
    
    /// <summary>
    /// 执行字符动画
    /// </summary>
    private IEnumerator ExecuteCharacterAnimation(CharacterAnimationData data)
    {
        // 根据动画类型设置初始状态
        SetCharacterInitialState(data.vertices, data.vertexIndex, data.originalVertices);
        
        // 安全更新网格
        if (UpdateMeshSafely(data.vertices)) yield break;
        
        // 执行动画
        float elapsedTime = 0f;
        while (elapsedTime < characterAnimationDuration)
        {
            float progress = elapsedTime / characterAnimationDuration;
            float easedProgress = DOVirtual.EasedValue(0, 1, progress, characterEase);
            
            AnimateCharacter(data.vertices, data.vertexIndex, data.originalVertices, easedProgress);
            
            // 安全更新网格
            if (UpdateMeshSafely(data.vertices)) break;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终状态正确
        for (int i = 0; i < 4; i++)
        {
            if (data.vertexIndex + i < data.vertices.Length)
            {
                data.vertices[data.vertexIndex + i] = data.originalVertices[i];
            }
        }
        UpdateMeshSafely(data.vertices);
    }
    
    /// <summary>
    /// 安全更新网格
    /// </summary>
    private bool UpdateMeshSafely(Vector3[] vertices)
    {
        try
        {
            if (textComponent == null || textComponent.mesh == null) return true;
            
            Mesh mesh = textComponent.mesh;
            if (vertices.Length != mesh.vertexCount) return true;
            
            mesh.vertices = vertices;
            textComponent.canvasRenderer.SetMesh(mesh);
            return false;
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"TMPTypewriterEffect: 网格更新失败 - {e.Message}");
            }
            return true; // 返回true表示出错并停止动画
        }
    }
    
    /// <summary>
    /// 设置字符初始状态
    /// </summary>
    private void SetCharacterInitialState(Vector3[] vertices, int vertexIndex, Vector3[] originalVertices)
    {
        Vector3 center = (originalVertices[0] + originalVertices[2]) * 0.5f;
        
        switch (animationType)
        {
            case AnimationType.FadeIn:
                // 淡入效果通过alpha处理，这里不改变顶点
                break;
                
            case AnimationType.SlideFromLeft:
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = originalVertices[i] + Vector3.left * 50f;
                }
                break;
                
            case AnimationType.SlideFromRight:
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = originalVertices[i] + Vector3.right * 50f;
                }
                break;
                
            case AnimationType.ScaleUp:
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = center;
                }
                break;
                
            case AnimationType.PopIn:
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = center;
                }
                break;
        }
    }
    
    /// <summary>
    /// 执行字符动画
    /// </summary>
    private void AnimateCharacter(Vector3[] vertices, int vertexIndex, Vector3[] originalVertices, float progress)
    {
        Vector3 center = (originalVertices[0] + originalVertices[2]) * 0.5f;
        
        switch (animationType)
        {
            case AnimationType.FadeIn:
                // 淡入效果
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = originalVertices[i];
                }
                break;
                
            case AnimationType.SlideFromLeft:
                Vector3 leftOffset = Vector3.left * 50f * (1 - progress);
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = originalVertices[i] + leftOffset;
                }
                break;
                
            case AnimationType.SlideFromRight:
                Vector3 rightOffset = Vector3.right * 50f * (1 - progress);
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = originalVertices[i] + rightOffset;
                }
                break;
                
            case AnimationType.ScaleUp:
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = Vector3.Lerp(center, originalVertices[i], progress);
                }
                break;
                
            case AnimationType.PopIn:
                float scale = progress;
                if (characterEase == Ease.OutBack)
                {
                    scale = progress * (1 + 0.3f * Mathf.Sin(progress * Mathf.PI));
                }
                for (int i = 0; i < 4; i++)
                {
                    vertices[vertexIndex + i] = Vector3.Lerp(center, originalVertices[i], scale);
                }
                break;
        }
    }
    
    /// <summary>
    /// 播放打字音效
    /// </summary>
    private void PlayTypingSound(char character)
    {
        if (!playSound || audioSource == null || typingSound == null) return;
        
        // 跳过空格和换行符的音效
        if (character == ' ' || character == '\n' || character == '\r') return;
        
        audioSource.PlayOneShot(typingSound, soundVolume);
    }
    
    /// <summary>
    /// 计算字符显示延迟
    /// </summary>
    private float CalculateDelay(char character)
    {
        float delay = typingSpeed;
        
        // 标点符号额外延迟
        if (IsPunctuation(character))
        {
            delay += punctuationDelay;
        }
        
        return delay;
    }
    
    /// <summary>
    /// 判断是否为标点符号
    /// </summary>
    private bool IsPunctuation(char character)
    {
        return character == '.' || character == '!' || character == '?' || 
               character == ',' || character == ';' || character == ':' ||
               character == '。' || character == '！' || character == '？' ||
               character == '，' || character == '；' || character == '：';
    }
    
    /// <summary>
    /// 设置文本内容（不立即播放）
    /// </summary>
    public void SetText(string text)
    {
        fullText = text;
        if (textComponent != null)
        {
            textComponent.text = "";
        }
    }
    
    /// <summary>
    /// 获取当前是否正在打字
    /// </summary>
    public bool IsTyping => isTyping;
    
    /// <summary>
    /// 获取完整文本
    /// </summary>
    public string FullText => fullText;
    
    /// <summary>
    /// 设置打字速度（运行时调整）
    /// </summary>
    public void SetTypingSpeed(float speed)
    {
        typingSpeed = Mathf.Max(0.001f, speed);
    }
    
    /// <summary>
    /// 获取当前打字速度
    /// </summary>
    public float GetTypingSpeed() => typingSpeed;
    
    /// <summary>
    /// 立即显示完整文本
    /// </summary>
    public void ShowFullText()
    {
        StopTypewriter();
        if (textComponent != null && !string.IsNullOrEmpty(fullText))
        {
            textComponent.text = fullText;
        }
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
    
    [ContextMenu("设置测试文本")]
    private void SetTestText()
    {
        if (textComponent != null)
        {
            string testText = "Hello World! This is a test for the typewriter effect. 你好世界，这是打字机效果的测试。";
            SetText(testText);
        }
    }
    #endif
}