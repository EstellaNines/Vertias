using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MagnifierCircleAnimation : MonoBehaviour
{
    [Header("放大镜图标设置")]
    [SerializeField][FieldLabel("放大镜图标")]
    private RectTransform magnifierIcon;
    
    [Header("圆环参数")]
    [SerializeField][FieldLabel("圆环中心点")]
    private RectTransform circleCenter;
    
    [SerializeField][FieldLabel("圆环半径")] 
    [Range(0f, 300f)]
    private float circleRadius = 100f;
    
    [SerializeField][FieldLabel("起始角度")]
    [Range(0f, 360f)]
    private float startAngle = 0f;
    
    [Header("动画参数")]
    [SerializeField][FieldLabel("动画持续时间")]
    [Range(1f, 10f)]
    private float animationDuration = 3f;
    
    [SerializeField][FieldLabel("旋转方向（顺时针）")]
    private bool isClockwise = true;
    
    [SerializeField][FieldLabel("环绕圈数")]
    [Range(1f, 5f)]
    private float rotationCount = 1f;
    
    [SerializeField][FieldLabel("动画缓动类型")]
    private Ease easeType = Ease.Linear;
    
    [Header("自动触发设置")]
    [SerializeField][FieldLabel("启动时自动开始")]
    private bool autoStartOnAwake = true;
    
    [SerializeField][FieldLabel("自动循环")]
    private bool autoLoop = true;
    
    [SerializeField][FieldLabel("循环间隔时间")]
    [Range(0f, 5f)]
    private float loopDelay = 0.5f;
    
    [Header("调试设置")]
    [SerializeField][FieldLabel("启用详细调试日志")]
    private bool enableDebugLogs = true;
    
    // 私有变量
    private Tween currentTween;
    private Vector3 centerPosition;
    private bool isAnimating = false;
    private float currentProgress = 0f;
    private float currentAnimationDuration = 0f;
    
    // 事件
    public event Action OnAnimationStart;
    public event Action OnAnimationComplete;
    public event Action OnAnimationLoop;
    public event Action<float> OnProgressUpdate; // 进度更新事件，参数为0-1之间的进度值
    
    private void Awake()
    {
        Initialize();
    }
    
    private void Start()
    {
        if (autoStartOnAwake)
        {
            StartCircleAnimation();
        }
    }
    
    private void Initialize()
    {
        // 验证组件
        if (magnifierIcon == null)
        {
            Debug.LogError("MagnifierCircleAnimation: 放大镜图标未设置！", this);
            return;
        }
        
        if (circleCenter == null)
        {
            circleCenter = transform as RectTransform;
        }
        
        // 获取中心点位置，确保使用正确的坐标
        centerPosition = circleCenter.localPosition;
        
        // 调试信息：输出初始化时的关键信息
        if (enableDebugLogs)
        {
            Debug.Log($"[初始化] 圆环中心组件: {circleCenter.name}");
            Debug.Log($"[初始化] 圆环中心位置: {centerPosition}");
            Debug.Log($"[初始化] 放大镜图标: {magnifierIcon.name}");
            Debug.Log($"[初始化] 放大镜初始位置: {magnifierIcon.localPosition}");
            Debug.Log($"[初始化] 圆环半径: {circleRadius}");
            Debug.Log($"[初始化] 起始角度: {startAngle}");
        }
        
        // 设置初始位置
        SetMagnifierPosition(startAngle);
        
        // 输出设置后的位置
        if (enableDebugLogs)
        {
            Debug.Log($"[初始化] 设置后放大镜位置: {magnifierIcon.localPosition}");
        }
    }
    
    private void SetMagnifierPosition(float angle)
    {
        if (magnifierIcon == null) return;
        
        // 转换为弧度，注意Unity UI坐标系中Y轴向上为正
        float radian = angle * Mathf.Deg2Rad;
        
        // 计算偏移量
        Vector3 offset = new Vector3(
            Mathf.Cos(radian) * circleRadius,   // X: 0度时为circleRadius（右侧）
            Mathf.Sin(radian) * circleRadius,   // Y: 0度时为0（水平）
            0f
        );
        
        // 重新获取中心点位置（防止运行时中心点移动）
        if (circleCenter != null)
        {
            centerPosition = circleCenter.localPosition;
        }
        
        // 确保0度在右侧（3点钟方向），90度在上方（12点钟方向）
        Vector3 position = centerPosition + offset;
        
        magnifierIcon.localPosition = position;
        
        // 详细调试信息
        if (enableDebugLogs)
        {
            Debug.Log($"[位置设置] 角度: {angle}°, 弧度: {radian:F3}");
            Debug.Log($"[位置设置] 圆环中心: {centerPosition}");
            Debug.Log($"[位置设置] 计算偏移: {offset}");
            Debug.Log($"[位置设置] 最终位置: {position}");
            Debug.Log($"[位置设置] 实际位置: {magnifierIcon.localPosition}");
        }
        
        // 验证0度位置
        if (Mathf.Approximately(angle, 0f))
        {
            float expectedX = centerPosition.x + circleRadius;
            float expectedY = centerPosition.y;
            bool correctX = Mathf.Approximately(magnifierIcon.localPosition.x, expectedX);
            bool correctY = Mathf.Approximately(magnifierIcon.localPosition.y, expectedY);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[0度验证] 预期X: {expectedX}, 实际X: {magnifierIcon.localPosition.x}, 正确: {correctX}");
                Debug.Log($"[0度验证] 预期Y: {expectedY}, 实际Y: {magnifierIcon.localPosition.y}, 正确: {correctY}");
            }
            
            if (!correctY)
            {
                Debug.LogWarning($"[0度验证] Y坐标不正确！预期: {expectedY}, 实际: {magnifierIcon.localPosition.y}, 差值: {magnifierIcon.localPosition.y - expectedY}");
            }
        }
    }
    
    /// <summary>
    /// 开始圆环动画
    /// </summary>
    public void StartCircleAnimation()
    {
        StartCircleAnimation(animationDuration);
    }
    
    /// <summary>
    /// 开始圆环动画，指定持续时间
    /// </summary>
    /// <param name="duration">动画持续时间</param>
    public void StartCircleAnimation(float duration)
    {
        if (magnifierIcon == null || isAnimating) return;
        
        StopCircleAnimation();
        
        isAnimating = true;
        currentProgress = 0f;
        currentAnimationDuration = duration;
        
        // 添加UI可见性调试
        Debug.Log($"[MagnifierCircleAnimation] 开始动画 - 持续时间: {duration:F1}秒");
        CheckMagnifierVisibility();
        
        OnAnimationStart?.Invoke();
        OnProgressUpdate?.Invoke(0f);
        
        // 计算总旋转角度
        float totalAngle = 360f * rotationCount * (isClockwise ? 1f : -1f);
        
        // 创建DOTween动画序列
        Sequence animSequence = DOTween.Sequence();
        
        // 添加旋转动画，包含进度更新
        animSequence.Append(DOTween.To(
            () => startAngle,
            angle => {
                SetMagnifierPosition(angle);
                UpdateProgress();
            },
            startAngle + totalAngle,
            duration
        ).SetEase(easeType));
        
        // 设置循环
        if (autoLoop)
        {
            animSequence.SetLoops(-1, LoopType.Restart);
            if (loopDelay > 0)
            {
                animSequence.AppendInterval(loopDelay);
            }
        }
        
        // 设置回调
        animSequence.OnComplete(() => {
            isAnimating = false;
            currentProgress = 1f;
            OnProgressUpdate?.Invoke(1f);
            OnAnimationComplete?.Invoke();
        });
        
        animSequence.OnStepComplete(() => {
            currentProgress = 0f;
            OnProgressUpdate?.Invoke(0f);
            OnAnimationLoop?.Invoke();
        });
        
        currentTween = animSequence;
    }
    
    /// <summary>
    /// 停止圆环动画
    /// </summary>
    public void StopCircleAnimation()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            currentTween = null;
        }
        isAnimating = false;
        currentProgress = 0f;
        OnProgressUpdate?.Invoke(0f);
    }
    
    /// <summary>
    /// 暂停圆环动画
    /// </summary>
    public void PauseCircleAnimation()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Pause();
        }
    }
    
    /// <summary>
    /// 恢复圆环动画
    /// </summary>
    public void ResumeCircleAnimation()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Play();
        }
    }
    
    /// <summary>
    /// 设置动画持续时间
    /// </summary>
    /// <param name="duration">持续时间</param>
    public void SetAnimationDuration(float duration)
    {
        animationDuration = Mathf.Max(0.1f, duration);
        if (isAnimating)
        {
            // 重新开始动画以应用新的持续时间
            StartCircleAnimation(animationDuration);
        }
    }
    
    /// <summary>
    /// 设置圆环半径
    /// </summary>
    /// <param name="radius">半径大小</param>
    public void SetCircleRadius(float radius)
    {
        circleRadius = Mathf.Max(10f, radius);
        if (!isAnimating)
        {
            SetMagnifierPosition(startAngle);
        }
    }
    
    /// <summary>
    /// 设置旋转方向
    /// </summary>
    /// <param name="clockwise">是否顺时针</param>
    public void SetRotationDirection(bool clockwise)
    {
        isClockwise = clockwise;
    }
    
    /// <summary>
    /// 设置环绕圈数
    /// </summary>
    /// <param name="count">圈数</param>
    public void SetRotationCount(float count)
    {
        rotationCount = Mathf.Max(0.1f, count);
    }
    
    /// <summary>
    /// 设置自动循环
    /// </summary>
    /// <param name="loop">是否循环</param>
    /// <param name="delay">循环间隔</param>
    public void SetAutoLoop(bool loop, float delay = 0.5f)
    {
        autoLoop = loop;
        loopDelay = Mathf.Max(0f, delay);
    }
    
    /// <summary>
    /// 获取当前是否正在播放动画
    /// </summary>
    /// <returns>是否正在播放动画</returns>
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    /// <summary>
    /// 重置到起始位置
    /// </summary>
    public void ResetToStartPosition()
    {
        StopCircleAnimation();
        SetMagnifierPosition(startAngle);
        currentProgress = 0f;
        OnProgressUpdate?.Invoke(0f);
    }
    
    /// <summary>
    /// 在指定延迟后开始动画
    /// </summary>
    /// <param name="delay">延迟时间</param>
    public void StartAnimationWithDelay(float delay)
    {
        StartCoroutine(DelayedStart(delay));
    }
    
    private IEnumerator DelayedStart(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCircleAnimation();
    }
    
    /// <summary>
    /// 更新动画进度
    /// </summary>
    private void UpdateProgress()
    {
        if (currentTween != null && currentTween.IsActive() && currentAnimationDuration > 0)
        {
            float elapsed = currentTween.Elapsed();
            currentProgress = Mathf.Clamp01(elapsed / currentAnimationDuration);
            OnProgressUpdate?.Invoke(currentProgress);
        }
    }
    
    /// <summary>
    /// 获取当前动画进度（0-1）
    /// </summary>
    /// <returns>当前进度值</returns>
    public float GetProgress()
    {
        return currentProgress;
    }
    
    /// <summary>
    /// 获取当前动画进度百分比（0-100）
    /// </summary>
    /// <returns>进度百分比</returns>
    public float GetProgressPercentage()
    {
        return currentProgress * 100f;
    }
    
    /// <summary>
    /// 获取剩余动画时间
    /// </summary>
    /// <returns>剩余时间（秒）</returns>
    public float GetRemainingTime()
    {
        if (!isAnimating || currentAnimationDuration <= 0) return 0f;
        return currentAnimationDuration * (1f - currentProgress);
    }
    
    /// <summary>
    /// 获取已经过的动画时间
    /// </summary>
    /// <returns>已过时间（秒）</returns>
    public float GetElapsedTime()
    {
        if (!isAnimating || currentAnimationDuration <= 0) return 0f;
        return currentAnimationDuration * currentProgress;
    }
    
    /// <summary>
    /// 获取当前动画总持续时间
    /// </summary>
    /// <returns>总持续时间（秒）</returns>
    public float GetTotalDuration()
    {
        return currentAnimationDuration;
    }
    
    /// <summary>
    /// 调试方法：测试0度位置（应该在右侧）
    /// </summary>
    [ContextMenu("测试0度位置（右侧）")]
    public void TestZeroDegreePosition()
    {
        TestAnglePosition(0f);
    }
    
    /// <summary>
    /// 调试方法：测试90度位置（应该在上方）
    /// </summary>
    [ContextMenu("测试90度位置（上方）")]
    public void TestNinetyDegreePosition()
    {
        TestAnglePosition(90f);
    }
    
    /// <summary>
    /// 调试方法：测试180度位置（应该在左侧）
    /// </summary>
    [ContextMenu("测试180度位置（左侧）")]
    public void TestOneEightyDegreePosition()
    {
        TestAnglePosition(180f);
    }
    
    /// <summary>
    /// 调试方法：测试270度位置（应该在下方）
    /// </summary>
    [ContextMenu("测试270度位置（下方）")]
    public void TestTwoSeventyDegreePosition()
    {
        TestAnglePosition(270f);
    }
    
    /// <summary>
    /// 调试方法：测试指定角度的位置
    /// </summary>
    /// <param name="testAngle">测试角度</param>
    public void TestAnglePosition(float testAngle)
    {
        if (magnifierIcon == null || circleCenter == null)
        {
            Debug.LogWarning("放大镜图标或圆环中心未设置！");
            return;
        }
        
        // 停止当前动画
        StopCircleAnimation();
        
        // 设置到指定角度
        SetMagnifierPosition(testAngle);
        
        // 计算预期位置（相对于中心点）
        float radian = testAngle * Mathf.Deg2Rad;
        Vector3 expectedOffset = new Vector3(
            Mathf.Cos(radian) * circleRadius,
            Mathf.Sin(radian) * circleRadius,
            0f
        );
        
        Debug.Log($"测试角度: {testAngle}°");
        Debug.Log($"圆环中心: {centerPosition}");
        Debug.Log($"预期偏移: {expectedOffset}");
        Debug.Log($"放大镜位置: {magnifierIcon.localPosition}");
        Debug.Log($"实际偏移: {magnifierIcon.localPosition - centerPosition}");
        
        // 验证0度是否在右侧
        if (Mathf.Approximately(testAngle, 0f))
        {
            bool isOnRightSide = magnifierIcon.localPosition.x > centerPosition.x;
            bool isOnCenterHeight = Mathf.Approximately(magnifierIcon.localPosition.y, centerPosition.y);
            Debug.Log($"0度位置验证 - 在右侧: {isOnRightSide}, 水平对齐: {isOnCenterHeight}");
        }
    }
    
    /// <summary>
    /// 强制修正位置（忽略可能的锚点问题）
    /// </summary>
    [ContextMenu("强制修正0度位置")]
    public void ForceCorrectZeroDegreePosition()
    {
        if (magnifierIcon == null || circleCenter == null)
        {
            Debug.LogWarning("放大镜图标或圆环中心未设置！");
            return;
        }
        
        // 强制设置为正确的0度位置
        Vector3 targetPosition = new Vector3(
            circleCenter.localPosition.x + circleRadius,
            circleCenter.localPosition.y,
            circleCenter.localPosition.z
        );
        
        magnifierIcon.localPosition = targetPosition;
        
        Debug.Log($"[强制修正] 圆环中心: {circleCenter.localPosition}");
        Debug.Log($"[强制修正] 目标位置: {targetPosition}");
        Debug.Log($"[强制修正] 实际位置: {magnifierIcon.localPosition}");
        
        // 检查是否有偏移
        Vector3 actualOffset = magnifierIcon.localPosition - targetPosition;
        if (actualOffset.magnitude > 0.1f)
        {
            Debug.LogWarning($"[强制修正] 位置仍有偏移: {actualOffset}，可能是锚点或pivot问题");
            
            // 提供修正建议
            Debug.Log("建议检查：");
            Debug.Log("1. 放大镜图标的Anchor设置");
            Debug.Log("2. 放大镜图标的Pivot设置（建议设为Center）");
            Debug.Log("3. 父对象的Scale设置");
            Debug.Log("4. Canvas的Render Mode设置");
        }
    }
    
    /// <summary>
    /// 检查组件设置
    /// </summary>
    [ContextMenu("检查组件设置")]
    public void CheckComponentSettings()
    {
        Debug.Log("=== 组件设置检查 ===");
        
        if (circleCenter != null)
        {
            Debug.Log($"圆环中心: {circleCenter.name}");
            Debug.Log($"圆环中心位置: {circleCenter.localPosition}");
            Debug.Log($"圆环中心锚点: {circleCenter.anchorMin} - {circleCenter.anchorMax}");
            Debug.Log($"圆环中心Pivot: {circleCenter.pivot}");
        }
        else
        {
            Debug.LogWarning("圆环中心未设置！");
        }
        
        if (magnifierIcon != null)
        {
            Debug.Log($"放大镜图标: {magnifierIcon.name}");
            Debug.Log($"放大镜位置: {magnifierIcon.localPosition}");
            Debug.Log($"放大镜锚点: {magnifierIcon.anchorMin} - {magnifierIcon.anchorMax}");
            Debug.Log($"放大镜Pivot: {magnifierIcon.pivot}");
            Debug.Log($"放大镜父对象: {magnifierIcon.parent?.name}");
        }
        else
        {
            Debug.LogWarning("放大镜图标未设置！");
        }
        
        Debug.Log($"圆环半径: {circleRadius}");
        Debug.Log($"起始角度: {startAngle}");
        
        // 检查Canvas设置
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas模式: {canvas.renderMode}");
            Debug.Log($"Canvas缩放: {canvas.scaleFactor}");
        }
    }
    
    private void OnDestroy()
    {
        StopCircleAnimation();
    }
    
    // 编辑器中实时预览
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (magnifierIcon != null && circleCenter != null)
        {
            centerPosition = circleCenter.localPosition;
            SetMagnifierPosition(startAngle);
        }
    }
    
    /// <summary>
    /// 检查放大镜图标的可见性设置
    /// </summary>
    private void CheckMagnifierVisibility()
    {
        if (magnifierIcon == null) return;
        
        Debug.Log("=== 放大镜可见性检查 ===");
        
        // 检查GameObject激活状态
        Debug.Log($"放大镜GameObject激活: {magnifierIcon.gameObject.activeInHierarchy}");
        Debug.Log($"放大镜GameObject本地激活: {magnifierIcon.gameObject.activeSelf}");
        
        // 检查Image组件
        UnityEngine.UI.Image imageComponent = magnifierIcon.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent != null)
        {
            Debug.Log($"Image组件启用: {imageComponent.enabled}");
            Debug.Log($"Image颜色: {imageComponent.color}");
            Debug.Log($"Image Sprite: {(imageComponent.sprite != null ? imageComponent.sprite.name : "null")}");
        }
        else
        {
            Debug.LogWarning("未找到Image组件!");
        }
        
        // 检查RawImage组件（如果有的话）
        UnityEngine.UI.RawImage rawImageComponent = magnifierIcon.GetComponent<UnityEngine.UI.RawImage>();
        if (rawImageComponent != null)
        {
            Debug.Log($"RawImage组件启用: {rawImageComponent.enabled}");
            Debug.Log($"RawImage颜色: {rawImageComponent.color}");
            Debug.Log($"RawImage纹理: {(rawImageComponent.texture != null ? rawImageComponent.texture.name : "null")}");
        }
        
        // 检查CanvasGroup
        CanvasGroup canvasGroup = magnifierIcon.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"CanvasGroup Alpha: {canvasGroup.alpha}");
            Debug.Log($"CanvasGroup Interactable: {canvasGroup.interactable}");
        }
        
        // 检查RectTransform
        Debug.Log($"放大镜RectTransform - LocalPosition: {magnifierIcon.localPosition}, LocalScale: {magnifierIcon.localScale}");
        Debug.Log($"放大镜RectTransform - SizeDelta: {magnifierIcon.sizeDelta}, AnchoredPosition: {magnifierIcon.anchoredPosition}");
    }
    
    private void OnDrawGizmos()
    {
        if (circleCenter == null) return;
        
        // 在Scene视图中绘制圆环预览
        Gizmos.color = Color.yellow;
        Vector3 worldCenter = circleCenter.position;
        
        // 绘制圆环
        float stepSize = 5f;
        for (float angle = 0; angle < 360f; angle += stepSize)
        {
            float radian1 = angle * Mathf.Deg2Rad;
            float radian2 = (angle + stepSize) * Mathf.Deg2Rad;
            
            Vector3 point1 = worldCenter + new Vector3(
                Mathf.Cos(radian1) * circleRadius,
                Mathf.Sin(radian1) * circleRadius,
                0f
            );
            Vector3 point2 = worldCenter + new Vector3(
                Mathf.Cos(radian2) * circleRadius,
                Mathf.Sin(radian2) * circleRadius,
                0f
            );
            
            Gizmos.DrawLine(point1, point2);
        }
        
        // 绘制0度位置（右侧）
        Gizmos.color = Color.blue;
        Vector3 zeroDegreePoint = worldCenter + new Vector3(circleRadius, 0f, 0f);
        Gizmos.DrawWireSphere(zeroDegreePoint, 8f);
        Gizmos.DrawLine(worldCenter, zeroDegreePoint);
        
        // 绘制起始点
        Gizmos.color = Color.green;
        float startRadian = startAngle * Mathf.Deg2Rad;
        Vector3 startPoint = worldCenter + new Vector3(
            Mathf.Cos(startRadian) * circleRadius,
            Mathf.Sin(startRadian) * circleRadius,
            0f
        );
        Gizmos.DrawWireSphere(startPoint, 5f);
        
        // 绘制90度位置（上方）用于参考
        Gizmos.color = Color.cyan;
        Vector3 ninetyDegreePoint = worldCenter + new Vector3(0f, circleRadius, 0f);
        Gizmos.DrawWireSphere(ninetyDegreePoint, 4f);
        
        // 绘制中心点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(worldCenter, 3f);
    }
    #endif
}
