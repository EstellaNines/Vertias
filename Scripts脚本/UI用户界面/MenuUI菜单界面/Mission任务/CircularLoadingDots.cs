using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 环形圆点加载动画
/// 使用DOTween实现20个圆点的环形加载效果
/// </summary>
public class CircularLoadingDots : MonoBehaviour
{
    [Header("=== 圆点设置 ===")]
    [FieldLabel("圆点预制体")]
    [SerializeField] private GameObject dotPrefab; // 圆点预制体
    [FieldLabel("圆点数量")]
    [SerializeField] private int dotCount = 20; // 圆点数量
    [FieldLabel("环形半径")]
    [SerializeField] private float radius = 100f; // 环形半径
    [FieldLabel("圆点大小")]
    [SerializeField] private float dotSize = 10f; // 圆点大小
    
    [Header("=== 动画设置 ===")]
    [FieldLabel("总持续时间")]
    [Tooltip("完整动画周期的总时长（秒）")]
    [SerializeField] private float totalDuration = 5f; // 总动画时长
    [FieldLabel("放大倍数")]
    [SerializeField] private float scaleMultiplier = 3f; // 放大倍数
    [FieldLabel("单点缩放时长")]
    [Tooltip("单个圆点的缩放动画持续时间")]
    [SerializeField] private float singleDotScaleDuration = 0.3f; // 单个圆点缩放时长
    [FieldLabel("缩放缓动类型")]
    [SerializeField] private Ease scaleEase = Ease.OutBack; // 缩放缓动类型
    [FieldLabel("自动开始")]
    [SerializeField] private bool autoStart = true; // 自动开始
    [FieldLabel("循环播放")]
    [SerializeField] private bool loop = true; // 是否循环
    
    [Header("=== 外观设置 ===")]
    [FieldLabel("正常状态颜色")]
    [SerializeField] private Color normalColor = Color.white; // 正常状态颜色
    [FieldLabel("激活状态颜色")]
    [SerializeField] private Color activeColor = Color.cyan; // 激活状态颜色
    [FieldLabel("使用颜色变化")]
    [SerializeField] private bool useColorChange = true; // 是否使用颜色变化
    
    [Header("=== 调试设置 ===")]
    [FieldLabel("显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 私有变量
    private List<Transform> dots = new List<Transform>();
    private List<Image> dotImages = new List<Image>();
    private Sequence loadingSequence;
    private bool isLoading = false;
    private int currentDotIndex = 0;
    
    // 动画计算属性
    private float DotInterval => totalDuration / dotCount; // 每个圆点的间隔时间
    private float EffectiveDotDuration => Mathf.Min(singleDotScaleDuration, DotInterval * 0.8f); // 有效的单点动画时长
    
    // 事件
    public System.Action OnLoadingStart;
    public System.Action OnLoadingComplete;
    public System.Action<int> OnDotActivated; // 参数为圆点索引
    
    void Start()
    {
        InitializeDots();
        
        if (autoStart)
        {
            StartLoading();
        }
    }
    
    void OnDestroy()
    {
        // 清理DOTween序列
        if (loadingSequence != null)
        {
            loadingSequence.Kill();
        }
    }
    
    /// <summary>
    /// 初始化圆点
    /// </summary>
    private void InitializeDots()
    {
        // 清除现有圆点
        ClearDots();
        
        // 检查预制体
        if (dotPrefab == null)
        {
            CreateDefaultDotPrefab();
        }
        
        // 创建圆点
        for (int i = 0; i < dotCount; i++)
        {
            CreateDot(i);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"CircularLoadingDots: 创建了 {dotCount} 个圆点");
        }
    }
    
    /// <summary>
    /// 创建默认圆点预制体
    /// </summary>
    private void CreateDefaultDotPrefab()
    {
        // 创建临时GameObject作为预制体
        GameObject tempDot = new GameObject("DefaultDot");
        tempDot.transform.SetParent(transform, false);
        
        // 添加Image组件
        Image dotImage = tempDot.AddComponent<Image>();
        dotImage.color = normalColor;
        
        // 设置为圆形
        dotImage.sprite = CreateCircleSprite();
        
        // 设置RectTransform
        RectTransform rectTransform = tempDot.GetComponent<RectTransform>();
        rectTransform.sizeDelta = Vector2.one * dotSize;
        
        dotPrefab = tempDot;
        
        if (showDebugInfo)
        {
            Debug.Log("CircularLoadingDots: 创建了默认圆点预制体");
        }
    }
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // 创建一个简单的白色圆形纹理
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float radius = textureSize / 2f - 2f;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * textureSize + x] = distance <= radius ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f);
    }
    
    /// <summary>
    /// 创建单个圆点
    /// </summary>
    private void CreateDot(int index)
    {
        // 实例化圆点
        GameObject dot = Instantiate(dotPrefab, transform);
        dot.name = $"Dot_{index:D2}";
        
        // 计算位置
        float angle = (360f / dotCount) * index;
        float radian = angle * Mathf.Deg2Rad;
        Vector3 position = new Vector3(
            Mathf.Cos(radian) * radius,
            Mathf.Sin(radian) * radius,
            0f
        );
        
        // 设置位置
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = Vector2.one * dotSize;
        
        // 获取Image组件
        Image dotImage = dot.GetComponent<Image>();
        if (dotImage != null)
        {
            dotImage.color = normalColor;
            dotImages.Add(dotImage);
        }
        
        // 添加到列表
        dots.Add(dot.transform);
        
        if (showDebugInfo)
        {
            Debug.Log($"创建圆点 {index}: 角度={angle:F1}°, 位置={position}");
        }
    }
    
    /// <summary>
    /// 清除所有圆点
    /// </summary>
    private void ClearDots()
    {
        // 停止动画
        StopLoading();
        
        // 销毁现有圆点
        foreach (Transform dot in dots)
        {
            if (dot != null)
            {
                DestroyImmediate(dot.gameObject);
            }
        }
        
        dots.Clear();
        dotImages.Clear();
    }
    
    /// <summary>
    /// 开始加载动画
    /// </summary>
    public void StartLoading()
    {
        if (isLoading) return;
        
        if (dots.Count == 0)
        {
            Debug.LogWarning("CircularLoadingDots: 没有圆点可以动画");
            return;
        }
        
        isLoading = true;
        currentDotIndex = 0;
        
        // 重置所有圆点状态
        ResetAllDots();
        
        // 创建动画序列
        CreateLoadingSequence();
        
        OnLoadingStart?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("CircularLoadingDots: 开始加载动画");
        }
    }
    
    /// <summary>
    /// 停止加载动画
    /// </summary>
    public void StopLoading()
    {
        if (!isLoading) return;
        
        isLoading = false;
        
        // 停止DOTween序列
        if (loadingSequence != null)
        {
            loadingSequence.Kill();
            loadingSequence = null;
        }
        
        // 重置所有圆点
        ResetAllDots();
        
        if (showDebugInfo)
        {
            Debug.Log("CircularLoadingDots: 停止加载动画");
        }
    }
    
    /// <summary>
    /// 重置所有圆点状态
    /// </summary>
    private void ResetAllDots()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] != null)
            {
                // 重置缩放
                dots[i].localScale = Vector3.one;
                
                // 重置颜色
                if (useColorChange && i < dotImages.Count && dotImages[i] != null)
                {
                    dotImages[i].color = normalColor;
                }
            }
        }
    }
    
    /// <summary>
    /// 创建加载动画序列
    /// </summary>
    private void CreateLoadingSequence()
    {
        loadingSequence = DOTween.Sequence();
        
        float currentTime = 0f;
        
        // 为每个圆点添加动画，确保总时长为totalDuration
        for (int i = 0; i < dots.Count; i++)
        {
            int dotIndex = i; // 闭包变量
            float dotStartTime = i * DotInterval;
            
            // 添加到指定时间点的延迟
            if (dotStartTime > currentTime)
            {
                loadingSequence.AppendInterval(dotStartTime - currentTime);
                currentTime = dotStartTime;
            }
            
            // 添加圆点激活回调（瞬间执行）
            loadingSequence.AppendCallback(() => {
                ActivateDot(dotIndex);
            });
            
            // 添加缩放动画（并行执行，不占用序列时间）
            loadingSequence.Insert(dotStartTime, CreateDotScaleAnimation(dotIndex));
        }
        
        // 确保序列总长度为totalDuration
        if (currentTime < totalDuration)
        {
            loadingSequence.AppendInterval(totalDuration - currentTime);
        }
        
        // 添加完成回调
        loadingSequence.AppendCallback(() => {
            OnLoadingComplete?.Invoke();
            
            // 如果需要循环
            if (loop && isLoading)
            {
                // 短暂延迟后重新开始
                DOVirtual.DelayedCall(0.3f, () => {
                    if (isLoading) // 再次检查状态
                    {
                        ResetAllDots();
                        CreateLoadingSequence();
                    }
                });
            }
            else
            {
                isLoading = false;
            }
        });
        
        // 开始播放序列
        loadingSequence.Play();
        
        if (showDebugInfo)
        {
            Debug.Log($"CircularLoadingDots: 创建动画序列，总时长={totalDuration}s，圆点间隔={DotInterval:F2}s");
        }
    }
    
    /// <summary>
    /// 创建单个圆点的缩放动画
    /// </summary>
    private Tween CreateDotScaleAnimation(int dotIndex)
    {
        if (dotIndex < 0 || dotIndex >= dots.Count) return null;
        
        Transform dot = dots[dotIndex];
        Sequence dotSequence = DOTween.Sequence();
        
        // 放大动画
        dotSequence.Append(
            dot.DOScale(scaleMultiplier, EffectiveDotDuration * 0.5f)
                .SetEase(scaleEase)
        );
        
        // 缩小动画
        dotSequence.Append(
            dot.DOScale(1f, EffectiveDotDuration * 0.5f)
                .SetEase(Ease.InBack)
        );
        
        return dotSequence;
    }
    
    /// <summary>
    /// 激活指定圆点
    /// </summary>
    private void ActivateDot(int index)
    {
        if (index < 0 || index >= dots.Count) return;
        
        currentDotIndex = index;
        
        // 改变颜色
        if (useColorChange && index < dotImages.Count && dotImages[index] != null)
        {
            dotImages[index].DOColor(activeColor, 0.1f).OnComplete(() => {
                // 延迟后恢复原色
                DOVirtual.DelayedCall(EffectiveDotDuration, () => {
                    if (dotImages[index] != null)
                    {
                        dotImages[index].DOColor(normalColor, 0.2f);
                    }
                });
            });
        }
        
        OnDotActivated?.Invoke(index);
        
        if (showDebugInfo)
        {
            Debug.Log($"激活圆点 {index}");
        }
    }
    
    /// <summary>
    /// 设置圆点数量（运行时修改）
    /// </summary>
    public void SetDotCount(int count)
    {
        if (count <= 0) return;
        
        bool wasLoading = isLoading;
        StopLoading();
        
        dotCount = count;
        InitializeDots();
        
        if (wasLoading)
        {
            StartLoading();
        }
    }
    
    /// <summary>
    /// 设置总动画时长
    /// </summary>
    public void SetTotalDuration(float duration)
    {
        totalDuration = Mathf.Max(0.5f, duration);
        
        if (showDebugInfo)
        {
            Debug.Log($"设置总动画时长为: {totalDuration}s，圆点间隔: {DotInterval:F2}s");
        }
    }
    
    /// <summary>
    /// 设置单个圆点缩放时长
    /// </summary>
    public void SetSingleDotScaleDuration(float duration)
    {
        singleDotScaleDuration = Mathf.Max(0.1f, duration);
    }
    
    /// <summary>
    /// 设置环形半径
    /// </summary>
    public void SetRadius(float newRadius)
    {
        if (newRadius <= 0) return;
        
        radius = newRadius;
        
        // 重新定位圆点
        for (int i = 0; i < dots.Count; i++)
        {
            float angle = (360f / dotCount) * i;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                Mathf.Cos(radian) * radius,
                Mathf.Sin(radian) * radius,
                0f
            );
            
            RectTransform rectTransform = dots[i].GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
        }
    }
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public bool IsLoading => isLoading;
    
    /// <summary>
    /// 获取当前激活的圆点索引
    /// </summary>
    public int CurrentDotIndex => currentDotIndex;
    
    /// <summary>
    /// 获取圆点总数
    /// </summary>
    public int DotCount => dots.Count;
    
    /// <summary>
    /// 获取总动画时长
    /// </summary>
    public float TotalDuration => totalDuration;
    
    /// <summary>
    /// 获取当前动画进度（0-1）
    /// </summary>
    public float Progress
    {
        get
        {
            if (!isLoading || loadingSequence == null) return 0f;
            return Mathf.Clamp01(loadingSequence.position / totalDuration);
        }
    }
    
    /// <summary>
    /// 获取圆点间隔时间
    /// </summary>
    public float GetDotInterval() => DotInterval;
    
    // 编辑器辅助方法
    #if UNITY_EDITOR
    [ContextMenu("开始加载")]
    private void TestStartLoading()
    {
        if (Application.isPlaying)
        {
            StartLoading();
        }
    }
    
    [ContextMenu("停止加载")]
    private void TestStopLoading()
    {
        if (Application.isPlaying)
        {
            StopLoading();
        }
    }
    
    [ContextMenu("重新初始化")]
    private void TestReinitialize()
    {
        if (Application.isPlaying)
        {
            InitializeDots();
        }
    }
    
    // 在Scene视图中绘制预览
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
            
            // 绘制圆点位置预览
            for (int i = 0; i < dotCount; i++)
            {
                float angle = (360f / dotCount) * i;
                float radian = angle * Mathf.Deg2Rad;
                Vector3 position = transform.position + new Vector3(
                    Mathf.Cos(radian) * radius,
                    Mathf.Sin(radian) * radius,
                    0f
                );
                
                Gizmos.color = i == 0 ? Color.red : Color.white;
                Gizmos.DrawWireSphere(position, dotSize * 0.5f);
            }
        }
    }
    #endif
}
