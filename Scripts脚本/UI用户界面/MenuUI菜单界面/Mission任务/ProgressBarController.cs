using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 进度条控制器
/// 控制Image填充和TMP文本显示，模拟5秒加载过程
/// 
/// 使用说明：
/// 1. 将此脚本挂载到包含进度条的GameObject上
/// 2. 设置 progressBarImage (设置为Fill类型，从左到右填充)
/// 3. 设置 progressText (显示百分比文本的TMP组件)
/// 4. 调用 StartProgress() 开始加载动画
/// </summary>
public class ProgressBarController : MonoBehaviour
{
    [Header("组件设置")]
    [Tooltip("进度条Image组件 - 需要设置为Fill类型，填充方向从左到右")]
    [SerializeField] private Image progressBarImage;
    
    [Tooltip("显示进度百分比的TMP文本组件")]
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("进度设置")]
    [Tooltip("加载总时长（秒） - 默认5秒")]
    [SerializeField] private float loadingDuration = 5f;
    
    [Tooltip("文本更新频率（秒） - 数值越小更新越频繁")]
    [SerializeField] private float textUpdateInterval = 0.1f;
    
    [Tooltip("进度完成后的延迟时间（秒）")]
    [SerializeField] private float completeDelay = 0.5f;
    
    [Header("动画设置")]
    [Tooltip("使用缓动动画 - 让进度条动画更平滑")]
    [SerializeField] private bool useEasing = true;
    
    [Tooltip("缓动类型")]
    [SerializeField] private Ease easeType = Ease.OutQuart;
    
    [Header("文本格式")]
    [Tooltip("进度文本格式 - {0}会被替换为百分比数字")]
    [SerializeField] private string progressFormat = "Loading... {0}%";
    
    [Tooltip("完成时的文本")]
    [SerializeField] private string completeText = "Complete!";
    
    // 事件
    [System.Serializable]
    public class ProgressEvent : UnityEngine.Events.UnityEvent<float> { }
    
    [System.Serializable]
    public class CompleteEvent : UnityEngine.Events.UnityEvent { }
    
    [Header("事件")]
    [Tooltip("进度更新事件 - 参数为当前进度(0-1)")]
    public ProgressEvent OnProgressUpdate;
    
    [Tooltip("加载完成事件")]
    public CompleteEvent OnLoadingComplete;
    
    // 私有变量
    private Coroutine loadingCoroutine;
    private Tween progressTween;
    private Sequence progressSequence;
    private bool isLoading = false;
    private float currentProgress = 0f;
    private Coroutine textUpdateCoroutine;
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 自动查找组件
        if (progressBarImage == null)
            progressBarImage = GetComponentInChildren<Image>();
            
        if (progressText == null)
            progressText = GetComponentInChildren<TextMeshProUGUI>();
            
        // 确保Image设置正确
        SetupProgressBarImage();
    }
    
    private void Start()
    {
        // 初始化进度条
        ResetProgress();
    }
    
    private void OnDestroy()
    {
        // 清理DOTween
        KillTween();
        
        // 停止协程
        StopAllRunningCoroutines();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 开始进度加载
    /// </summary>
    public void StartProgress()
    {
        // 无论当前状态如何，都先停止并清理，确保每次从0开始
        ForceStopInternal();
        Debug.Log("ProgressBarController: 开始进度加载 (重置为0)");
        ResetProgress();
        
        // 开始加载
        if (useEasing)
        {
            StartProgressWithDOTween();
        }
        else
        {
            loadingCoroutine = StartCoroutine(StartProgressWithCoroutine());
        }
    }
    
    /// <summary>
    /// 停止进度加载
    /// </summary>
    public void StopProgress()
    {
        if (!isLoading) return;
        
        Debug.Log("ProgressBarController: 停止进度加载");
        ForceStopInternal();
    }
    
    /// <summary>
    /// 重置进度条
    /// </summary>
    public void ResetProgress()
    {
        currentProgress = 0f;
        
        if (progressBarImage != null)
            progressBarImage.fillAmount = 0f;
            
        if (progressText != null)
            progressText.text = string.Format(progressFormat, 0);
            
        Debug.Log("ProgressBarController: 进度条已重置");
    }
    
    /// <summary>
    /// 设置进度（手动控制）
    /// </summary>
    /// <param name="progress">进度值 0-1</param>
    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        UpdateProgressDisplay(currentProgress);
    }
    
    /// <summary>
    /// 设置加载时长
    /// </summary>
    /// <param name="duration">时长（秒）</param>
    public void SetLoadingDuration(float duration)
    {
        loadingDuration = Mathf.Max(0.1f, duration);
        Debug.Log($"ProgressBarController: 加载时长设置为 {loadingDuration:F1}秒");
    }
    
    /// <summary>
    /// 获取当前进度
    /// </summary>
    /// <returns>当前进度 0-1</returns>
    public float GetCurrentProgress()
    {
        return currentProgress;
    }
    
    /// <summary>
    /// 是否正在加载
    /// </summary>
    /// <returns>加载状态</returns>
    public bool IsLoading()
    {
        return isLoading;
    }
    
    /// <summary>
    /// 重新设置进度条Image的填充属性
    /// </summary>
    public void SetupImageFillProperties()
    {
        SetupProgressBarImage();
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 设置进度条Image的填充属性
    /// </summary>
    private void SetupProgressBarImage()
    {
        if (progressBarImage == null)
        {
            Debug.LogWarning("ProgressBarController: progressBarImage 未设置");
            return;
        }
        
        // 设置Image类型为Filled
        progressBarImage.type = Image.Type.Filled;
        
        // 设置填充方式为水平填充
        progressBarImage.fillMethod = Image.FillMethod.Horizontal;
        
        // 设置填充原点为左侧 (0 = 左, 1 = 右)
        progressBarImage.fillOrigin = 0;
        
        // 设置顺时针填充为false（从左到右）
        progressBarImage.fillClockwise = true;
        
        // 初始化填充量为0
        progressBarImage.fillAmount = 0f;
        
        Debug.Log($"ProgressBarController: Image填充设置完成 - Type: {progressBarImage.type}, FillMethod: {progressBarImage.fillMethod}, FillOrigin: {progressBarImage.fillOrigin}");
    }
    
    /// <summary>
    /// 使用DOTween实现进度动画
    /// </summary>
    private void StartProgressWithDOTween()
    {
        isLoading = true;
        
        // 创建DOTween序列
        progressSequence = DOTween.Sequence().SetTarget(this);
        
        // 进度条填充动画
        if (progressBarImage != null)
        {
            KillTween();
            DOTween.Kill(progressBarImage, complete: false);
            progressTween = progressBarImage.DOFillAmount(1f, loadingDuration)
                .SetEase(easeType)
                .OnUpdate(() => {
                    currentProgress = progressBarImage.fillAmount;
                    UpdateProgressText(currentProgress);
                    OnProgressUpdate?.Invoke(currentProgress);
                });
            
            progressSequence.Append(progressTween);
        }
        
        // 完成后的处理
        progressSequence.OnComplete(() => {
            if (this != null && isActiveAndEnabled) StartCoroutine(OnProgressCompleteCoroutine());
        });
        
        // 开始文本更新协程
        if (textUpdateCoroutine != null)
        {
            StopCoroutine(textUpdateCoroutine);
        }
        textUpdateCoroutine = StartCoroutine(UpdateProgressTextCoroutine());
    }
    
    /// <summary>
    /// 使用协程实现进度动画
    /// </summary>
    private IEnumerator StartProgressWithCoroutine()
    {
        isLoading = true;
        float elapsed = 0f;
        
        while (elapsed < loadingDuration && isLoading)
        {
            elapsed += Time.deltaTime;
            currentProgress = Mathf.Clamp01(elapsed / loadingDuration);
            
            UpdateProgressDisplay(currentProgress);
            OnProgressUpdate?.Invoke(currentProgress);
            
            yield return null;
        }
        
        // 确保完成时进度为100%
        if (isLoading)
        {
            currentProgress = 1f;
            UpdateProgressDisplay(currentProgress);
            OnProgressUpdate?.Invoke(currentProgress);
            
            yield return StartCoroutine(OnProgressCompleteCoroutine());
        }
    }
    
    /// <summary>
    /// 更新进度条和文本显示
    /// </summary>
    private void UpdateProgressDisplay(float progress)
    {
        if (progressBarImage != null)
            progressBarImage.fillAmount = progress;
            
        UpdateProgressText(progress);
    }
    
    /// <summary>
    /// 更新进度文本
    /// </summary>
    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100);
            progressText.text = string.Format(progressFormat, percentage);
        }
    }
    
    /// <summary>
    /// 文本更新协程（用于DOTween模式）
    /// </summary>
    private IEnumerator UpdateProgressTextCoroutine()
    {
        while (isLoading && currentProgress < 1f)
        {
            UpdateProgressText(currentProgress);
            yield return new WaitForSeconds(textUpdateInterval);
        }
    }
    
    /// <summary>
    /// 进度完成后的处理
    /// </summary>
    private IEnumerator OnProgressCompleteCoroutine()
    {
        // 显示完成文本
        if (progressText != null)
            progressText.text = completeText;
        
        // 等待延迟时间
        yield return new WaitForSeconds(completeDelay);
        
        // 标记完成
        isLoading = false;
        
        // 触发完成事件
        OnLoadingComplete?.Invoke();
        
        Debug.Log("ProgressBarController: 进度加载完成");
    }
    
    private void ForceStopInternal()
    {
        isLoading = false;
        KillTween();
        StopAllRunningCoroutines();
    }
    
    private void KillTween()
    {
        if (progressTween != null)
        {
            progressTween.Kill();
            progressTween = null;
        }
        DOTween.Kill(progressBarImage, complete: false);
        if (progressSequence != null)
        {
            progressSequence.Kill();
            progressSequence = null;
        }
        DOTween.Kill(this);
    }
    
    private void StopAllRunningCoroutines()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        if (textUpdateCoroutine != null)
        {
            StopCoroutine(textUpdateCoroutine);
            textUpdateCoroutine = null;
        }
    }
    
    #endregion
    
    #region Unity编辑器测试
    
    #if UNITY_EDITOR
    [ContextMenu("开始进度测试")]
    private void TestStartProgress()
    {
        if (Application.isPlaying)
        {
            StartProgress();
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("停止进度测试")]
    private void TestStopProgress()
    {
        if (Application.isPlaying)
        {
            StopProgress();
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("重置进度")]
    private void TestResetProgress()
    {
        if (Application.isPlaying)
        {
            ResetProgress();
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("设置50%进度")]
    private void TestSetHalfProgress()
    {
        if (Application.isPlaying)
        {
            SetProgress(0.5f);
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("修复Image填充设置")]
    private void TestSetupImageFill()
    {
        SetupProgressBarImage();
        Debug.Log("已重新设置Image填充属性");
    }
    
    [ContextMenu("检查组件状态")]
    private void CheckComponentStatus()
    {
        Debug.Log($"ProgressBarController 状态检查:");
        Debug.Log($"- progressBarImage: {(progressBarImage != null ? "已设置" : "未设置")}");
        Debug.Log($"- progressText: {(progressText != null ? "已设置" : "未设置")}");
        Debug.Log($"- loadingDuration: {loadingDuration:F1}秒");
        Debug.Log($"- isLoading: {isLoading}");
        Debug.Log($"- currentProgress: {currentProgress:F2} ({currentProgress * 100:F0}%)");
        
        if (progressBarImage != null)
        {
            Debug.Log($"- Image.fillAmount: {progressBarImage.fillAmount:F2}");
            Debug.Log($"- Image.type: {progressBarImage.type}");
            Debug.Log($"- Image.fillMethod: {progressBarImage.fillMethod}");
            Debug.Log($"- Image.fillOrigin: {progressBarImage.fillOrigin}");
            Debug.Log($"- Image.fillClockwise: {progressBarImage.fillClockwise}");
            
            // 检查填充设置是否正确
            bool isCorrectSetup = progressBarImage.type == Image.Type.Filled && 
                                progressBarImage.fillMethod == Image.FillMethod.Horizontal && 
                                progressBarImage.fillOrigin == 0;
            Debug.Log($"- 填充设置是否正确: {(isCorrectSetup ? "是" : "否")}");
            
            if (!isCorrectSetup)
            {
                Debug.LogWarning("Image填充设置不正确！请调用SetupImageFillProperties()方法或右键菜单'修复Image填充设置'");
            }
        }
    }
    #endif
    
    #endregion
}
