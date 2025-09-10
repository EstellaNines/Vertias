using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 放大镜动画进度条控制器
/// 用于显示和控制放大镜圆环动画的进度
/// </summary>
public class MagnifierProgressController : MonoBehaviour
{
    [Header("进度条UI组件")]
    [SerializeField][FieldLabel("进度条父对象")]
    private GameObject progressBarParent;
    
    [SerializeField][FieldLabel("背景RawImage")]
    private RawImage backgroundRawImage;
    
    [SerializeField][FieldLabel("填充Image")]
    private Image fillImage;
    
    [Header("进度文本显示")]
    [SerializeField][FieldLabel("进度百分比文本")]
    private TextMeshProUGUI progressText;
    
    [SerializeField][FieldLabel("剩余时间文本")]
    private TextMeshProUGUI remainingTimeText;
    
    [SerializeField][FieldLabel("已过时间文本")]
    private TextMeshProUGUI elapsedTimeText;
    
    [Header("动画组件引用")]
    [SerializeField][FieldLabel("放大镜动画组件")]
    private MagnifierCircleAnimation magnifierAnimation;
    
    [Header("进度条样式设置")]
    [SerializeField][FieldLabel("填充Image颜色渐变")]
    private bool useGradientColor = true;
    
    [SerializeField][FieldLabel("起始颜色")]
    private Color startColor = Color.red;
    
    [SerializeField][FieldLabel("结束颜色")]
    private Color endColor = Color.green;
    
    [SerializeField][FieldLabel("背景颜色")]
    private Color backgroundColor = Color.gray;
    
    [SerializeField][FieldLabel("进度文本格式")]
    private string progressTextFormat = "{0:F1}%";
    
    [SerializeField][FieldLabel("时间文本格式")]
    private string timeTextFormat = "{0:F1}s";
    
    private bool isSubscribed = false;
    
    private void Start()
    {
        InitializeProgressBar();
        SubscribeToEvents();
    }
    
    private void InitializeProgressBar()
    {
        // 初始化背景RawImage
        if (backgroundRawImage != null)
        {
            backgroundRawImage.color = backgroundColor;
        }
        
        // 初始化填充Image
        if (fillImage != null)
        {
            // 设置为填充类型，从左到右填充
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0; // 从左开始填充
            fillImage.fillAmount = 0f; // 初始填充为0
            
            // 设置初始颜色
            fillImage.color = useGradientColor ? startColor : fillImage.color;
        }
        
        // 初始化文本
        UpdateProgressText(0f);
        UpdateTimeTexts(0f, 0f);
    }
    
    private void SubscribeToEvents()
    {
        if (magnifierAnimation != null && !isSubscribed)
        {
            magnifierAnimation.OnProgressUpdate += OnProgressUpdate;
            magnifierAnimation.OnAnimationStart += OnAnimationStart;
            magnifierAnimation.OnAnimationComplete += OnAnimationComplete;
            magnifierAnimation.OnAnimationLoop += OnAnimationLoop;
            isSubscribed = true;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (magnifierAnimation != null && isSubscribed)
        {
            magnifierAnimation.OnProgressUpdate -= OnProgressUpdate;
            magnifierAnimation.OnAnimationStart -= OnAnimationStart;
            magnifierAnimation.OnAnimationComplete -= OnAnimationComplete;
            magnifierAnimation.OnAnimationLoop -= OnAnimationLoop;
            isSubscribed = false;
        }
    }
    
    #region 事件回调方法
    
    private void OnProgressUpdate(float progress)
    {
        UpdateProgressBar(progress);
        UpdateProgressText(progress * 100f);
        
        if (magnifierAnimation != null)
        {
            float elapsedTime = magnifierAnimation.GetElapsedTime();
            float remainingTime = magnifierAnimation.GetRemainingTime();
            UpdateTimeTexts(elapsedTime, remainingTime);
        }
    }
    
    private void OnAnimationStart()
    {
        Debug.Log("进度条：动画开始");
        if (progressBarParent != null)
        {
            progressBarParent.SetActive(true);
        }
    }
    
    private void OnAnimationComplete()
    {
        Debug.Log("进度条：动画完成");
        UpdateProgressBar(1f);
        UpdateProgressText(100f);
        
        if (magnifierAnimation != null)
        {
            float totalDuration = magnifierAnimation.GetTotalDuration();
            UpdateTimeTexts(totalDuration, 0f);
        }
    }
    
    private void OnAnimationLoop()
    {
        Debug.Log("进度条：动画循环");
        // 循环时重置进度条
        UpdateProgressBar(0f);
        UpdateProgressText(0f);
        UpdateTimeTexts(0f, magnifierAnimation?.GetTotalDuration() ?? 0f);
    }
    
    #endregion
    
    #region UI更新方法
    
    private void UpdateProgressBar(float progress)
    {
        // 更新填充Image的填充量
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
            
            // 根据进度更新颜色
            if (useGradientColor)
            {
                Color currentColor = Color.Lerp(startColor, endColor, progress);
                fillImage.color = currentColor;
            }
        }
    }
    
    private void UpdateProgressText(float percentage)
    {
        if (progressText != null)
        {
            progressText.text = string.Format(progressTextFormat, percentage);
        }
    }
    
    private void UpdateTimeTexts(float elapsed, float remaining)
    {
        if (elapsedTimeText != null)
        {
            elapsedTimeText.text = "已过: " + string.Format(timeTextFormat, elapsed);
        }
        
        if (remainingTimeText != null)
        {
            remainingTimeText.text = "剩余: " + string.Format(timeTextFormat, remaining);
        }
    }
    
    #endregion
    
    #region 公共控制方法
    
    /// <summary>
    /// 设置关联的动画组件
    /// </summary>
    /// <param name="animation">动画组件</param>
    public void SetMagnifierAnimation(MagnifierCircleAnimation animation)
    {
        if (magnifierAnimation != animation)
        {
            UnsubscribeFromEvents();
            magnifierAnimation = animation;
            SubscribeToEvents();
        }
    }
    
    /// <summary>
    /// 显示进度条
    /// </summary>
    public void ShowProgressBar()
    {
        if (progressBarParent != null)
        {
            progressBarParent.SetActive(true);
        }
    }
    
    /// <summary>
    /// 隐藏进度条
    /// </summary>
    public void HideProgressBar()
    {
        if (progressBarParent != null)
        {
            progressBarParent.SetActive(false);
        }
    }
    
    /// <summary>
    /// 设置进度条颜色渐变
    /// </summary>
    /// <param name="start">起始颜色</param>
    /// <param name="end">结束颜色</param>
    public void SetProgressColors(Color start, Color end)
    {
        startColor = start;
        endColor = end;
        useGradientColor = true;
        
        // 如果正在播放动画，立即更新颜色
        if (magnifierAnimation != null && magnifierAnimation.IsAnimating())
        {
            float currentProgress = magnifierAnimation.GetProgress();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(startColor, endColor, currentProgress);
            }
        }
    }
    
    /// <summary>
    /// 设置进度条固定颜色
    /// </summary>
    /// <param name="color">固定颜色</param>
    public void SetProgressColor(Color color)
    {
        useGradientColor = false;
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
    
    /// <summary>
    /// 设置背景颜色
    /// </summary>
    /// <param name="color">背景颜色</param>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        if (backgroundRawImage != null)
        {
            backgroundRawImage.color = backgroundColor;
        }
    }
    
    /// <summary>
    /// 设置文本格式
    /// </summary>
    /// <param name="progressFormat">进度文本格式</param>
    /// <param name="timeFormat">时间文本格式</param>
    public void SetTextFormats(string progressFormat, string timeFormat)
    {
        progressTextFormat = progressFormat;
        timeTextFormat = timeFormat;
        
        // 立即更新当前显示
        if (magnifierAnimation != null)
        {
            float progress = magnifierAnimation.GetProgressPercentage();
            float elapsed = magnifierAnimation.GetElapsedTime();
            float remaining = magnifierAnimation.GetRemainingTime();
            
            UpdateProgressText(progress);
            UpdateTimeTexts(elapsed, remaining);
        }
    }
    
    /// <summary>
    /// 获取当前进度值
    /// </summary>
    /// <returns>进度值（0-1）</returns>
    public float GetCurrentProgress()
    {
        return magnifierAnimation?.GetProgress() ?? 0f;
    }
    
    #endregion
    
    #region Unity生命周期
    
    private void OnValidate()
    {
        // 在编辑器中实时预览颜色变化
        if (Application.isPlaying && fillImage != null && useGradientColor)
        {
            float currentProgress = GetCurrentProgress();
            fillImage.color = Color.Lerp(startColor, endColor, currentProgress);
        }
        
        // 在编辑器中预览背景颜色
        if (!Application.isPlaying && backgroundRawImage != null)
        {
            backgroundRawImage.color = backgroundColor;
        }
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion
}
