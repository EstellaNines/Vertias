using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 任务系统加载界面控制器
/// 按F键开始3秒加载过程，包含进度、圆环动画、文本打字机效果
/// </summary>
public class MissionLoadingController : MonoBehaviour
{
    [Header("=== 界面控制 ===")]
    [FieldLabel("加载界面面板")]
    [SerializeField] private GameObject loadingPanel; // 加载界面面板
    [FieldLabel("打开按键")]
    [SerializeField] private KeyCode openKey = KeyCode.F; // 打开界面的按键
    [FieldLabel("是否完成后自动关闭")]
    [SerializeField] private bool closeOnComplete = true; // 加载完成后是否自动关闭
    
    [Header("=== 进度显示 ===")]
    [FieldLabel("进度文本组件")]
    [SerializeField] private TextMeshProUGUI progressText; // 进度文本组件
    [FieldLabel("加载时长")]
    [Tooltip("整个加载过程的持续时间（秒） - 数值越大加载越慢，数值越小加载越快")]
    [SerializeField] private float loadingDuration = 3f; // 加载时长（秒）
    [FieldLabel("进度格式")]
    [SerializeField] private string progressFormat = "{0}%"; // 进度格式化字符串
    
    [Header("=== 圆环加载器 ===")]
    [FieldLabel("圆环加载器")]
    [SerializeField] private CircularLoadingDots circularLoader; // 圆环加载器
    
    [Header("=== CMD扫描效果 ===")]
    [FieldLabel("CMD扫描器")]
    [SerializeField] private CMDScannerEffect cmdScanner; // CMD扫描效果
    
    [Header("=== 打字机效果 ===")]
    [FieldLabel("标题打字机")]
    [SerializeField] private TMPTypewriterEffect loadingTitleTypewriter; // 标题打字机（复杂版）
    [FieldLabel("描述打字机")]
    [SerializeField] private TMPTypewriterEffect descriptionTypewriter; // 描述打字机（复杂版）
    [FieldLabel("标题简单打字机")]
    [SerializeField] private SimpleTMPTypewriter simpleTitleTypewriter; // 标题打字机（简单版）
    [FieldLabel("描述简单打字机")]
    [SerializeField] private SimpleTMPTypewriter simpleDescriptionTypewriter; // 描述打字机（简单版）
    [FieldLabel("标题开始延迟")]
    [Tooltip("标题打字机效果开始前的延迟时间（秒） - 数值越大延迟越久，数值越小延迟越短")]
    [SerializeField] private float titleStartDelay = 0f; // 标题开始延迟
    [FieldLabel("描述开始延迟")]
    [Tooltip("描述打字机效果开始前的延迟时间（秒） - 数值越大延迟越久，数值越小延迟越短")]
    [SerializeField] private float descriptionStartDelay = 0.5f; // 描述开始延迟
    
    [Header("=== 音效设置 ===")]
    [FieldLabel("音频源")]
    [SerializeField] private AudioSource audioSource;
    [FieldLabel("开始加载音效")]
    [SerializeField] private AudioClip loadingStartSound; // 开始加载音效
    [FieldLabel("完成加载音效")]
    [SerializeField] private AudioClip loadingCompleteSound; // 完成加载音效
    [FieldLabel("音效音量")]
    [SerializeField] private float soundVolume = 0.5f;
    
    [Header("=== 调试设置 ===")]
    [FieldLabel("是否显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 私有变量
    private bool isLoading = false;
    private bool isInterfaceOpen = false;
    private Coroutine loadingCoroutine;
    private Tween progressTween;
    
    // 事件
    public System.Action OnLoadingStart;
    public System.Action OnLoadingComplete;
    public System.Action<float> OnProgressUpdate; // 参数为进度百分比(0-100)
    
    void Start()
    {
        InitializeComponents();
        
        // 初始时隐藏界面
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // 检测按键输入
        if (Input.GetKeyDown(openKey))
        {
            if (!isInterfaceOpen && !isLoading)
            {
                OpenLoadingInterface();
            }
            else if (isInterfaceOpen && !isLoading)
            {
                CloseLoadingInterface();
            }
        }
        
        // ESC键强制关闭
        if (Input.GetKeyDown(KeyCode.Escape) && isInterfaceOpen)
        {
            ForceCloseInterface();
        }
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
    {
        // 自动获取组件
        if (loadingPanel == null)
        {
            loadingPanel = gameObject;
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // 检查必要组件
        if (progressText == null)
        {
            Debug.LogWarning("MissionLoadingController: 进度文本组件未分配");
        }
        
        if (circularLoader == null)
        {
            Debug.LogWarning("MissionLoadingController: 圆环加载器组件未分配");
        }
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 组件初始化完成");
        }
    }
    
    /// <summary>
    /// 打开加载界面
    /// </summary>
    public void OpenLoadingInterface()
    {
        if (isInterfaceOpen || isLoading) return;
        
        isInterfaceOpen = true;
        
        // 显示界面
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        
        // 重置所有组件状态
        ResetAllComponents();
        
        // 开始加载过程
        StartLoadingProcess();
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 打开加载界面");
        }
    }
    
    /// <summary>
    /// 关闭加载界面
    /// </summary>
    public void CloseLoadingInterface()
    {
        if (!isInterfaceOpen) return;
        
        // 停止所有加载过程
        StopLoadingProcess();
        
        isInterfaceOpen = false;
        
        // 隐藏界面
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 关闭加载界面");
        }
    }
    
    /// <summary>
    /// 强制关闭界面
    /// </summary>
    public void ForceCloseInterface()
    {
        StopLoadingProcess();
        isInterfaceOpen = false;
        isLoading = false;
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 强制关闭界面");
        }
    }
    
    /// <summary>
    /// 重置所有组件状态
    /// </summary>
    private void ResetAllComponents()
    {
        // 重置进度文本
        if (progressText != null)
        {
            progressText.text = string.Format(progressFormat, 0);
        }
        
        // 停止圆环动画
        if (circularLoader != null)
        {
            circularLoader.StopLoading();
        }
        
        // 停止CMD扫描效果
        if (cmdScanner != null)
        {
            cmdScanner.StopScanner();
        }
        
        // 停止打字机效果
        if (loadingTitleTypewriter != null)
        {
            loadingTitleTypewriter.StopTypewriter();
        }
        
        if (descriptionTypewriter != null)
        {
            descriptionTypewriter.StopTypewriter();
        }
    }
    
    /// <summary>
    /// 开始加载过程
    /// </summary>
    public void StartLoadingProcess()
    {
        if (isLoading) return;
        
        isLoading = true;
        
        // 播放开始音效
        PlaySound(loadingStartSound);
        
        // 开始圆环动画
        if (circularLoader != null)
        {
            circularLoader.StartLoading();
        }
        
        // 启动CMD扫描效果
        if (cmdScanner != null)
        {
            cmdScanner.StartScanner();
        }
        
        // 开始进度动画
        StartProgressAnimation();
        
        // 开始打字机效果
        StartTypewriterEffects();
        
        // 开始加载协程
        loadingCoroutine = StartCoroutine(LoadingCoroutine());
        
        OnLoadingStart?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 开始加载过程");
        }
    }
    
    /// <summary>
    /// 停止加载过程
    /// </summary>
    public void StopLoadingProcess()
    {
        if (!isLoading) return;
        
        isLoading = false;
        
        // 停止协程
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        // 停止进度动画
        if (progressTween != null)
        {
            progressTween.Kill();
            progressTween = null;
        }
        
        // 停止圆环动画
        if (circularLoader != null)
        {
            circularLoader.StopLoading();
        }
        
        // 停止打字机效果
        if (loadingTitleTypewriter != null)
        {
            loadingTitleTypewriter.StopTypewriter();
        }
        
        if (descriptionTypewriter != null)
        {
            descriptionTypewriter.StopTypewriter();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 停止加载过程");
        }
    }
    
    /// <summary>
    /// 开始进度动画
    /// </summary>
    private void StartProgressAnimation()
    {
        if (progressText == null) return;
        
        // 使用DOTween创建进度动画
        progressTween = DOVirtual.Float(0f, 100f, loadingDuration, (value) => {
            // 更新进度文本
            int progressValue = Mathf.RoundToInt(value);
            progressText.text = string.Format(progressFormat, progressValue);
            
            // 触发进度更新事件
            OnProgressUpdate?.Invoke(value);
            
        }).SetEase(Ease.OutQuart);
    }
    
    /// <summary>
    /// 开始打字机效果
    /// </summary>
    private void StartTypewriterEffects()
    {
        // 启动标题打字机效果（优先使用简单版）
        if (simpleTitleTypewriter != null)
        {
            // 先调整速度
            float titleDuration = loadingDuration - titleStartDelay;
            AdjustSimpleTypewriterSpeed(simpleTitleTypewriter, titleDuration);
            
            // 延迟启动
            DOVirtual.DelayedCall(titleStartDelay, () => {
                if (isLoading && simpleTitleTypewriter != null)
                {
                    simpleTitleTypewriter.StartTypewriter();
                }
            });
        }
        else if (loadingTitleTypewriter != null)
        {
            // 使用复杂版作为备选
            float titleDuration = loadingDuration - titleStartDelay;
            AdjustTypewriterSpeed(loadingTitleTypewriter, titleDuration);
            
            DOVirtual.DelayedCall(titleStartDelay, () => {
                if (isLoading && loadingTitleTypewriter != null)
                {
                    loadingTitleTypewriter.StartTypewriter();
                }
            });
        }
        
        // 启动描述打字机效果（优先使用简单版）
        if (simpleDescriptionTypewriter != null)
        {
            // 先调整速度
            float descriptionDuration = loadingDuration - descriptionStartDelay;
            AdjustSimpleTypewriterSpeed(simpleDescriptionTypewriter, descriptionDuration);
            
            // 延迟启动
            DOVirtual.DelayedCall(descriptionStartDelay, () => {
                if (isLoading && simpleDescriptionTypewriter != null)
                {
                    simpleDescriptionTypewriter.StartTypewriter();
                }
            });
        }
        else if (descriptionTypewriter != null)
        {
            // 使用复杂版作为备选
            float descriptionDuration = loadingDuration - descriptionStartDelay;
            AdjustTypewriterSpeed(descriptionTypewriter, descriptionDuration);
            
            DOVirtual.DelayedCall(descriptionStartDelay, () => {
                if (isLoading && descriptionTypewriter != null)
                {
                    descriptionTypewriter.StartTypewriter();
                }
            });
        }
    }
    
    /// <summary>
    /// 调整打字机速度以在指定时间内完成
    /// </summary>
    private void AdjustTypewriterSpeed(TMPTypewriterEffect typewriter, float targetDuration)
    {
        if (typewriter == null || targetDuration <= 0) return;
        
        // 获取文本组件
        TextMeshProUGUI textComp = typewriter.GetComponent<TextMeshProUGUI>();
        if (textComp == null) return;
        
        // 获取文本内容
        string fullText = textComp.text;
        if (string.IsNullOrEmpty(fullText)) return;
        
        // 计算需要的速度（每个字符的间隔时间）
        float requiredSpeed = targetDuration / fullText.Length;
        
        // 设置速度（确保不会太快或太慢）
        requiredSpeed = Mathf.Clamp(requiredSpeed, 0.005f, 0.5f);
        
        // 设置打字机速度
        typewriter.SetTypingSpeed(requiredSpeed);
        
        if (showDebugInfo)
        {
            Debug.Log($"调整打字机速度: 文本=\"{fullText.Substring(0, Mathf.Min(20, fullText.Length))}...\", 长度={fullText.Length}, 目标时间={targetDuration:F2}s, 速度={requiredSpeed:F3}s/字符");
        }
    }
    
    /// <summary>
    /// 调整简单打字机速度以在指定时间内完成
    /// </summary>
    private void AdjustSimpleTypewriterSpeed(SimpleTMPTypewriter typewriter, float targetDuration)
    {
        if (typewriter == null || targetDuration <= 0) return;
        
        // 获取文本组件
        TextMeshProUGUI textComp = typewriter.GetComponent<TextMeshProUGUI>();
        if (textComp == null) return;
        
        // 获取文本内容
        string fullText = textComp.text;
        if (string.IsNullOrEmpty(fullText)) return;
        
        // 计算需要的速度（每个字符的间隔时间）
        float requiredSpeed = targetDuration / fullText.Length;
        
        // 设置速度（确保不会太快或太慢）
        requiredSpeed = Mathf.Clamp(requiredSpeed, 0.005f, 0.5f);
        
        // 设置打字机速度
        typewriter.SetTypingSpeed(requiredSpeed);
        
        if (showDebugInfo)
        {
            Debug.Log($"调整简单打字机速度: 文本=\"{fullText.Substring(0, Mathf.Min(20, fullText.Length))}...\", 长度={fullText.Length}, 目标时间={targetDuration:F2}s, 速度={requiredSpeed:F3}s/字符");
        }
    }
    
    /// <summary>
    /// 加载协程
    /// </summary>
    private IEnumerator LoadingCoroutine()
    {
        // 等待加载完成
        yield return new WaitForSeconds(loadingDuration);
        
        // 确保所有打字机效果都完成
        ForceCompleteAllTypewriters();
        
        // 加载完成
        OnLoadingComplete?.Invoke();
        
        // 播放完成音效
        PlaySound(loadingCompleteSound);
        
        if (showDebugInfo)
        {
            Debug.Log("MissionLoadingController: 加载完成");
        }
        
        // 短暂延迟后关闭界面
        if (closeOnComplete)
        {
            yield return new WaitForSeconds(0.5f);
            CloseLoadingInterface();
        }
        
        isLoading = false;
    }
    
    /// <summary>
    /// 强制完成所有打字机效果
    /// </summary>
    private void ForceCompleteAllTypewriters()
    {
        // 强制完成标题打字机
        if (simpleTitleTypewriter != null && simpleTitleTypewriter.IsTyping)
        {
            simpleTitleTypewriter.SkipTypewriter();
            if (showDebugInfo)
            {
                Debug.Log("强制完成简单标题打字机效果");
            }
        }
        else if (loadingTitleTypewriter != null && loadingTitleTypewriter.IsTyping)
        {
            loadingTitleTypewriter.SkipTypewriter();
            if (showDebugInfo)
            {
                Debug.Log("强制完成标题打字机效果");
            }
        }
        
        // 强制完成描述打字机
        if (simpleDescriptionTypewriter != null && simpleDescriptionTypewriter.IsTyping)
        {
            simpleDescriptionTypewriter.SkipTypewriter();
            if (showDebugInfo)
            {
                Debug.Log("强制完成简单描述打字机效果");
            }
        }
        else if (descriptionTypewriter != null && descriptionTypewriter.IsTyping)
        {
            descriptionTypewriter.SkipTypewriter();
            if (showDebugInfo)
            {
                Debug.Log("强制完成描述打字机效果");
            }
        }
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }
    
    /// <summary>
    /// 设置加载时长
    /// </summary>
    public void SetLoadingDuration(float duration)
    {
        loadingDuration = Mathf.Max(0.1f, duration);
    }
    
    /// <summary>
    /// 设置进度格式
    /// </summary>
    public void SetProgressFormat(string format)
    {
        progressFormat = format;
    }
    
    /// <summary>
    /// 检查是否正在加载
    /// </summary>
    public bool IsLoading => isLoading;
    
    /// <summary>
    /// 检查界面是否打开
    /// </summary>
    public bool IsInterfaceOpen => isInterfaceOpen;
    
    /// <summary>
    /// 获取当前进度（0-100）
    /// </summary>
    public float CurrentProgress
    {
        get
        {
            if (progressTween != null)
            {
                return progressTween.ElapsedPercentage() * 100f;
            }
            return isLoading ? 0f : 100f;
        }
    }
    
    void OnDestroy()
    {
        // 清理DOTween
        if (progressTween != null)
        {
            progressTween.Kill();
        }
        
        // 停止协程
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
    }
    
    // 编辑器辅助方法
    #if UNITY_EDITOR
    [ContextMenu("测试开始加载")]
    private void TestStartLoading()
    {
        if (Application.isPlaying)
        {
            OpenLoadingInterface();
        }
    }
    
    [ContextMenu("测试停止加载")]
    private void TestStopLoading()
    {
        if (Application.isPlaying)
        {
            ForceCloseInterface();
        }
    }
    
    [ContextMenu("重置组件")]
    private void TestResetComponents()
    {
        if (Application.isPlaying)
        {
            ResetAllComponents();
        }
    }
    
    [ContextMenu("调试文本长度")]
    private void DebugTextLengths()
    {
        if (loadingTitleTypewriter != null)
        {
            TextMeshProUGUI titleText = loadingTitleTypewriter.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                Debug.Log($"标题文本: \"{titleText.text}\" (长度: {titleText.text.Length})");
            }
        }
        
        if (descriptionTypewriter != null)
        {
            TextMeshProUGUI descText = descriptionTypewriter.GetComponent<TextMeshProUGUI>();
            if (descText != null)
            {
                Debug.Log($"描述文本: \"{descText.text}\" (长度: {descText.text.Length})");
            }
        }
        
        Debug.Log($"加载时长: {loadingDuration}秒");
        Debug.Log($"标题延迟: {titleStartDelay}秒, 可用时间: {loadingDuration - titleStartDelay}秒");
        Debug.Log($"描述延迟: {descriptionStartDelay}秒, 可用时间: {loadingDuration - descriptionStartDelay}秒");
    }
    #endif
}
