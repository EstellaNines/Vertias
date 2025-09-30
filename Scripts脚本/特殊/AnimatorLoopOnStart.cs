using UnityEngine;

[DisallowMultipleComponent]
// 在启动时立即播放 Animator 中的 "Idle" 状态（请在动画或 Animator 中将该状态设置为循环）
public sealed class AnimatorLoopOnStart : MonoBehaviour
{
	[SerializeField, Tooltip("为空则自动获取 Animator")]
	private Animator targetAnimator;

	[Header("差异化播放设置")]
	[SerializeField, Tooltip("播放速度最小值（含）")]
	private float speedMin = 0.9f;
	[SerializeField, Tooltip("播放速度最大值（含）")]
	private float speedMax = 1.1f;
	[SerializeField, Tooltip("是否随机初始进度（相位），用于错峰播放")]
	private bool randomizeStartOffset = true;

	private static readonly int IdleHash = Animator.StringToHash("Idle");

	private void Awake()
	{
		if (targetAnimator == null)
		{
			targetAnimator = GetComponent<Animator>();
		}
	}

	private void Start()
	{
		if (targetAnimator == null) return;

		// 随机设置播放速度（频率差异）
		if (speedMax < 0.01f) speedMax = 0.01f;
		if (speedMin > speedMax)
		{
			float tmp = speedMin; speedMin = speedMax; speedMax = tmp;
		}
		targetAnimator.speed = Random.Range(speedMin, speedMax);

		// 随机起始进度（相位差异）
		float startT = randomizeStartOffset ? Random.value : 0f;
		targetAnimator.Play(IdleHash, 0, startT);
		// 立即评估一帧，保证首帧立刻显示
		targetAnimator.Update(0f);
	}

	[ContextMenu("立即播放 Idle")]
	private void PlayIdleNow()
	{
		if (targetAnimator == null) return;
		float startT = randomizeStartOffset ? Random.value : 0f;
		targetAnimator.Play(IdleHash, 0, startT);
		targetAnimator.Update(0f);
	}

	private void OnValidate()
	{
		if (speedMax < 0.01f) speedMax = 0.01f;
		if (speedMin < 0f) speedMin = 0f;
		if (speedMin > speedMax)
		{
			float tmp = speedMin; speedMin = speedMax; speedMax = tmp;
		}
	}
}


