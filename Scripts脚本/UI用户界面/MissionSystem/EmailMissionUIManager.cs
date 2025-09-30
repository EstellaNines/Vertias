using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

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

		[Header("提交判定服务 (可选: 提供智力总值与物品查询/消耗)")]
		[SerializeField] private MonoBehaviour submissionServiceBehaviour; // 需实现 IMissionSubmissionService
		private IMissionSubmissionService submissionService;

		private MissionDataRoot cachedData;
		private string rawJsonText; // 保存原始 JSON 文本，用于解析自定义 item 映射
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
			// 运行时填充：从原始 JSON 中解析出的物品需求
			public List<int> requiredItemIds;
			public List<string> requiredItemNames;
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
			public int itemCount; // 兼容旧字段，不再使用
		}
		#endregion

		private void Reset()
		{
			AutoBindReferences();
		}

		private void Awake()
		{
			LoadDataIfNeeded();
			ResolveSubmissionService();
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
				rawJsonText = json.text;
				cachedData = JsonUtility.FromJson<MissionDataRoot>(rawJsonText);
				if (cachedData == null || cachedData.missions == null)
				{
					cachedData = new MissionDataRoot { missions = new List<Mission>() };
				}
				// 尝试为每个 mission 解析 item 要求（支持 { "item": {"1405": "Name"} } 结构）
				for (int i = 0; i < cachedData.missions.Count; i++)
				{
					var m = cachedData.missions[i];
					ExtractItemRequirementsForMission(rawJsonText, m);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"EmailMissionUIManager: 解析 MissionData.json 失败: {ex.Message}");
				cachedData = new MissionDataRoot { missions = new List<Mission>() };
			}
		}

		private void ResolveSubmissionService()
		{
			if (submissionService != null) return;
			if (submissionServiceBehaviour != null)
			{
				submissionService = submissionServiceBehaviour as IMissionSubmissionService;
			}
			if (submissionService == null)
			{
				// 场景中查找任意实现该接口的组件
				var behaviours = FindObjectsOfType<MonoBehaviour>(true);
				foreach (var b in behaviours)
				{
					if (b is IMissionSubmissionService svc)
					{
						submissionService = svc;
						break;
					}
				}
			}
		}

		private static void ExtractItemRequirementsForMission(string rawJson, Mission mission)
		{
			if (mission == null || string.IsNullOrEmpty(rawJson)) return;
			try
			{
				string idSearch = "\"id\": " + mission.id;
				int idxId = rawJson.IndexOf(idSearch, StringComparison.Ordinal);
				if (idxId < 0) return;
				int idxReq = rawJson.IndexOf("\"requirements\"", idxId, StringComparison.Ordinal);
				if (idxReq < 0) return;
				int idxItemKey = rawJson.IndexOf("\"item\"", idxReq, StringComparison.Ordinal);
				if (idxItemKey < 0) return; // 无 item 要求
				int start = rawJson.IndexOf('{', idxItemKey);
				if (start < 0) return;
				int end = FindMatchingBrace(rawJson, start);
				if (end <= start) return;
				string obj = rawJson.Substring(start + 1, end - start - 1);
				var ids = new List<int>();
				var names = new List<string>();
				var matches = Regex.Matches(obj, "\\\"(\\d+)\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
				foreach (Match match in matches)
				{
					if (!match.Success || match.Groups.Count < 3) continue;
					if (int.TryParse(match.Groups[1].Value, out int id))
					{
						ids.Add(id);
						names.Add(match.Groups[2].Value);
					}
				}
				mission.requiredItemIds = ids;
				mission.requiredItemNames = names;
			}
			catch { /* 忽略解析异常，保持兼容 */ }
		}

		private static int FindMatchingBrace(string text, int openIndex)
		{
			int depth = 0;
			for (int i = openIndex; i < text.Length; i++)
			{
				char c = text[i];
				if (c == '{') depth++;
				else if (c == '}')
				{
					depth--;
					if (depth == 0) return i;
				}
			}
			return -1;
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
			// 若 requiredItem 未解析，则尝试就地解析
			if ((mission.requiredItemIds == null || mission.requiredItemIds.Count == 0) && !string.IsNullOrEmpty(rawJsonText))
			{
				ExtractItemRequirementsForMission(rawJsonText, mission);
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
			ApplyRequirementRow(requirementsIntelligence, reqInt, "intelligence");
			// item：展示名称列表
			if (requirementsItem != null)
			{
				bool hasItems = mission.requiredItemNames != null && mission.requiredItemNames.Count > 0;
				if (requirementsItem.rawRoot != null) requirementsItem.rawRoot.SetActive(hasItems);
				if (hasItems && requirementsItem.valueTMP != null)
				{
					requirementsItem.valueTMP.text = "item: " + string.Join(", ", mission.requiredItemNames);
				}
			}

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

		/// <summary>
		/// 当前所选任务是否满足提交条件（需要：情报值达到要求 且 所需物品全部具备）。
		/// </summary>
		public bool CanSubmitCurrentMission()
		{
			if (cachedData == null || cachedData.missions == null) return false;
			var mission = cachedData.missions.Find(m => m.id == selectedMissionId);
			if (mission == null) return false;
			int reqInt = mission.requirements != null ? mission.requirements.intelligence : 0;
			bool intelOk = submissionService != null ? submissionService.GetPlayerIntelligenceTotal() >= reqInt : true; // 若无服务，默认通过情报校验以便测试
			bool itemsOk = true;
			if (mission.requiredItemIds != null && mission.requiredItemIds.Count > 0)
			{
				if (submissionService == null) return false; // 有物品要求但无服务，视为不满足
				foreach (var id in mission.requiredItemIds)
				{
					if (!submissionService.HasItem(id, 1)) { itemsOk = false; break; }
				}
			}
			return intelOk && itemsOk;
		}

		/// <summary>
		/// 尝试提交当前任务：若满足条件则消耗所需物品并显示完成图标。
		/// </summary>
		public bool TrySubmitCurrentMission()
		{
			bool can = CanSubmitCurrentMission();
			if (!can)
			{
				ShowCompletionResult(false);
				return false;
			}
			var mission = cachedData.missions.Find(m => m.id == selectedMissionId);
			if (mission == null) return false;
			// 消耗物品
			if (mission.requiredItemIds != null && mission.requiredItemIds.Count > 0)
			{
				foreach (var id in mission.requiredItemIds)
				{
					if (!submissionService.TryConsumeItem(id, 1))
					{
						ShowCompletionResult(false);
						return false;
					}
				}
			}
			ShowCompletionResult(true);
			return true;
		}

		/// <summary>
		/// 获取当前选中任务所需的物品ID列表（来自 MissionData.json 的 requirements.item）。
		/// 若无则返回空列表。
		/// </summary>
		public System.Collections.Generic.IReadOnlyList<int> GetRequiredItemIdsForSelectedMission()
		{
			LoadDataIfNeeded();
			var mission = cachedData != null && cachedData.missions != null
				? cachedData.missions.Find(m => m.id == selectedMissionId)
				: null;
			if (mission == null)
			{
				return System.Array.Empty<int>();
			}
			if ((mission.requiredItemIds == null || mission.requiredItemIds.Count == 0) && !string.IsNullOrEmpty(rawJsonText))
			{
				ExtractItemRequirementsForMission(rawJsonText, mission);
			}
			return mission.requiredItemIds != null ? mission.requiredItemIds.AsReadOnly() : System.Array.Empty<int>();
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


