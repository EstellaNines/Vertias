using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MissionSection : MonoBehaviour, IPointerClickHandler
{
    [Header("任务条背景图片")]
    [SerializeField] private Image missionBarImage;

    [Header("任务名称文本")]
    [SerializeField] private TextMeshProUGUI missionNameText;

    [Header("任务类型图标")]
    [SerializeField] private Image missionTypeIcon;

    [Header("普通状态图片")]
    [SerializeField] private Sprite normalSprite;

    [Header("确认状态图片")]
    [SerializeField] private Sprite confirmedSprite;

    private bool isConfirmed = false;
    private int missionIndex = -1;
    private MissionManager missionManager;
    private List<GameObject> clickableObjects = new List<GameObject>();

    private void Start()
    {
        if (missionBarImage == null)
            Debug.LogError("MissionSection: missionBarImage 未在 Inspector 中赋值！");

        if (normalSprite == null)
            Debug.LogError("MissionSection: normalSprite 未在 Inspector 中赋值！");

        if (confirmedSprite == null)
            Debug.LogError("MissionSection: confirmedSprite 未在 Inspector 中赋值！");

        if (missionBarImage != null && normalSprite != null)
            missionBarImage.sprite = normalSprite;

        CollectClickableObjects();
        InitializeMissionDisplay();
    }

    private void InitializeMissionDisplay()
    {
        if (missionManager != null && missionIndex >= 0)
            UpdateMissionDisplay();
    }

    private void UpdateMissionDisplay()
    {
        if (missionManager == null || missionIndex < 0) return;

        MissionData missionData = missionManager.GetMissionData(missionIndex);
        if (missionData != null)
        {
			if (missionNameText != null)
			{
				// 名称优先显示 missionData.name；可选追加类别标识
				missionNameText.text = string.IsNullOrEmpty(missionData.category)
					? missionData.name
					: $"{missionData.name} [{missionData.category}]";
			}

			if (missionTypeIcon != null)
			{
				string iconPath = !string.IsNullOrEmpty(missionData.iconPath)
					? missionData.iconPath
					: GetFallbackIconPathByType(missionData.type);
				if (!string.IsNullOrEmpty(iconPath))
				{
					LoadAndSetIcon(iconPath);
				}
				else
				{
					missionTypeIcon.gameObject.SetActive(false);
				}
			}
        }
        else
        {
            if (missionNameText != null)
                missionNameText.text = $"任务 {missionIndex + 1}";

            if (missionTypeIcon != null)
                missionTypeIcon.gameObject.SetActive(false);
        }
    }

	private string GetFallbackIconPathByType(string type)
	{
		if (string.IsNullOrEmpty(type)) return null;
		switch (type.ToLowerInvariant())
		{
			case "explore": return "MissionIcon/explore_icon";
			case "combat": return "MissionIcon/combat_icon"; // 若资源不存在，LoadAndSetIcon会自动隐藏
			case "talk": return "MissionIcon/talk_icon";
			case "trade": return "MissionIcon/talk_icon"; // 复用对话图标作为交易回退
			default: return null;
		}
	}

    private void LoadAndSetIcon(string iconPath)
    {
        if (missionTypeIcon == null || string.IsNullOrEmpty(iconPath)) return;

        try
        {
            Sprite iconSprite = Resources.Load<Sprite>(iconPath);
            if (iconSprite != null)
            {
                missionTypeIcon.sprite = iconSprite;
                missionTypeIcon.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"图标加载失败: {iconPath}");
                missionTypeIcon.gameObject.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载图标异常: {e.Message}");
            missionTypeIcon.gameObject.SetActive(false);
        }
    }

    private void CollectClickableObjects()
    {
        clickableObjects.Clear();
        if (missionBarImage != null)
        {
            clickableObjects.Add(missionBarImage.gameObject);
            CollectChildObjects(missionBarImage.transform);
        }
    }

    private void CollectChildObjects(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            clickableObjects.Add(child.gameObject);
            if (child.childCount > 0)
                CollectChildObjects(child);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        if (IsClickableObject(clickedObject))
        {
            missionManager?.OnMissionItemClicked(missionIndex);
        }
    }

    private bool IsClickableObject(GameObject clickedObject)
    {
        if (clickableObjects.Contains(clickedObject)) return true;

        if (missionBarImage != null)
        {
            Transform t = clickedObject.transform;
            while (t != null)
            {
                if (t.gameObject == missionBarImage.gameObject) return true;
                t = t.parent;
            }
        }
        return false;
    }

    public void SetMissionIndex(int index)
    {
        missionIndex = index;
        UpdateMissionDisplay();
    }

    public void SetMissionManager(MissionManager manager)
    {
        missionManager = manager;
        UpdateMissionDisplay();
    }

    public void SetConfirmedStateDirectly(bool confirmed)
    {
        isConfirmed = confirmed;
        if (missionBarImage != null)
            missionBarImage.sprite = isConfirmed ? confirmedSprite : normalSprite;
    }

    public bool IsConfirmed() => isConfirmed;
    public int GetMissionIndex() => missionIndex;

    public void SetMissionBarImage(Image image)
    {
        missionBarImage = image;
        CollectClickableObjects();
    }

    public void SetMissionNameText(TextMeshProUGUI textComponent)
    {
        missionNameText = textComponent;
        UpdateMissionDisplay();
    }

    public void SetMissionTypeIcon(Image iconComponent)
    {
        missionTypeIcon = iconComponent;
        UpdateMissionDisplay();
    }

    public void RefreshClickableObjects() => CollectClickableObjects();
    public void RefreshMissionDisplay() => UpdateMissionDisplay();
}