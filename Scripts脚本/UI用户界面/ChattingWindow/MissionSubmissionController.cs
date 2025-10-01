using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using InventorySystem.SpawnSystem;

/// <summary>
/// 任务物品提交控制器
/// 管理任务物品网格、验证物品ID、触发Stage3对话、发放奖励
/// </summary>
public class MissionSubmissionController : MonoBehaviour
{
	[Header("UI引用")]
	[FieldLabel("任务物品网格")]
	[SerializeField] private ItemGrid missionItemGrid;
	[FieldLabel("提交按钮")]
	[SerializeField] private Button submitButton;
	[FieldLabel("对话控制器")]
	[SerializeField] private ChatBubbleController chatController;
	[FieldLabel("提示文本")]
	[SerializeField] private TextMeshProUGUI hintText;

	[Header("奖励生成")]
	[FieldLabel("奖励物品网格")]
	[Tooltip("用于放置任务奖励物品的网格")]
	[SerializeField] private ItemGrid rewardItemGrid;
	
	[FieldLabel("Logan Stryke奖励配置")]
	[Tooltip("Logan Stryke的情报物品奖励生成配置（2个随机情报物品）")]
	[SerializeField] private RandomItemSpawnConfig loganStrykeRewardConfig;

	[Header("按钮状态颜色")]
	[FieldLabel("禁用颜色（灰色）")]
	[SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
	[FieldLabel("错误颜色（黄色）")]
	[SerializeField] private Color warningColor = new Color(1f, 0.9f, 0.3f, 1f);
	[FieldLabel("正确颜色（绿色）")]
	[SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 1f);

	[Header("任务数据")]
	[FieldLabel("任务数据JSON")]
	[SerializeField] private string missionDataResourceName = "MissionData";

	// 运行时数据
	private MissionDataCollection missionDatabase;
	private Image submitButtonImage;
	private int currentMissionId = -1;
	private int requiredItemId = -1;

	private void Awake()
	{
		// 获取按钮的Image组件
		if (submitButton != null)
		{
			submitButtonImage = submitButton.GetComponent<Image>();
		}

		// 加载任务数据
		LoadMissionDatabase();
	}

	private void Start()
	{
		// 绑定按钮事件
		if (submitButton != null)
		{
			submitButton.onClick.AddListener(OnSubmitButtonClicked);
		}

		// 初始化按钮状态
		UpdateButtonState();
	}

	private void OnEnable()
	{
		// ChatBubbleController会在加载对话后主动调用RefreshMissionInfo()
		// 这里不需要做任何操作
	}

	private void Update()
	{
		// 实时更新按钮状态
		UpdateButtonState();
	}

	/// <summary>
	/// 加载任务数据库
	/// </summary>
	private void LoadMissionDatabase()
	{
		if (string.IsNullOrEmpty(missionDataResourceName))
		{
			Debug.LogWarning("[MissionSubmission] 未设置任务数据资源名");
			return;
		}

		var textAsset = Resources.Load<TextAsset>(missionDataResourceName);
		if (textAsset == null)
		{
			Debug.LogError($"[MissionSubmission] 未找到任务数据: {missionDataResourceName}");
			return;
		}

		try
		{
			missionDatabase = JsonUtility.FromJson<MissionDataCollection>(textAsset.text);
			Debug.Log($"[MissionSubmission] 成功加载任务数据");
		}
		catch (Exception e)
		{
			Debug.LogError($"[MissionSubmission] 解析任务数据失败: {e.Message}");
		}
	}

	/// <summary>
	/// 刷新当前任务信息
	/// </summary>
	public void RefreshMissionInfo()
	{
		if (chatController == null)
		{
			Debug.LogWarning("[MissionSubmission] 未设置ChatBubbleController引用");
			return;
		}

		currentMissionId = chatController.GetCurrentMissionId();
		requiredItemId = GetRequiredItemId(currentMissionId);

		Debug.Log($"[MissionSubmission] 当前任务ID: {currentMissionId}, 需求物品ID: {requiredItemId}");
	}

	/// <summary>
	/// 从任务数据中获取需求物品ID
	/// 使用手动JSON解析来处理item字典
	/// </summary>
	private int GetRequiredItemId(int missionId)
	{
		if (missionDatabase == null)
		{
			Debug.LogWarning("[MissionSubmission] 任务数据库为空");
			return -1;
		}

		MissionData targetMission = GetMissionById(missionId);
		if (targetMission == null)
		{
			Debug.LogWarning($"[MissionSubmission] 未找到任务ID: {missionId}");
			return -1;
		}

		// 手动解析requirements.item中的物品ID
		// 由于Unity JsonUtility不支持Dictionary，我们需要手动从原始JSON中提取
		var textAsset = Resources.Load<TextAsset>(missionDataResourceName);
		if (textAsset == null)
		{
			Debug.LogError($"[MissionSubmission] 无法加载任务数据: {missionDataResourceName}");
			return -1;
		}

		try
		{
			string jsonText = textAsset.text;
			Debug.Log($"[MissionSubmission] 开始解析任务 {missionId} 的需求物品...");

			// 查找当前任务的ID位置
			string searchPattern = $"\"id\": {missionId}";
			int missionStartIndex = jsonText.IndexOf(searchPattern);
			
			if (missionStartIndex == -1)
			{
				Debug.LogWarning($"[MissionSubmission] 在JSON中未找到任务ID: {missionId}");
				return -1;
			}

			Debug.Log($"[MissionSubmission] 找到任务ID位置: {missionStartIndex}");

			// 查找这个任务块的结束位置（下一个任务或数组结束）
			int nextMissionIndex = jsonText.IndexOf("\"id\":", missionStartIndex + searchPattern.Length);
			int missionEndIndex = (nextMissionIndex != -1) ? nextMissionIndex : jsonText.Length;

			// 在当前任务块中查找requirements
			int requirementsIndex = jsonText.IndexOf("\"requirements\"", missionStartIndex);
			if (requirementsIndex == -1 || requirementsIndex > missionEndIndex)
			{
				Debug.LogWarning($"[MissionSubmission] 任务 {missionId} 没有requirements字段");
				return -1;
			}

			Debug.Log($"[MissionSubmission] 找到requirements位置: {requirementsIndex}");

			// 在requirements中查找item字段
			int itemIndex = jsonText.IndexOf("\"item\"", requirementsIndex);
			if (itemIndex == -1 || itemIndex > missionEndIndex)
			{
				Debug.LogWarning($"[MissionSubmission] 任务 {missionId} 的requirements中没有item字段");
				return -1;
			}

			Debug.Log($"[MissionSubmission] 找到item字段位置: {itemIndex}");

			// 查找item字段的值部分: "item": { "1402": "..." }
			int itemColonIndex = jsonText.IndexOf(":", itemIndex + 6);
			int itemBraceIndex = jsonText.IndexOf("{", itemColonIndex);
			
			if (itemBraceIndex == -1 || itemBraceIndex > missionEndIndex)
			{
				Debug.LogWarning($"[MissionSubmission] 无法找到item对象的开始括号");
				return -1;
			}

			// 查找第一个物品ID（在引号中）
			int firstQuoteIndex = jsonText.IndexOf("\"", itemBraceIndex + 1);
			if (firstQuoteIndex == -1 || firstQuoteIndex > missionEndIndex)
			{
				Debug.LogWarning($"[MissionSubmission] 无法找到物品ID的开始引号");
				return -1;
			}

			int secondQuoteIndex = jsonText.IndexOf("\"", firstQuoteIndex + 1);
			if (secondQuoteIndex == -1 || secondQuoteIndex > missionEndIndex)
			{
				Debug.LogWarning($"[MissionSubmission] 无法找到物品ID的结束引号");
				return -1;
			}

			string itemIdStr = jsonText.Substring(firstQuoteIndex + 1, secondQuoteIndex - firstQuoteIndex - 1);
			Debug.Log($"[MissionSubmission] 提取到的物品ID字符串: '{itemIdStr}'");

			if (int.TryParse(itemIdStr, out int itemId))
			{
				Debug.Log($"[MissionSubmission] ? 任务 {missionId} 需要物品ID: {itemId}");
				return itemId;
			}
			else
			{
				Debug.LogError($"[MissionSubmission] 无法将 '{itemIdStr}' 解析为整数");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[MissionSubmission] 解析物品需求时发生异常: {e.Message}\n{e.StackTrace}");
		}

		return -1;
	}

	/// <summary>
	/// 更新提交按钮状态
	/// </summary>
	private void UpdateButtonState()
	{
		if (submitButton == null || missionItemGrid == null) return;

		int submittedItemId = GetSubmittedItemId();

		// 网格为空 - 禁用按钮（灰色）
		if (submittedItemId == -1)
		{
			submitButton.interactable = false;
			if (submitButtonImage != null)
			{
				submitButtonImage.color = disabledColor;
			}
			SetHintText("Place the mission item here...");
			return;
		}

		// 仅在ID不匹配时输出调试信息
		// Debug.Log($"[MissionSubmission] 比对物品ID - 提交: {submittedItemId}, 需求: {requiredItemId}");

		// 物品ID不匹配 - 警告（黄色）
		if (submittedItemId != requiredItemId)
		{
			submitButton.interactable = false;
			if (submitButtonImage != null)
			{
				submitButtonImage.color = warningColor;
			}
			SetHintText($"Wrong item! Need ID: {requiredItemId}, Got: {submittedItemId}");
			return;
		}

		// 物品ID正确 - 可提交（绿色）
		submitButton.interactable = true;
		if (submitButtonImage != null)
		{
			submitButtonImage.color = validColor;
		}
		SetHintText("Ready to submit!");
	}

	/// <summary>
	/// 获取任务网格中的物品ID
	/// </summary>
	private int GetSubmittedItemId()
	{
		if (missionItemGrid == null)
		{
			Debug.LogWarning("[MissionSubmission] 任务网格为空");
			return -1;
		}

		// 获取网格中的第一个物品
		Item item = missionItemGrid.GetItemAt(0, 0);
		if (item == null)
		{
			Debug.Log("[MissionSubmission] 网格中没有物品");
			return -1;
		}

		// 获取物品的ItemDataReader组件
		ItemDataReader reader = item.GetComponent<ItemDataReader>();
		if (reader == null)
		{
			Debug.LogWarning("[MissionSubmission] 物品没有ItemDataReader组件");
			return -1;
		}

		if (reader.ItemData == null)
		{
			Debug.LogWarning("[MissionSubmission] ItemDataReader的ItemData为空");
			return -1;
		}

		int itemId = reader.ItemData.id;
		// Debug.Log($"[MissionSubmission] 网格中的物品ID: {itemId} ({reader.ItemData.itemName})");
		return itemId;
	}

	/// <summary>
	/// 设置提示文本
	/// </summary>
	private void SetHintText(string text)
	{
		if (hintText != null)
		{
			hintText.text = text;
		}
	}

	/// <summary>
	/// 提交按钮点击事件
	/// </summary>
	private void OnSubmitButtonClicked()
	{
		int submittedItemId = GetSubmittedItemId();

		// 再次验证
		if (submittedItemId != requiredItemId)
		{
			Debug.LogWarning($"[MissionSubmission] 物品ID不匹配！提交: {submittedItemId}, 需求: {requiredItemId}");
			SetHintText("Item mismatch! Please check the requirement.");
			return;
		}

		// 移除物品
		Item item = missionItemGrid.GetItemAt(0, 0);
		if (item != null)
		{
			// 销毁物品GameObject
			Destroy(item.gameObject);
			Debug.Log($"[MissionSubmission] 已移除物品ID: {submittedItemId}");
		}

		// 触发Stage3对话
		if (chatController != null)
		{
			SetHintText("Mission complete! Talking to NPC...");
			chatController.AdvanceToStage3();
			
			// 对话完成后发放奖励（延迟调用）
			StartCoroutine(DelayedRewardDistribution());
		}
		else
		{
			Debug.LogError("[MissionSubmission] 未设置ChatBubbleController引用！");
		}
	}

	/// <summary>
	/// 延迟发放奖励（等待对话结束）
	/// </summary>
	private IEnumerator DelayedRewardDistribution()
	{
		// 等待Stage3对话完成（假设3句话，每句2秒 + 额外2秒）
		float estimatedDialogueTime = 8f;
		yield return new WaitForSeconds(estimatedDialogueTime);

		// 发放奖励
		DistributeReward(currentMissionId);
	}

	/// <summary>
	/// 发放任务奖励
	/// </summary>
	private void DistributeReward(int missionId)
	{
		if (missionDatabase == null)
		{
			Debug.LogWarning("[MissionSubmission] 任务数据库未加载，无法发放奖励");
			return;
		}

		MissionData mission = GetMissionById(missionId);
		if (mission == null)
		{
			Debug.LogWarning($"[MissionSubmission] 未找到任务ID: {missionId}");
			return;
		}

		Debug.Log($"[MissionSubmission] 发放任务 {missionId} ({mission.name}) 的奖励:");
		
		if (mission.reward.money > 0)
		{
			Debug.Log($"  + {mission.reward.money} 金币");
			// TODO: 调用货币管理器增加金币
			// CurrencyManager.Instance.AddMoney(mission.reward.money);
		}

		if (mission.reward.intelligence > 0)
		{
			Debug.Log($"  + {mission.reward.intelligence} 情报物品");
			// 生成随机情报物品
			GenerateIntelligenceRewards(missionId, mission.publisher);
		}

		if (mission.reward.discount > 0)
		{
			Debug.Log($"  + {mission.reward.discount}% 商店折扣");
			// 将折扣应用到 AM(Asher Myles) 的交易网格
			try
			{
				var shopGen = FindObjectOfType<DialogueShopGenerator>(true);
				if (shopGen != null)
				{
					shopGen.ActivateAsherDiscount(mission.reward.discount);
					Debug.Log("[MissionSubmission] 已激活 Asher 折扣，正在应用到交易物品...");
				}
				else
				{
					Debug.LogWarning("[MissionSubmission] 未找到 DialogueShopGenerator，无法应用 Asher 折扣");
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[MissionSubmission] 应用 Asher 折扣时异常: {e.Message}");
			}
		}

		if (mission.reward.weapon > 0)
		{
			Debug.Log($"  + {mission.reward.weapon} 武器");
			// TODO: 调用物品管理器发放武器
			// ItemManager.Instance.GiveWeapon(mission.reward.weapon);
		}

		if (mission.reward.food > 0)
		{
			Debug.Log($"  + {mission.reward.food} 食物");
			// TODO: 调用物品管理器发放食物
			// ItemManager.Instance.GiveFood(mission.reward.food);
		}

		SetHintText("Reward distributed!");
	}

	/// <summary>
	/// 生成情报物品奖励
	/// </summary>
	private void GenerateIntelligenceRewards(int missionId, string npcPublisher)
	{
		if (rewardItemGrid == null)
		{
			Debug.LogWarning("[MissionSubmission] 未设置奖励物品网格，无法生成情报奖励");
			return;
		}

		// 根据NPC名称选择对应的奖励配置
		RandomItemSpawnConfig rewardConfig = GetRewardConfigForNPC(npcPublisher);
		
		if (rewardConfig == null)
		{
			Debug.LogError($"[MissionSubmission] 未找到NPC '{npcPublisher}' 的奖励配置，请在Inspector中设置");
			return;
		}

		Debug.Log($"[MissionSubmission] 使用奖励配置: {rewardConfig.configName}");

		// 使用 ShelfRandomItemManager 生成奖励物品
		var spawnManager = ShelfRandomItemManager.Instance;
		if (spawnManager == null)
		{
			Debug.LogError("[MissionSubmission] 无法找到 ShelfRandomItemManager 实例");
			return;
		}

		// 创建一个临时GameObject作为"货架"标识
		GameObject tempShelf = new GameObject($"MissionReward_{missionId}_{npcPublisher}");
		tempShelf.transform.SetParent(rewardItemGrid.transform);

		// 强制生成奖励物品（忽略会话状态）
		bool success = spawnManager.ForceRegenerateItems(tempShelf, rewardItemGrid, rewardConfig);

		if (success)
		{
			Debug.Log($"[MissionSubmission] 成功开始生成任务 {missionId} 的情报奖励");
		}
		else
		{
			Debug.LogWarning($"[MissionSubmission] 生成任务 {missionId} 的情报奖励失败");
			Destroy(tempShelf);
		}
	}

	/// <summary>
	/// 根据NPC名称获取对应的奖励配置
	/// </summary>
	private RandomItemSpawnConfig GetRewardConfigForNPC(string npcPublisher)
	{
		// 根据NPC名称返回对应的配置
		switch (npcPublisher)
		{
			case "Logan Stryke":
				return loganStrykeRewardConfig;
			
			case "Asher Myles":
				// Asher Myles的奖励是折扣，不需要生成物品
				Debug.Log("[MissionSubmission] Asher Myles的奖励是折扣，无需物品生成配置");
				return null;
			
			default:
				Debug.LogWarning($"[MissionSubmission] 未配置NPC '{npcPublisher}' 的物品奖励");
				return null;
		}
	}

	/// <summary>
	/// 根据任务ID获取任务数据
	/// </summary>
	private MissionData GetMissionById(int missionId)
	{
		if (missionDatabase == null) return null;

		// 搜索旧格式任务列表（兼容性）
		if (missionDatabase.missions != null)
		{
			foreach (var mission in missionDatabase.missions)
			{
				if (mission.id == missionId) return mission;
			}
		}

		// 搜索主线任务
		if (missionDatabase.mainMissions != null)
		{
			foreach (var mission in missionDatabase.mainMissions)
			{
				if (mission.id == missionId) return mission;
			}
		}

		// 搜索支线任务
		if (missionDatabase.sideMissions != null)
		{
			foreach (var mission in missionDatabase.sideMissions)
			{
				if (mission.id == missionId) return mission;
			}
		}

		return null;
	}
}
