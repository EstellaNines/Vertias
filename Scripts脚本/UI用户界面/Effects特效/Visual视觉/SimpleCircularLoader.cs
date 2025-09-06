using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 简单的环形圆点加载器
/// 20个圆点按顺序放大1.5倍的加载动画
/// </summary>
public class SimpleCircularLoader : MonoBehaviour
{
    [Header("=== 基础设置 ===")]
    [FieldLabel("圆点数量")]
    [SerializeField] private int dotCount = 20; // 圆点数量
    [FieldLabel("环形半径")]
    [SerializeField] private float radius = 80f; // 环形半径
    [FieldLabel("圆点大小")]
    [SerializeField] private float dotSize = 8f; // 圆点大小
    
    [Header("=== 动画设置 ===")]
    [FieldLabel("动画间隔")]
    [Tooltip("每个圆点激活的间隔时间 - 数值越大动画越慢，数值越小动画越快")]
    [SerializeField] private float animationInterval = 0.1f; // 圆点动画间隔
    [FieldLabel("放大倍数")]
    [SerializeField] private float scaleSize = 1.5f; // 放大倍数
    [FieldLabel("缩放持续时间")]
    [Tooltip("单个圆点缩放动画的持续时间 - 数值越大缩放越慢，数值越小缩放越快")]
    [SerializeField] private float scaleDuration = 0.4f; // 缩放持续时间
    [FieldLabel("自动播放")]
    [SerializeField] private bool autoPlay = true; // 自动播放
    [FieldLabel("循环播放")]
    [SerializeField] private bool loopAnimation = true; // 循环播放
    
    [Header("=== 外观设置 ===")]
    [FieldLabel("圆点颜色")]
    [SerializeField] private Color dotColor = Color.white; // 圆点颜色
    [FieldLabel("激活时颜色")]
    [SerializeField] private Color activeColor = Color.cyan; // 激活时颜色
    
    // 私有变量
    private List<GameObject> dots = new List<GameObject>();
    private Sequence loadingSequence;
    private bool isPlaying = false;
    
    void Start()
    {
        CreateDots();
        
        if (autoPlay)
        {
            StartAnimation();
        }
    }
    
    void OnDestroy()
    {
        StopAnimation();
    }
    
    /// <summary>
    /// 创建圆点
    /// </summary>
    private void CreateDots()
    {
        // 清除现有圆点
        foreach (GameObject dot in dots)
        {
            if (dot != null) DestroyImmediate(dot);
        }
        dots.Clear();
        
        // 创建新圆点
        for (int i = 0; i < dotCount; i++)
        {
            GameObject dot = CreateSingleDot(i);
            dots.Add(dot);
        }
        
        Debug.Log($"SimpleCircularLoader: 创建了 {dotCount} 个圆点");
    }
    
    /// <summary>
    /// 创建单个圆点
    /// </summary>
    private GameObject CreateSingleDot(int index)
    {
        // 创建GameObject
        GameObject dot = new GameObject($"Dot_{index:D2}");
        dot.transform.SetParent(transform, false);
        
        // 添加Image组件
        Image image = dot.AddComponent<Image>();
        image.color = dotColor;
        
        // 创建圆形精灵
        image.sprite = CreateCircleSprite();
        image.type = Image.Type.Simple;
        
        // 设置RectTransform
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.sizeDelta = Vector2.one * dotSize;
        
        // 计算位置
        float angle = (360f / dotCount) * index - 90f; // -90度让第一个圆点在顶部
        float radian = angle * Mathf.Deg2Rad;
        Vector2 position = new Vector2(
            Mathf.Cos(radian) * radius,
            Mathf.Sin(radian) * radius
        );
        
        rectTransform.anchoredPosition = position;
        
        return dot;
    }
    
    /// <summary>
    /// 创建圆形精灵
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = Vector2.one * (size / 2f);
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }
    
    /// <summary>
    /// 开始动画
    /// </summary>
    public void StartAnimation()
    {
        if (isPlaying) return;
        
        if (dots.Count == 0)
        {
            Debug.LogWarning("没有圆点可以播放动画");
            return;
        }
        
        isPlaying = true;
        CreateAnimationSequence();
        
        Debug.Log("开始环形加载动画");
    }
    
    /// <summary>
    /// 停止动画
    /// </summary>
    public void StopAnimation()
    {
        isPlaying = false;
        
        if (loadingSequence != null)
        {
            loadingSequence.Kill();
            loadingSequence = null;
        }
        
        // 重置所有圆点
        ResetAllDots();
    }
    
    /// <summary>
    /// 重置所有圆点状态
    /// </summary>
    private void ResetAllDots()
    {
        foreach (GameObject dot in dots)
        {
            if (dot != null)
            {
                dot.transform.localScale = Vector3.one;
                Image image = dot.GetComponent<Image>();
                if (image != null)
                {
                    image.color = dotColor;
                }
            }
        }
    }
    
    /// <summary>
    /// 创建动画序列
    /// </summary>
    private void CreateAnimationSequence()
    {
        loadingSequence = DOTween.Sequence();
        
        // 为每个圆点添加动画
        for (int i = 0; i < dots.Count; i++)
        {
            int dotIndex = i; // 闭包变量
            GameObject currentDot = dots[i];
            Image currentImage = currentDot.GetComponent<Image>();
            
            // 添加间隔（第一个圆点不需要间隔）
            if (i > 0)
            {
                loadingSequence.AppendInterval(animationInterval);
            }
            
            // 同时进行缩放和颜色变化
            Sequence dotSequence = DOTween.Sequence();
            
            // 放大动画
            dotSequence.Append(
                currentDot.transform.DOScale(scaleSize, scaleDuration * 0.5f)
                    .SetEase(Ease.OutBack)
            );
            
            // 缩小动画
            dotSequence.Append(
                currentDot.transform.DOScale(1f, scaleDuration * 0.5f)
                    .SetEase(Ease.InBack)
            );
            
            // 颜色变化动画
            dotSequence.Join(
                currentImage.DOColor(activeColor, scaleDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        currentImage.DOColor(dotColor, scaleDuration * 0.7f)
                            .SetEase(Ease.InQuad);
                    })
            );
            
            // 将圆点动画添加到主序列
            loadingSequence.Append(dotSequence);
        }
        
        // 动画完成回调
        loadingSequence.OnComplete(() => {
            if (loopAnimation && isPlaying)
            {
                // 循环播放
                DOVirtual.DelayedCall(0.3f, () => {
                    if (isPlaying)
                    {
                        CreateAnimationSequence();
                    }
                });
            }
            else
            {
                isPlaying = false;
                Debug.Log("环形加载动画完成");
            }
        });
        
        // 开始播放
        loadingSequence.Play();
    }
    
    /// <summary>
    /// 设置动画速度
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationInterval = Mathf.Max(0.01f, speed);
    }
    
    /// <summary>
    /// 设置缩放大小
    /// </summary>
    public void SetScaleSize(float scale)
    {
        scaleSize = Mathf.Max(1f, scale);
    }
    
    /// <summary>
    /// 切换循环模式
    /// </summary>
    public void SetLoopMode(bool loop)
    {
        loopAnimation = loop;
    }
    
    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;
    
    // 编辑器辅助功能
    #if UNITY_EDITOR
    [ContextMenu("开始动画")]
    void TestStart()
    {
        if (Application.isPlaying) StartAnimation();
    }
    
    [ContextMenu("停止动画")]
    void TestStop()
    {
        if (Application.isPlaying) StopAnimation();
    }
    
    [ContextMenu("重新创建圆点")]
    void TestRecreate()
    {
        if (Application.isPlaying) CreateDots();
    }
    
    void OnDrawGizmosSelected()
    {
        // 在Scene视图中预览圆点位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        for (int i = 0; i < dotCount; i++)
        {
            float angle = (360f / dotCount) * i - 90f;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 position = transform.position + new Vector3(
                Mathf.Cos(radian) * radius,
                Mathf.Sin(radian) * radius,
                0f
            );
            
            Gizmos.color = i == 0 ? Color.red : Color.white;
            Gizmos.DrawSphere(position, dotSize * 0.3f);
        }
    }
    #endif
}
