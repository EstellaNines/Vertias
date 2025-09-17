using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.UI
{
	/// <summary>
	/// 通过 Button 点击，让目标 Image 的 RectTransform 在 Y 轴从 0 缩放到 1
	/// 使用 DOTween 控制动效。
	/// 将本脚本挂在含有 Button 的物体上，或手动指定 <see cref="triggerButton"/>。
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class ScaleImageOnButton : MonoBehaviour
	{
		[Header("Trigger 按钮 (可不填, 默认取本物体上的 Button)")]
		[SerializeField] private Button triggerButton;

		[Header("需要缩放的目标 Image")]
		[SerializeField] private Image targetImage;

		[Header("动效参数")]
		[SerializeField, Min(0f)] private float durationSeconds = 0.35f;
		[SerializeField] private Ease ease = Ease.OutBack;
		[SerializeField] private bool ignoreTimeScale = false;
		[SerializeField] private bool setYZeroOnEnable = true;
		[Tooltip("启用时是否为打开状态 (优先级低于 setYZeroOnEnable)")]
		[SerializeField] private bool startOpened = false;

		private Tween activeTween;
		private bool isOpen;

		private void Reset()
		{
			triggerButton = GetComponent<Button>();
			if (targetImage == null)
			{
				// 优先尝试从子物体中找到一个 Image
				targetImage = GetComponentInChildren<Image>();
			}
		}

		private void Awake()
		{
			if (triggerButton == null)
			{
				triggerButton = GetComponent<Button>();
			}
		}

		private void OnEnable()
		{
			if (triggerButton != null)
			{
				triggerButton.onClick.AddListener(OnTriggerClicked);
			}

			if (targetImage != null)
			{
				var rt = targetImage.rectTransform;
				Vector3 s = rt.localScale;
				if (setYZeroOnEnable)
				{
					s.y = 0f;
					rt.localScale = s;
					isOpen = false;
				}
				else if (startOpened)
				{
					s.y = 1f;
					rt.localScale = s;
					isOpen = true;
				}
				else
				{
					// 夹紧初始值，防止出现 <0 或 >1 的脏状态
					s.y = Mathf.Clamp01(s.y);
					rt.localScale = s;
					isOpen = s.y > 0.5f;
				}
			}
		}

		private void OnDisable()
		{
			if (triggerButton != null)
			{
				triggerButton.onClick.RemoveListener(OnTriggerClicked);
			}

			KillActiveTween();
		}

		/// <summary>
		/// 外部也可以直接调用：切换打开/关闭
		/// </summary>
		public void Play() => Toggle();

		/// <summary>
		/// 切换打开/关闭
		/// </summary>
		public void Toggle()
		{
			AnimateToState(!isOpen);
		}

		/// <summary>
		/// 打开（Y→1）
		/// </summary>
		public void Open()
		{
			AnimateToState(true);
		}

		/// <summary>
		/// 关闭（Y→0）
		/// </summary>
		public void Close()
		{
			AnimateToState(false);
		}

		private void OnTriggerClicked()
		{
			Toggle();
		}

		private void AnimateToState(bool open)
		{
			if (targetImage == null)
			{
				Debug.LogWarning("ScaleImageOnButton: 未设置 targetImage。");
				return;
			}

			var rt = targetImage.rectTransform;
			KillActiveTween();
			float endValue = open ? 1f : 0f;
			activeTween = rt.DOScaleY(endValue, durationSeconds)
				.SetEase(ease)
				.SetUpdate(ignoreTimeScale)
				.OnUpdate(() =>
				{
					var s = rt.localScale;
					s.y = Mathf.Clamp01(s.y);
					rt.localScale = s;
				})
				.OnComplete(() => { isOpen = open; });
		}

		private void KillActiveTween()
		{
			if (activeTween != null && activeTween.IsActive())
			{
				activeTween.Kill();
				activeTween = null;
			}
		}
	}
}


