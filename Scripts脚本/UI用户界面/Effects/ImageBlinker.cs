using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI Image组件闪烁控制器
/// 支持多种闪烁模式和自定义参数
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageBlinker : MonoBehaviour
{
    [Header("闪烁设置")]
    [FieldLabel("闪烁模式")]
    [SerializeField] private BlinkMode blinkMode = BlinkMode.Alpha;
    
    [FieldLabel("闪烁间隔(秒)")]
    [SerializeField] private float blinkInterval = 0.5f;
    
    [FieldLabel("闪烁持续时间(秒)")]
    [SerializeField] private float blinkDuration = -1f; // -1表示无限循环
    
    [FieldLabel("自动开始")]
    [SerializeField] private bool autoStart = false;
    
    [Header("透明度闪烁")]
    [FieldLabel("最小透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0f;
    
    [FieldLabel("最大透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;
    
    [FieldLabel("渐变速度")]
    [SerializeField] private float fadeSpeed = 2f;
    
    [Header("颜色闪烁")]
    [FieldLabel("原始颜色")]
    [SerializeField] private Color originalColor = Color.white;
    
    [FieldLabel("闪烁颜色")]
    [SerializeField] private Color blinkColor = Color.red;
    
    [Header("尺寸闪烁")]
    [FieldLabel("最小缩放")]
    [Range(0.1f, 2f)]
    [SerializeField] private float minScale = 0.8f;
    
    [FieldLabel("最大缩放")]
    [Range(0.1f, 2f)]
    [SerializeField] private float maxScale = 1.2f;
    
    [Header("调试设置")]
    [FieldLabel("启用调试日志")]
    [SerializeField] private bool enableDebugLog = false;
    
    // 组件引用
    private Image targetImage;
    private RectTransform targetRectTransform;
    
    // 运行时状态
    private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    private Color initialColor;
    private Vector3 initialScale;
    private float blinkStartTime;
    
    // 闪烁模式枚举
    public enum BlinkMode
    {
        [InspectorName("透明度闪烁")] Alpha = 0,
        [InspectorName("颜色闪烁")] Color = 1,
        [InspectorName("尺寸闪烁")] Scale = 2,
        [InspectorName("开关闪烁")] Toggle = 3,
        [InspectorName("组合闪烁")] Combined = 4
    }
    
    #region Unity生命周期
    
    private void Awake()
    {
        // 获取组件引用
        targetImage = GetComponent<Image>();
        targetRectTransform = GetComponent<RectTransform>();
        
        // 记录初始状态
        if (targetImage != null)
        {
            initialColor = targetImage.color;
            originalColor = initialColor; // 自动设置原始颜色
        }
        
        if (targetRectTransform != null)
        {
            initialScale = targetRectTransform.localScale;
        }
        
        LogDebug("ImageBlinker初始化完成");
    }
    
    private void Start()
    {
        if (autoStart)
        {
            StartBlinking();
        }
    }
    
    private void OnValidate()
    {
        // 确保参数合理性
        blinkInterval = Mathf.Max(0.01f, blinkInterval);
        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp01(maxAlpha);
        fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
        minScale = Mathf.Max(0.1f, minScale);
        maxScale = Mathf.Max(0.1f, maxScale);
        
        // 确保最小值不大于最大值
        if (minAlpha > maxAlpha)
        {
            float temp = minAlpha;
            minAlpha = maxAlpha;
            maxAlpha = temp;
        }
        
        if (minScale > maxScale)
        {
            float temp = minScale;
            minScale = maxScale;
            maxScale = temp;
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 开始闪烁
    /// </summary>
    public void StartBlinking()
    {
        if (isBlinking)
        {
            LogDebug("已经在闪烁中，跳过重复启动");
            return;
        }
        
        if (targetImage == null)
        {
            LogError("目标Image组件不存在，无法开始闪烁");
            return;
        }
        
        isBlinking = true;
        blinkStartTime = Time.time;
        
        LogDebug($"开始闪烁 - 模式: {blinkMode}, 间隔: {blinkInterval}s, 持续时间: {(blinkDuration > 0 ? blinkDuration + "s" : "无限")}");
        
        // 根据闪烁模式选择对应的协程
        switch (blinkMode)
        {
            case BlinkMode.Alpha:
                blinkCoroutine = StartCoroutine(AlphaBlinkCoroutine());
                break;
            case BlinkMode.Color:
                blinkCoroutine = StartCoroutine(ColorBlinkCoroutine());
                break;
            case BlinkMode.Scale:
                blinkCoroutine = StartCoroutine(ScaleBlinkCoroutine());
                break;
            case BlinkMode.Toggle:
                blinkCoroutine = StartCoroutine(ToggleBlinkCoroutine());
                break;
            case BlinkMode.Combined:
                blinkCoroutine = StartCoroutine(CombinedBlinkCoroutine());
                break;
        }
    }
    
    /// <summary>
    /// 停止闪烁
    /// </summary>
    public void StopBlinking()
    {
        if (!isBlinking)
        {
            LogDebug("当前没有在闪烁，跳过停止操作");
            return;
        }
        
        isBlinking = false;
        
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // 恢复初始状态
        RestoreInitialState();
        
        LogDebug("闪烁已停止并恢复初始状态");
    }
    
    /// <summary>
    /// 切换闪烁状态
    /// </summary>
    public void ToggleBlinking()
    {
        if (isBlinking)
        {
            StopBlinking();
        }
        else
        {
            StartBlinking();
        }
    }
    
    /// <summary>
    /// 设置闪烁模式
    /// </summary>
    /// <param name="mode">新的闪烁模式</param>
    public void SetBlinkMode(BlinkMode mode)
    {
        bool wasBlinking = isBlinking;
        
        if (wasBlinking)
        {
            StopBlinking();
        }
        
        blinkMode = mode;
        LogDebug($"闪烁模式已设置为: {blinkMode}");
        
        if (wasBlinking)
        {
            StartBlinking();
        }
    }
    
    /// <summary>
    /// 设置闪烁间隔
    /// </summary>
    /// <param name="interval">新的闪烁间隔（秒）</param>
    public void SetBlinkInterval(float interval)
    {
        blinkInterval = Mathf.Max(0.01f, interval);
        LogDebug($"闪烁间隔已设置为: {blinkInterval}s");
    }
    
    /// <summary>
    /// 设置闪烁颜色
    /// </summary>
    /// <param name="color">新的闪烁颜色</param>
    public void SetBlinkColor(Color color)
    {
        blinkColor = color;
        LogDebug($"闪烁颜色已设置为: {blinkColor}");
    }
    
    /// <summary>
    /// 获取当前是否正在闪烁
    /// </summary>
    /// <returns>是否正在闪烁</returns>
    public bool IsBlinking()
    {
        return isBlinking;
    }
    
    /// <summary>
    /// 获取闪烁剩余时间
    /// </summary>
    /// <returns>剩余时间（秒），-1表示无限</returns>
    public float GetRemainingTime()
    {
        if (!isBlinking || blinkDuration <= 0)
        {
            return -1f;
        }
        
        float elapsedTime = Time.time - blinkStartTime;
        return Mathf.Max(0f, blinkDuration - elapsedTime);
    }
    
    #endregion
    
    #region 闪烁协程
    
    /// <summary>
    /// 透明度闪烁协程
    /// </summary>
    private IEnumerator AlphaBlinkCoroutine()
    {
        float elapsedTime = 0f;
        bool increasing = true;
        
        while (isBlinking)
        {
            // 检查持续时间
            if (blinkDuration > 0 && elapsedTime >= blinkDuration)
            {
                break;
            }
            
            // 计算当前透明度
            float currentAlpha = targetImage.color.a;
            float targetAlpha = increasing ? maxAlpha : minAlpha;
            
            // 平滑过渡到目标透明度
            float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            
            Color newColor = targetImage.color;
            newColor.a = newAlpha;
            targetImage.color = newColor;
            
            // 检查是否到达目标值
            if (Mathf.Approximately(newAlpha, targetAlpha))
            {
                increasing = !increasing;
                yield return new WaitForSeconds(blinkInterval);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        StopBlinking();
    }
    
    /// <summary>
    /// 颜色闪烁协程
    /// </summary>
    private IEnumerator ColorBlinkCoroutine()
    {
        float elapsedTime = 0f;
        bool useBlinkColor = false;
        
        while (isBlinking)
        {
            // 检查持续时间
            if (blinkDuration > 0 && elapsedTime >= blinkDuration)
            {
                break;
            }
            
            // 切换颜色
            targetImage.color = useBlinkColor ? blinkColor : originalColor;
            useBlinkColor = !useBlinkColor;
            
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }
        
        StopBlinking();
    }
    
    /// <summary>
    /// 尺寸闪烁协程
    /// </summary>
    private IEnumerator ScaleBlinkCoroutine()
    {
        float elapsedTime = 0f;
        bool increasing = true;
        
        while (isBlinking)
        {
            // 检查持续时间
            if (blinkDuration > 0 && elapsedTime >= blinkDuration)
            {
                break;
            }
            
            // 计算当前缩放
            float currentScale = targetRectTransform.localScale.x;
            float targetScale = increasing ? maxScale : minScale;
            
            // 平滑过渡到目标缩放
            float newScale = Mathf.MoveTowards(currentScale, targetScale, fadeSpeed * Time.deltaTime);
            targetRectTransform.localScale = Vector3.one * newScale;
            
            // 检查是否到达目标值
            if (Mathf.Approximately(newScale, targetScale))
            {
                increasing = !increasing;
                yield return new WaitForSeconds(blinkInterval);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        StopBlinking();
    }
    
    /// <summary>
    /// 开关闪烁协程
    /// </summary>
    private IEnumerator ToggleBlinkCoroutine()
    {
        float elapsedTime = 0f;
        bool visible = true;
        
        while (isBlinking)
        {
            // 检查持续时间
            if (blinkDuration > 0 && elapsedTime >= blinkDuration)
            {
                break;
            }
            
            // 切换可见性
            targetImage.enabled = visible;
            visible = !visible;
            
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }
        
        StopBlinking();
    }
    
    /// <summary>
    /// 组合闪烁协程（同时改变透明度、颜色和尺寸）
    /// </summary>
    private IEnumerator CombinedBlinkCoroutine()
    {
        float elapsedTime = 0f;
        bool phase1 = true; // true为第一阶段，false为第二阶段
        
        while (isBlinking)
        {
            // 检查持续时间
            if (blinkDuration > 0 && elapsedTime >= blinkDuration)
            {
                break;
            }
            
            if (phase1)
            {
                // 第一阶段：最大透明度，闪烁颜色，最大尺寸
                Color newColor = blinkColor;
                newColor.a = maxAlpha;
                targetImage.color = newColor;
                targetRectTransform.localScale = Vector3.one * maxScale;
            }
            else
            {
                // 第二阶段：最小透明度，原始颜色，最小尺寸
                Color newColor = originalColor;
                newColor.a = minAlpha;
                targetImage.color = newColor;
                targetRectTransform.localScale = Vector3.one * minScale;
            }
            
            phase1 = !phase1;
            
            yield return new WaitForSeconds(blinkInterval);
            elapsedTime += blinkInterval;
        }
        
        StopBlinking();
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 恢复初始状态
    /// </summary>
    private void RestoreInitialState()
    {
        if (targetImage != null)
        {
            targetImage.color = initialColor;
            targetImage.enabled = true;
        }
        
        if (targetRectTransform != null)
        {
            targetRectTransform.localScale = initialScale;
        }
    }
    
    /// <summary>
    /// 调试日志
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ImageBlinker] {gameObject.name}: {message}");
        }
    }
    
    /// <summary>
    /// 错误日志
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[ImageBlinker] {gameObject.name}: {message}");
    }
    
    #endregion
    
    #region 上下文菜单
    
    [ContextMenu("开始闪烁")]
    private void ContextMenuStartBlinking()
    {
        StartBlinking();
    }
    
    [ContextMenu("停止闪烁")]
    private void ContextMenuStopBlinking()
    {
        StopBlinking();
    }
    
    [ContextMenu("切换闪烁状态")]
    private void ContextMenuToggleBlinking()
    {
        ToggleBlinking();
    }
    
    [ContextMenu("恢复初始状态")]
    private void ContextMenuRestoreInitialState()
    {
        RestoreInitialState();
    }
    
    #endregion
}
