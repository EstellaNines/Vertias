using System;
using UnityEngine;

public class DelayMagnifierUIController : MonoBehaviour
{
	[Header("组件引用")]
	[SerializeField][FieldLabel("放大镜圆环动画")] private MagnifierCircleAnimation magnifierAnimation;
	[SerializeField][FieldLabel("进度控制器(可选)")] private MagnifierProgressController progressController;
	[SerializeField][FieldLabel("根对象(显示/隐藏)")] private GameObject rootObject;
	[SerializeField][FieldLabel("可选CanvasGroup用于淡入淡出")] private CanvasGroup canvasGroup;

	[Header("参数")]
	[SerializeField][FieldLabel("默认延迟秒数")] private float defaultDelaySeconds = 1.5f;
	[SerializeField][FieldLabel("完成后自动隐藏")] private bool autoHideOnComplete = true;

	private bool isRunning;
	private Action onCompleted;
	private Action onCancelled;

	private void Awake()
	{
		if (rootObject == null) rootObject = gameObject;
		HideImmediate();
	}

	public bool IsRunning => isRunning;
	
	/// <summary>
	/// 检查是否正在延迟中（与IsRunning相同，但语义更清晰）
	/// </summary>
	public bool IsDelaying() => isRunning;

	public void StartDelay(float durationSeconds, Action onComplete = null)
	{
		StartDelay(durationSeconds, onComplete, null);
	}

	public void StartDelay(Action onComplete = null)
	{
		StartDelay(defaultDelaySeconds, onComplete, null);
	}
	
	/// <summary>
	/// 开始延迟，支持完成和取消回调
	/// </summary>
	/// <param name="durationSeconds">延迟持续时间</param>
	/// <param name="onComplete">完成时的回调</param>
	/// <param name="onCancel">取消时的回调</param>
	public void StartDelay(float durationSeconds, Action onComplete = null, Action onCancel = null)
	{
		if (isRunning) return;
		isRunning = true;
		onCompleted = onComplete;
		onCancelled = onCancel;

		gameObject.SetActive(true);

		Debug.Log($"[DelayMagnifierUI] 开始延迟 {durationSeconds:F1} 秒");
		ShowImmediate();
		if (magnifierAnimation != null)
		{
			magnifierAnimation.SetAutoLoop(false);
			magnifierAnimation.SetAnimationDuration(durationSeconds);
			magnifierAnimation.ResetToStartPosition();
			magnifierAnimation.OnAnimationComplete += HandleCompleted;
			magnifierAnimation.StartCircleAnimation();
		}
		else
		{
			// 若未配置动画，直接计时完成
			Invoke(nameof(HandleCompleted), Mathf.Max(0.01f, durationSeconds));
		}
	}

	public void Cancel()
	{
		if (!isRunning) return;
		
		Debug.Log("[DelayMagnifierUI] 延迟被取消");
		
		if (magnifierAnimation != null)
		{
			magnifierAnimation.StopCircleAnimation();
			magnifierAnimation.OnAnimationComplete -= HandleCompleted;
		}
		
		// 取消Invoke调用（如果有）
		CancelInvoke(nameof(HandleCompleted));
		
		isRunning = false;
		HideImmediate();

		gameObject.SetActive(false);

		// 调用取消回调
		onCancelled?.Invoke();
		
		// 清理回调引用
		onCompleted = null;
		onCancelled = null;
	}

	private void HandleCompleted()
	{
		if (!isRunning) return;
		
		Debug.Log("[DelayMagnifierUI] 延迟完成");
		
		isRunning = false;
		if (magnifierAnimation != null)
		{
			magnifierAnimation.OnAnimationComplete -= HandleCompleted;
		}
		
		if (autoHideOnComplete) HideImmediate();
		
		// 调用完成回调
		onCompleted?.Invoke();
		
		// 清理回调引用
		onCompleted = null;
		onCancelled = null;
	}

	public void ShowImmediate()
	{
		Debug.Log($"[DelayMagnifierUI] ShowImmediate被调用");
		
		if (rootObject != null) 
		{
			rootObject.SetActive(true);
			Debug.Log($"[DelayMagnifierUI] RootObject激活状态: {rootObject.activeInHierarchy}, 名称: {rootObject.name}");
		}
		else
		{
			Debug.LogWarning("[DelayMagnifierUI] RootObject为null!");
		}
		
		if (canvasGroup != null)
		{
			canvasGroup.alpha = 1f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			Debug.Log($"[DelayMagnifierUI] CanvasGroup设置 - Alpha: {canvasGroup.alpha}, Interactable: {canvasGroup.interactable}, BlocksRaycasts: {canvasGroup.blocksRaycasts}");
		}
		else
		{
			Debug.LogWarning("[DelayMagnifierUI] CanvasGroup为null!");
		}
		
		// 输出RectTransform信息用于调试
		if (rootObject != null)
		{
			RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
			if (rectTransform != null)
			{
				Debug.Log($"[DelayMagnifierUI] RootObject RectTransform - LocalPosition: {rectTransform.localPosition}, SizeDelta: {rectTransform.sizeDelta}, LocalScale: {rectTransform.localScale}");
				Debug.Log($"[DelayMagnifierUI] RootObject RectTransform - AnchoredPosition: {rectTransform.anchoredPosition}, Pivot: {rectTransform.pivot}, AnchorMin/Max: {rectTransform.anchorMin}/{rectTransform.anchorMax}");
			}
		}
	}

	public void HideImmediate()
	{
		if (canvasGroup != null)
		{
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}
		if (rootObject != null) rootObject.SetActive(false);
	}
}
