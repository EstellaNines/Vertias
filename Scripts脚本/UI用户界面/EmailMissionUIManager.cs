using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
	/// <summary>
	/// Email 任务展示管理器：读取 Resources/MissionData.json，并将数据渲染到 Email 预制体 UI。
	/// 将本脚本挂载到 Email 预制体的根 GameObject。
	/// </summary>
	[DisallowMultipleComponent]
	[ExecuteAlways]
	public sealed class EmailMissionUIManager : MonoBehaviour
	{
		[Header("要显示的任务 ID (对应 MissionData.json 中的 mission.id)")]
		[SerializeField] private int selectedMissionId = 0;

		[Header("引用 - 标题区 (按钮内的 TMP 显示 name)")]
		[SerializeField] private TextMeshProUGUI headerNameTMP;

		[Header("引用 - 内容区 (Image 下的 TMP 显示 description)")]
		[SerializeField] private TextMeshProUGUI descriptionTMP;

		[Serializable]
		private sealed class PrizeRefs
		{
			public GameObject root;
			public Image icon;
			public TextMeshProUGUI valueTMP;
		}

		[Header("引用 - 奖励区 (Image/Prize 下的四个奖励)")]
		[SerializeField] private PrizeRefs prizeMoney;
		[SerializeField] private PrizeRefs prizeWeapon;
		[SerializeField] private PrizeRefs prizeFood;
		[SerializeField] private PrizeRefs prizeIntelligence;

		[Header("可选 - 标题图标 (若需要用 mission.iconPath)")]
		[SerializeField] private Image headerIconImage;

		[Header("引用 - Requirements 文本")]
		[SerializeField] private TextMeshProUGUI requirementsValueTMP;

		[Serializable]
		private sealed class RequirementRowRefs
		{
			public GameObject rawRoot;
			public TextMeshProUGUI valueTMP;
		}

		[Header("引用 - Requirements 行 (Intelligence / Item)")]
		[SerializeField] private RequirementRowRefs requirementsIntelligence;
		[SerializeField] private RequirementRowRefs requirementsItem;

		private MissionDataRoot cachedData;
		[Header("完成状态&图标显示")]
		[SerializeField] private bool isCompleted = false;
		[SerializeField] private Image completeImage; // Email/MissionEmail/Complete 上的 Image
		[SerializeField] private Sprite completeSuccessSprite; // 成功用（如未指定，将尝试按名称从 Resources 读取）
		[SerializeField] private Sprite completeFailSprite;    // 失败用（如未指定，将尝试按名称从 Resources 读取）
		[SerializeField] private string successSpriteResourceName = "complete_0";
		[SerializeField] private string failSpriteResourceName = "complete_1";

		public bool IsCompleted => isCompleted; // 提供只读访问并使字段被读取

		#region JSON 数据结构
		[Serializable]
		private sealed class MissionDataRoot
		{
			public List<Mission> missions;
		}

		[Serializable]
		private sealed class Mission
		{
			public int id;
			public string name;
			public string type;
			public string category;
			public string iconPath;
			public string legendPath;
			public string description;
			public Reward reward;
			public Requirements requirements;
		}

		[Serializable]
		private sealed class Reward
		{
			public int money;
			public int weapon;
			public int food;
			public int intelligence;
			public string moneyIconPath;
			public string weaponIconPath;
			public string foodIconPath;
			public string intelligenceIconPath;
		}

		[Serializable]
		private sealed class Requirements
		{
			public int intelligence;
			public int itemCount;
		}
		#endregion

		private void Reset()
		{
			AutoBindReferences();
		}

		private void Awake()
		{
			LoadDataIfNeeded();
			if (headerNameTMP == null || descriptionTMP == null || prizeMoney == null || prizeWeapon == null || prizeFood == null || prizeIntelligence == null)
			{
				AutoBindReferences();
			}
		}

		private void OnEnable()
		{
			ApplySelectedMission();
		}

		private void OnValidate()
		{
			// 在编辑器中修改 ID 或引用时自动预览
			#if UNITY_EDITOR
			AutoBindReferences();
			cachedData = null; // 强制重新加载，确保 JSON 更新能反映到编辑器
			LoadDataIfNeeded();
			ApplySelectedMission();
			#endif
		}

		private void LoadDataIfNeeded()
		{
			if (cachedData != null) return;
			TextAsset json = Resources.Load<TextAsset>("MissionData");
			if (json == null)
			{
				Debug.LogError("EmailMissionUIManager: 无法在 Resources 中加载 MissionData.json (应命名为 MissionData 无扩展名)");
				cachedData = new MissionDataRoot { missions = new List<Mission>() };
				return;
			}
			try
			{
				cachedData = JsonUtility.FromJson<MissionDataRoot>(json.text);
				if (cachedData == null || cachedData.missions == null)
				{
					cachedData = new MissionDataRoot { missions = new List<Mission>() };
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"EmailMissionUIManager: 解析 MissionData.json 失败: {ex.Message}");
				cachedData = new MissionDataRoot { missions = new List<Mission>() };
			}
		}

		public void SetSelectedMissionId(int missionId)
		{
			selectedMissionId = missionId;
			ApplySelectedMission();
		}

		private void ApplySelectedMission()
		{
			if (cachedData == null || cachedData.missions == null || cachedData.missions.Count == 0) return;
			Mission mission = cachedData.missions.Find(m => m.id == selectedMissionId);
			if (mission == null)
			{
				// 若找不到 ID，则退回到列表第一个（避免空白）
				mission = cachedData.missions[0];
			}

			// 标题 name
			if (headerNameTMP != null)
			{
				headerNameTMP.text = mission.name ?? string.Empty;
			}

			// 描述 description
			if (descriptionTMP != null)
			{
				descriptionTMP.text = mission.description ?? string.Empty;
			}

			// 标题图标保持 Email 预制体上的原始设置，不在此处覆盖

			// 奖励区
			if (mission.reward != null)
			{
				ApplyPrize(prizeMoney, mission.reward.money, mission.reward.moneyIconPath);
				ApplyPrize(prizeWeapon, mission.reward.weapon, mission.reward.weaponIconPath);
				ApplyPrize(prizeFood, mission.reward.food, mission.reward.foodIconPath);
				ApplyPrize(prizeIntelligence, mission.reward.intelligence, mission.reward.intelligenceIconPath);
			}

			// Requirements 行：
			int reqInt = mission.requirements != null ? mission.requirements.intelligence : 0;
			int reqItem = mission.requirements != null ? mission.requirements.itemCount : 0;
			ApplyRequirementRow(requirementsIntelligence, reqInt, "intelligence");
			ApplyRequirementRow(requirementsItem, reqItem, "item");

			// 兼容：若仍设置了旧的 requirementsValueTMP，则复用 intelligence 显示
			if (requirementsValueTMP != null)
			{
				requirementsValueTMP.text = reqInt > 0 ? $"intelligence {reqInt}/{reqInt}" : string.Empty;
			}

			// 使用 isCompleted 更新完成图标的可见性，确保其值被读取（消除 CS0414）
			if (completeImage != null)
			{
				completeImage.enabled = isCompleted && completeImage.sprite != null;
			}
		}

		private void ApplyPrize(PrizeRefs prize, int value, string iconPath)
		{
			if (prize == null) return;
			bool show = value > 0;
			if (prize.root != null) prize.root.SetActive(show);
			if (!show) return;

			if (prize.valueTMP != null)
			{
				prize.valueTMP.text = value.ToString();
			}
			if (prize.icon != null)
			{
				var sprite = LoadSpriteSafe(iconPath);
				prize.icon.sprite = sprite;
				prize.icon.enabled = sprite != null;
			}
		}

		private static Sprite LoadSpriteSafe(string resourcePath)
		{
			if (string.IsNullOrWhiteSpace(resourcePath)) return null;
			return Resources.Load<Sprite>(resourcePath);
		}

		private void AutoBindReferences()
		{
			// 基于 Email.prefab 的当前结构按名称自动查找，减少手动接线
			Transform t = transform;
			if (headerNameTMP == null)
			{
				var headerText = t.Find("MissionEmail/Text (TMP)");
				if (headerText != null) headerNameTMP = headerText.GetComponent<TextMeshProUGUI>();
			}
			if (headerIconImage == null)
			{
				var headerIcon = t.Find("MissionEmail/Icon");
				if (headerIcon != null) headerIconImage = headerIcon.GetComponent<Image>();
			}
			if (descriptionTMP == null)
			{
				var desc = t.Find("Image/description");
				if (desc != null) descriptionTMP = desc.GetComponent<TextMeshProUGUI>();
			}

			BindPrize(ref prizeMoney, "Image/Prize/money");
			BindPrize(ref prizeWeapon, "Image/Prize/weapon");
			BindPrize(ref prizeFood, "Image/Prize/food");
			BindPrize(ref prizeIntelligence, "Image/Prize/intelligence");

			// Requirements 行自动绑定
			BindRequirementRow(ref requirementsIntelligence, "Image/Requirements/Intelligence");
			BindRequirementRow(ref requirementsItem, "Image/Requirements/Item");
			// 兼容旧字段
			if (requirementsValueTMP == null && requirementsIntelligence != null)
			{
				requirementsValueTMP = requirementsIntelligence.valueTMP;
			}

			// 完成图标自动绑定
			if (completeImage == null)
			{
				var complete = t.Find("MissionEmail/Complete");
				if (complete != null) completeImage = complete.GetComponent<Image>();
			}
		}

		/// <summary>
		/// 将当前 Email 任务标记为已完成（供外部调用，如提交按钮）。
		/// </summary>
		public void MarkCompleted()
		{
			isCompleted = true;
			// 如需可视化完成状态，可在此处更新样式或触发事件
		}

		/// <summary>
		/// 根据提交结果显示完成/失败图标
		/// </summary>
		public void ShowCompletionResult(bool success)
		{
			if (completeImage == null)
			{
				AutoBindReferences();
			}
			if (completeImage == null) return;

			Sprite target = null;
			if (success)
			{
				target = completeSuccessSprite != null ? completeSuccessSprite : Resources.Load<Sprite>(successSpriteResourceName);
				isCompleted = true;
			}
			else
			{
				target = completeFailSprite != null ? completeFailSprite : Resources.Load<Sprite>(failSpriteResourceName);
			}

			completeImage.sprite = target;
			completeImage.enabled = target != null;
		}

		private void ApplyRequirementRow(RequirementRowRefs refs, int value, string label)
		{
			if (refs == null) return;
			bool show = value > 0;
			if (refs.rawRoot != null) refs.rawRoot.SetActive(show);
			if (!show) return;
			if (refs.valueTMP != null)
			{
				refs.valueTMP.text = $"{label} {value}/{value}";
			}
		}

		private void BindRequirementRow(ref RequirementRowRefs refs, string rootPath)
		{
			if (refs == null) refs = new RequirementRowRefs();
			var root = transform.Find(rootPath);
			if (root != null)
			{
				refs.rawRoot = root.gameObject;
				if (refs.valueTMP == null)
				{
					var text = root.Find("Text (TMP)");
					if (text != null) refs.valueTMP = text.GetComponent<TextMeshProUGUI>();
				}
			}
		}

		private void BindPrize(ref PrizeRefs prize, string rootPath)
		{
			if (prize == null) prize = new PrizeRefs();
			var root = transform.Find(rootPath);
			if (root != null)
			{
				prize.root = root.gameObject;
				if (prize.icon == null)
				{
					var icon = root.Find("PrizeIcon");
					if (icon != null) prize.icon = icon.GetComponent<Image>();
				}
				if (prize.valueTMP == null)
				{
					var text = root.Find("Text (TMP)");
					if (text != null) prize.valueTMP = text.GetComponent<TextMeshProUGUI>();
				}
			}
		}
	}
}


