using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[Range(1f, 2f)] public float targetScale = 1.08f;
	[Range(0f, 1f)] public float duration = 0.12f;
	public Ease ease = Ease.OutQuad;

	private Vector3 _originalScale;
	private Tween _tween;

	private void Awake()
	{
		_originalScale = transform.localScale;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		PlayTo(targetScale);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		PlayTo(1f);
	}

	private void PlayTo(float scale)
	{
		_tween?.Kill(false);
		_tween = transform.DOScale(_originalScale * scale, duration).SetEase(ease);
	}

	private void OnDisable()
	{
		_tween?.Kill(false);
		transform.localScale = _originalScale;
	}
}
