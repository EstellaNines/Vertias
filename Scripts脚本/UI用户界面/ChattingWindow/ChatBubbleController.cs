using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using DG.Tweening;

// 对话数据结构
[Serializable]
public class DialogNode
{
	public string id;
	public string title;
	public string text;
	public string next; // 可为 null
}

[Serializable]
public class NpcTalkData
{
	public string npcId;
	public string npcName;
	public string avatar; // Resources 路径或名称，例如 "AsherMyles"
	public int missionId; // 关联的任务ID
	public List<DialogNode> dialogs;
	public List<DialogNode> stage2_1; // 二阶段：分支 2-1（肯定）
	public List<DialogNode> stage2_2; // 二阶段：分支 2-2（否定）
	public List<DialogNode> stage3; // 三阶段：任务完成
}

[Serializable]
public class PlayerChoice
{
	public string id; // "1" 肯定, "2" 否定
	public string text;
}

[Serializable]
public class PlayerTalkData
{
	public string playerId;
	public string playerName;
	public string avatar; // 可覆盖玩家头像
	public List<PlayerChoice> choices;
}

public class ChatBubbleController : MonoBehaviour
{
	[Header("References")]
	[FieldLabel("聊天区域")]
	[SerializeField] private RectTransform chatRegion; // 设定好的 ChatRegion（包含 VerticalLayoutGroup）
	[FieldLabel("NPC对话预制体")]
	[SerializeField] private GameObject npcTextBubblePrefab; // 对话人 TextBubble 预制体
	[FieldLabel("滚动视图")]
	[SerializeField] private ScrollRect scrollRect; // 可选：用于自动滚动到底部

	[Header("Settings")]
	[FieldLabel("自动滚动到底部")]
	[SerializeField] private bool autoScrollToBottom = true;
	[FieldLabel("进入界面自动播放")]
	[SerializeField] private bool autoPlayOnEnable = true;
	[FieldLabel("气泡时间间隔(秒)")]
	[SerializeField] private float messageIntervalSeconds = 2f;
	[FieldLabel("左边距")]
	[SerializeField] private float leftPadding = 0f; // 左边距
	[FieldLabel("上边距")]
	[SerializeField] private float topPadding = 0f; // 顶部边距
	[FieldLabel("垂直间距")]
	[SerializeField] private float verticalSpacing = 8f; // 气泡之间的间距
	[FieldLabel("对话结束额外高度")]
	[SerializeField] private float endExtraHeight = 500f; // 结束后额外扩展高度

	[Header("Test Data")]
	[TextArea]
	[SerializeField] private List<string> testMessages = new List<string>();

	[Header("Dialogue JSON")]
	[FieldLabel("对话JSON资源名")]
	[SerializeField] private string dialogueResourceName = "AsherMylesTalkData"; // 直接引用 Resources 下 JSON（无扩展名）
	[FieldLabel("起始对话ID")]
	[SerializeField] private string startDialogId = "1"; // 默认从 1 开始

	[Header("Avatar")]
	[FieldLabel("左侧头像子物体名")]
	[SerializeField] private string avatarImageChildName = "LeftIcon"; // 预制体里用于显示头像的子物体名
	[FieldLabel("右侧头像子物体名")]
	[SerializeField] private string playerAvatarImageChildName = "RightIcon"; // 玩家按钮预制体里的头像位
	[FieldLabel("玩家头像资源路径")]
	[SerializeField] private string playerAvatarResourceName = "Portraits/JohnClaw"; // 玩家头像资源路径

	[Header("Player Answer UI")]
	[FieldLabel("玩家回答预制体")]
	[SerializeField] private GameObject playerAnswerPrefab; // 玩家选择按钮预制体
	[FieldLabel("右边距")]
	[SerializeField] private float rightPadding = 0f; // 右边距（用于右侧控件）
	[FieldLabel("玩家选项JSON资源名")]
	[SerializeField] private string playerChoicesResourceName = "JohnClawTalkData"; // 玩家选项文本 JSON
	[FieldLabel("玩家对话预制体")]
	[SerializeField] private GameObject playerTextBubblePrefab; // 玩家选择后生成的右侧文字泡

	[Header("Tween Settings - Bubbles")] 
	[FieldLabel("对话入场时长(秒)")]
	[SerializeField] private float bubbleTweenDuration = 0.25f;
	[FieldLabel("对话滑入偏移(px)")]
	[SerializeField] private float bubbleSlideOffset = 30f;
	[SerializeField] private Ease bubbleEase = Ease.OutQuad;

	[Header("Tween Settings - Answer Buttons")] 
	[FieldLabel("按钮入场时长(秒)")]
	[SerializeField] private float answerTweenDuration = 0.25f;
	[FieldLabel("按钮滑入偏移(px)")]
	[SerializeField] private float answerSlideOffset = 30f;
	[SerializeField] private Ease answerEase = Ease.OutQuad;
	[FieldLabel("悬停放大倍率")]
	[SerializeField] private float hoverScale = 1.08f;
	[FieldLabel("悬停放大时长(秒)")]
	[SerializeField] private float hoverDuration = 0.12f;
	[SerializeField] private Ease hoverEase = Ease.OutQuad;

	[Header("Save/Load - ES3")]
	[FieldLabel("启用进度保存")]
	[SerializeField] private bool enableProgressSave = true;
	[FieldLabel("保存文件名(ES3)")]
	[SerializeField] private string progressSaveFile = "DialogueProgress.es3";
	[FieldLabel("保存键前缀")]
	[SerializeField] private string progressKeyPrefix = "ChatProgress_";

	private NpcTalkData loadedDialogue;
	private Dictionary<string, DialogNode> idToNode = new Dictionary<string, DialogNode>(StringComparer.Ordinal);
	private bool dialogueLoaded = false;
	private int currentDialogIndex = -1; // 简化结构：顺序推进
	private Sprite npcAvatarSprite;
	private Sprite playerAvatarSprite;
	private bool awaitingPlayerAnswer = false;
	private List<DialogNode> activeDialogList; // 当前播放的对话列表（阶段1或阶段2分支）
	private PlayerTalkData playerTalkData;
	private Dictionary<string, string> playerChoiceTextById = new Dictionary<string, string>(StringComparer.Ordinal);
	private GameObject currentAnswerUI; // 当前的玩家按钮UI实例
	private bool manualAdvanceByButtons = false; // 阶段2是否由按钮驱动推进
	// 已移除结束标记功能
	private Coroutine autoPlayRoutine;
	private Coroutine delayedAnswerButtonsRoutine;
	private bool endExtended = false;

	private int nextMessageIndex = 0;
	private float accumulatedHeight = 0f; // 已用高度累计（从上往下）

	// 按钮调用：每按一次生成一个对话人文字泡
	public void SpawnNextBubble()
	{
		// 优先从 JSON 播放对话
		string message = GetNextJsonLine();
		if (!string.IsNullOrEmpty(message))
		{
			SpawnBubble(message);
			// 如果当前已经是阶段2并且刚好播完最后一条，立刻扩展区域（不依赖下一次轮询）
			if (!ReferenceEquals(activeDialogList, loadedDialogue.dialogs) && activeDialogList != null && currentDialogIndex >= activeDialogList.Count && !awaitingPlayerAnswer)
			{
				StartCoroutine(ExtendAfterFrame());
			}
			return;
		}

		// 若正在等待玩家回答，则不回退到测试数据
		if (awaitingPlayerAnswer)
		{
			return;
		}

		// 回退到测试数据（如未加载到 JSON 或已结束）
		message = GetNextTestMessage();
		if (!string.IsNullOrEmpty(message))
		{
			SpawnBubble(message);
		}
	}

	private void OnEnable()
	{
		// 先尝试加载进度（无需等待自动播放）
		if (enableProgressSave)
		{
			// 确保已加载对话数据
			EnsureDialogueLoaded();
			LoadProgressIfAny();
		}
		if (autoPlayOnEnable)
		{
			StartAutoPlay();
		}
	}

	private void OnDisable()
	{
		// 保存当前进度
		if (enableProgressSave)
		{
			SaveProgress();
		}
		StopAutoPlay();
    if (delayedAnswerButtonsRoutine != null)
    {
        StopCoroutine(delayedAnswerButtonsRoutine);
        delayedAnswerButtonsRoutine = null;
    }
	}

	// 外部也可直接调用以生成自定义文本
	public void SpawnBubble(string message)
	{
		if (npcTextBubblePrefab == null)
		{
			Debug.LogWarning("[ChatBubbleController] npcTextBubblePrefab 未设置。");
			return;
		}
		if (chatRegion == null)
		{
			Debug.LogWarning("[ChatBubbleController] chatRegion 未设置。");
			return;
		}

		GameObject instance = Instantiate(npcTextBubblePrefab, chatRegion);
		var bubbleRect = instance.GetComponent<RectTransform>();
		var tmp = instance.GetComponentInChildren<TextMeshProUGUI>(true);
		if (tmp != null) tmp.text = message;

		// 设置左侧头像（若存在名为 avatarImageChildName 的 Image）
		var iconTrans = instance.transform.Find(avatarImageChildName);
		if (iconTrans != null)
		{
			var icon = iconTrans.GetComponent<Image>();
			if (icon != null)
			{
				icon.sprite = npcAvatarSprite;
				icon.enabled = npcAvatarSprite != null;
			}
		}

		// 顶左对齐（无 LayoutGroup 手动堆叠）
		if (bubbleRect != null)
		{
			AlignTopLeft(bubbleRect);
			// 先重建以拿到正确尺寸（Sizer 已设置文本并刷新）
			ForceLayoutRebuild(bubbleRect);
			float height = GetBubbleHeight(bubbleRect, tmp);
			// 目标位置（最终）
			Vector2 finalPos = new Vector2(leftPadding, -(topPadding + accumulatedHeight));
			// 先放置到最终位置以便计算累计高度
			bubbleRect.anchoredPosition = finalPos;
			accumulatedHeight += height + verticalSpacing;
			UpdateChatRegionHeight(topPadding + accumulatedHeight);

			// 入场动画：从左侧滑入 + 淡入 + 轻微缩放
			var cg = instance.GetComponent<CanvasGroup>();
			if (cg == null) cg = instance.AddComponent<CanvasGroup>();
			cg.alpha = 0f;
			bubbleRect.localScale = Vector3.one * 0.95f;
			bubbleRect.anchoredPosition = finalPos + new Vector2(-bubbleSlideOffset, 0f);
			Sequence seq = DOTween.Sequence();
			seq.Join(bubbleRect.DOAnchorPos(finalPos, bubbleTweenDuration).SetEase(bubbleEase))
			   .Join(cg.DOFade(1f, bubbleTweenDuration).SetEase(Ease.Linear))
			   .Join(bubbleRect.DOScale(1f, bubbleTweenDuration).SetEase(bubbleEase));
		}

		// 刷新整体以避免首帧定位误差
		ForceLayoutRebuild(chatRegion);

		if (autoScrollToBottom) ScrollToBottom();
	}

	public void ClearAll()
	{
		if (chatRegion == null) return;
		for (int i = chatRegion.childCount - 1; i >= 0; i--)
		{
			Destroy(chatRegion.GetChild(i).gameObject);
		}
		nextMessageIndex = 0;
		accumulatedHeight = 0f;
		UpdateChatRegionHeight(0f);
		ResetProgressToStart();
		awaitingPlayerAnswer = false;
		// removed end marker state
		if (delayedAnswerButtonsRoutine != null)
		{
			StopCoroutine(delayedAnswerButtonsRoutine);
			delayedAnswerButtonsRoutine = null;
		}
		endExtended = false;
		if (autoScrollToBottom)
		{
			ScrollToBottom();
		}
	}

	public void StartAutoPlay()
	{
		StopAutoPlay();
		autoPlayRoutine = StartCoroutine(AutoPlayLoop());
	}

	public void StopAutoPlay()
	{
		if (autoPlayRoutine != null)
		{
			StopCoroutine(autoPlayRoutine);
			autoPlayRoutine = null;
		}
	}

	private IEnumerator AutoPlayLoop()
	{
		// 初次进入等待一帧，保证布局
		yield return null;
		// 首次延迟
		float delay = messageIntervalSeconds;
		while (true)
		{
			if (!awaitingPlayerAnswer && HasMoreMessagesToPlay())
			{
				yield return new WaitForSeconds(delay);
				// 若等待期间进入选择或无消息，跳过
				if (awaitingPlayerAnswer || !HasMoreMessagesToPlay())
				{
					delay = messageIntervalSeconds;
					continue;
				}
				SpawnNextBubble();
				// 后续循环继续使用固定间隔
				delay = messageIntervalSeconds;
			}
			else
			{
				yield return null;
			}
		}
	}

	private bool HasMoreMessagesToPlay()
	{
		if (!EnsureDialogueLoaded()) return false;
		if (activeDialogList == null || activeDialogList.Count == 0) return false;
		return currentDialogIndex >= 0 && currentDialogIndex < activeDialogList.Count;
	}

	// 一句话一个对话框：按句号/问号/感叹号/省略号切分，并逐句生成气泡
	public void SpawnBubblesPerSentence(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return;
		foreach (var sentence in SplitSentences(text))
		{
			SpawnBubble(sentence);
		}
	}

	// 批量输入（每个条目可包含多句）
	public void SpawnBubblesPerSentences(IEnumerable<string> texts)
	{
		if (texts == null) return;
		foreach (var t in texts)
		{
			SpawnBubblesPerSentence(t);
		}
	}

	private string GetNextTestMessage()
	{
		if (testMessages != null && testMessages.Count > 0)
		{
			string msg = testMessages[nextMessageIndex % testMessages.Count];
			nextMessageIndex++;
			return msg;
		}
		return "测试消息";
	}

	// ============ JSON 加载与逐句推进 ============
	public void SetDialogueResource(string resourceNameWithoutExt)
	{
		if (string.IsNullOrEmpty(resourceNameWithoutExt))
		{
			Debug.LogWarning("[ChatBubbleController] 资源名为空。");
			return;
		}
		dialogueResourceName = resourceNameWithoutExt;
		dialogueLoaded = false;
		LoadDialogue();
	}

	// 根据 NPC Id 设定对应的 JSON（可按需扩展映射）
	public void SetNpcById(string npcId)
	{
		switch (npcId)
		{
			case "AsherMyles":
				SetDialogueResource("AsherMylesTalkData");
				break;
			case "LoganStryke":
				SetDialogueResource("LoganStrykeTalkData");
				break;
			default:
				Debug.LogWarning($"[ChatBubbleController] 未知 NPC Id: {npcId}");
				break;
		}
	}

	private bool EnsureDialogueLoaded()
	{
		if (dialogueLoaded && loadedDialogue != null) return true;
		LoadDialogue();
		return loadedDialogue != null;
	}

	private void LoadDialogue()
	{
		idToNode.Clear();
		loadedDialogue = null;
		npcAvatarSprite = null;
		playerAvatarSprite = null;

		if (string.IsNullOrEmpty(dialogueResourceName))
		{
			Debug.LogWarning("[ChatBubbleController] dialogueResourceName 未设置。");
			dialogueLoaded = false;
			return;
		}

		var ta = Resources.Load<TextAsset>(dialogueResourceName);
		if (ta == null)
		{
			Debug.LogWarning($"[ChatBubbleController] 未找到 JSON 资源: {dialogueResourceName}");
			dialogueLoaded = false;
			return;
		}

		try
		{
			loadedDialogue = JsonUtility.FromJson<NpcTalkData>(ta.text);
		}
		catch (Exception e)
		{
			Debug.LogError($"[ChatBubbleController] 解析 JSON 失败: {e.Message}");
			loadedDialogue = null;
		}

		if (loadedDialogue == null || loadedDialogue.dialogs == null || loadedDialogue.dialogs.Count == 0)
		{
			Debug.LogWarning("[ChatBubbleController] 对话数据为空或无节点。");
			dialogueLoaded = false;
			return;
		}


		// 加载玩家选项与头像
		LoadPlayerChoices();

		// 加载 NPC 头像
		if (!string.IsNullOrEmpty(loadedDialogue.avatar))
		{
			npcAvatarSprite = Resources.Load<Sprite>(loadedDialogue.avatar);
			if (npcAvatarSprite == null)
			{
				Debug.LogWarning($"[ChatBubbleController] 未能加载头像 Sprite: {loadedDialogue.avatar}");
			}
		}

		// 设置当前活动列表为阶段1
		activeDialogList = loadedDialogue.dialogs;
		ResetProgressToStart();
		dialogueLoaded = true;

		// 尝试应用已保存的进度
		LoadProgressIfAny();

		// 通知MissionSubmissionController对话已加载
		NotifyMissionSubmissionController();
	}

	/// <summary>
	/// 通知MissionSubmissionController对话已加载
	/// </summary>
	private void NotifyMissionSubmissionController()
	{
		// 查找场景中的MissionSubmissionController
		var missionController = FindObjectOfType<MissionSubmissionController>();
		if (missionController != null)
		{
			Debug.Log("[ChatBubbleController] 通知MissionSubmissionController刷新任务信息");
			missionController.RefreshMissionInfo();
		}
	}

	private void ResetProgressToStart()
	{
		currentDialogIndex = -1;
		if (activeDialogList != null && activeDialogList.Count > 0)
		{
			// 若提供 startDialogId，则从该 id 开始，否则从 0 开始
			if (!string.IsNullOrEmpty(startDialogId))
			{
				int found = activeDialogList.FindIndex(n => n != null && n.id == startDialogId);
				currentDialogIndex = found >= 0 ? found : 0;
			}
			else
			{
				currentDialogIndex = 0;
			}
		}
		else
		{
			currentDialogIndex = -1;
		}
	}

	private string GetNextJsonLine()
	{
		if (!EnsureDialogueLoaded()) return null;
		if (activeDialogList == null || activeDialogList.Count == 0) return null;
		if (currentDialogIndex < 0 || currentDialogIndex >= activeDialogList.Count)
		{
			// 阶段列表播放完毕：如果是阶段1，无条件生成玩家回答按钮
			if (!awaitingPlayerAnswer && ReferenceEquals(activeDialogList, loadedDialogue.dialogs))
			{
				awaitingPlayerAnswer = true;
				// 延迟若干秒后再生成玩家回答按钮
				if (delayedAnswerButtonsRoutine != null)
				{
					StopCoroutine(delayedAnswerButtonsRoutine);
				}
				delayedAnswerButtonsRoutine = StartCoroutine(DelayedSpawnAnswerButtons(messageIntervalSeconds));
			}
			else
			{
				// 阶段2或其它列表结束：扩展区域确保长文本可视
				ExtendRegionAfterEnd();
			}
			return null;
		}
		var node = activeDialogList[currentDialogIndex++];
    // 如果这是阶段1的最后一句，预先安排延迟生成玩家按钮
    if (!awaitingPlayerAnswer && ReferenceEquals(activeDialogList, loadedDialogue.dialogs)
        && currentDialogIndex >= activeDialogList.Count && currentAnswerUI == null)
    {
        awaitingPlayerAnswer = true;
        if (delayedAnswerButtonsRoutine != null)
        {
            StopCoroutine(delayedAnswerButtonsRoutine);
        }
        delayedAnswerButtonsRoutine = StartCoroutine(DelayedSpawnAnswerButtons(messageIntervalSeconds));
    }
		return node != null ? node.text : null;
	}

	// ==================== 进度保存/加载（ES3） ====================
	private string GetProgressKey()
	{
		// 以NPC唯一ID作为键，避免不同UI激活/隐藏造成键冲突
		string npcIdKey = loadedDialogue != null && !string.IsNullOrEmpty(loadedDialogue.npcId)
			? loadedDialogue.npcId
			: (!string.IsNullOrEmpty(dialogueResourceName) ? dialogueResourceName : "_unknown");
		return progressKeyPrefix + npcIdKey;
	}

	private string GetCurrentStageKey()
	{
		if (loadedDialogue == null) return "dialogs";
		if (ReferenceEquals(activeDialogList, loadedDialogue.dialogs)) return "dialogs";
		if (loadedDialogue.stage2_1 != null && ReferenceEquals(activeDialogList, loadedDialogue.stage2_1)) return "stage2_1";
		if (loadedDialogue.stage2_2 != null && ReferenceEquals(activeDialogList, loadedDialogue.stage2_2)) return "stage2_2";
		if (loadedDialogue.stage3 != null && ReferenceEquals(activeDialogList, loadedDialogue.stage3)) return "stage3";
		return "dialogs";
	}

	private void SaveProgress()
	{
		if (!enableProgressSave || !dialogueLoaded || loadedDialogue == null) return;
		try
		{
			string keyBase = GetProgressKey();
			string stageKey = GetCurrentStageKey();
			// 保存阶段、索引、是否在等待选择
			ES3.Save<string>($"{keyBase}.stage", stageKey, progressSaveFile);
			ES3.Save<int>($"{keyBase}.index", Mathf.Max(0, currentDialogIndex), progressSaveFile);
			ES3.Save<bool>($"{keyBase}.await", awaitingPlayerAnswer, progressSaveFile);
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[ChatBubbleController] 保存进度失败: {e.Message}");
		}
	}

	private void LoadProgressIfAny()
	{
		if (!enableProgressSave || loadedDialogue == null) return;
		try
		{
			string keyBase = GetProgressKey();
			if (!ES3.KeyExists($"{keyBase}.stage", progressSaveFile)) return;

			string stageKey = ES3.Load<string>($"{keyBase}.stage", progressSaveFile, "dialogs");
			int savedIndex = ES3.Load<int>($"{keyBase}.index", progressSaveFile, 0);
			bool savedAwait = ES3.Load<bool>($"{keyBase}.await", progressSaveFile, false);

			// 应用阶段
			List<DialogNode> targetList = loadedDialogue.dialogs;
			switch (stageKey)
			{
				case "stage2_1":
					targetList = (loadedDialogue.stage2_1 != null && loadedDialogue.stage2_1.Count > 0) ? loadedDialogue.stage2_1 : loadedDialogue.dialogs;
					break;
				case "stage2_2":
					targetList = (loadedDialogue.stage2_2 != null && loadedDialogue.stage2_2.Count > 0) ? loadedDialogue.stage2_2 : loadedDialogue.dialogs;
					break;
				case "stage3":
					targetList = (loadedDialogue.stage3 != null && loadedDialogue.stage3.Count > 0) ? loadedDialogue.stage3 : loadedDialogue.dialogs;
					break;
				default:
					targetList = loadedDialogue.dialogs;
					break;
			}

			activeDialogList = targetList;
			// 应用索引（指向下一句）
			int maxCount = activeDialogList != null ? activeDialogList.Count : 0;
			currentDialogIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, maxCount));

			// 应用等待选择状态（仅对阶段1有效）
			awaitingPlayerAnswer = savedAwait && ReferenceEquals(activeDialogList, loadedDialogue.dialogs);

			// 若界面是空的，则重建已显示的历史内容
			TryRebuildHistory(stageKey, savedIndex);
			if (awaitingPlayerAnswer && (activeDialogList == null || currentDialogIndex >= (activeDialogList?.Count ?? 0)))
			{
				// 立即（轻微延迟）生成玩家回答按钮
				if (delayedAnswerButtonsRoutine != null)
				{
					StopCoroutine(delayedAnswerButtonsRoutine);
				}
				delayedAnswerButtonsRoutine = StartCoroutine(DelayedSpawnAnswerButtons(0.1f));
			}
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[ChatBubbleController] 加载进度失败: {e.Message}");
		}
	}

	/// <summary>
	/// 在载入存档后，如果当前聊天区域没有内容，则重建至保存位置之前的消息。
	/// 对于阶段2，会先回放一次玩家的选择气泡。
	/// </summary>
	private void TryRebuildHistory(string stageKey, int savedIndex)
	{
		if (chatRegion == null || activeDialogList == null) return;
		if (chatRegion.childCount > 0) return; // 已有内容则不重建，避免重复

		// 阶段2：先回放玩家选择（根据分支推断文案）
		if (string.Equals(stageKey, "stage2_1", StringComparison.Ordinal) || string.Equals(stageKey, "stage2_2", StringComparison.Ordinal))
		{
			// 确保玩家选项已加载（用于头像与文案）
			if (playerTalkData == null) LoadPlayerChoices();
			string choiceId = string.Equals(stageKey, "stage2_1", StringComparison.Ordinal) ? "1" : "2";
			string choiceText = GetPlayerChoiceText(choiceId);
			if (!string.IsNullOrEmpty(choiceText))
			{
				SpawnPlayerTextBubble(choiceText);
			}
		}

		// 回放本阶段已显示的NPC气泡
		int limit = Mathf.Clamp(savedIndex, 0, activeDialogList.Count);
		for (int i = 0; i < limit; i++)
		{
			var node = activeDialogList[i];
			if (node != null && !string.IsNullOrEmpty(node.text))
			{
				SpawnBubble(node.text);
			}
		}
	}

	private bool HasStage2Content()
	{
		return (loadedDialogue != null) &&
			((loadedDialogue.stage2_1 != null && loadedDialogue.stage2_1.Count > 0) ||
			 (loadedDialogue.stage2_2 != null && loadedDialogue.stage2_2.Count > 0));
	}

	private void SpawnPlayerAnswerButtons()
	{
		if (playerAnswerPrefab == null || chatRegion == null)
		{
			Debug.LogWarning("[ChatBubbleController] playerAnswerPrefab 或 chatRegion 未设置。");
			return;
		}
		// 确保玩家选项已加载
		if (playerTalkData == null)
		{
			LoadPlayerChoices();
		}
		GameObject instance = Instantiate(playerAnswerPrefab, chatRegion);
		currentAnswerUI = instance;
		var rect = instance.GetComponent<RectTransform>();
		// 设置右上对齐
		if (rect != null)
		{
			AlignTopRight(rect);
			ForceLayoutRebuild(rect);
			float height = GetBubbleHeight(rect, null);
			Vector2 finalPos = new Vector2(-rightPadding, -(topPadding + accumulatedHeight));
			rect.anchoredPosition = finalPos;
			accumulatedHeight += height + verticalSpacing;
			UpdateChatRegionHeight(topPadding + accumulatedHeight);

			// 入场动画：从右侧滑入 + 淡入 + 轻微缩放
			var cg = instance.GetComponent<CanvasGroup>();
			if (cg == null) cg = instance.AddComponent<CanvasGroup>();
			cg.alpha = 0f;
			rect.localScale = Vector3.one * 0.95f;
			rect.anchoredPosition = finalPos + new Vector2(answerSlideOffset, 0f);
			Sequence seq = DOTween.Sequence();
			seq.Join(rect.DOAnchorPos(finalPos, answerTweenDuration).SetEase(answerEase))
			   .Join(cg.DOFade(1f, answerTweenDuration).SetEase(Ease.Linear))
			   .Join(rect.DOScale(1f, answerTweenDuration).SetEase(answerEase));
		}

		// 头像设置（玩家头像）
		var iconTrans = instance.transform.Find(playerAvatarImageChildName);
		if (iconTrans != null)
		{
			var icon = iconTrans.GetComponent<Image>();
			if (icon != null)
			{
				icon.sprite = playerAvatarSprite;
				icon.enabled = playerAvatarSprite != null;
			}
		}

		// 绑定按钮事件（按名称查找，找不到则取前两个 Button 作为正/负）
		Button positive = null, negative = null;
		var buttons = instance.GetComponentsInChildren<Button>(true);
		for (int i = 0; i < buttons.Length; i++)
		{
			var b = buttons[i];
			if (b.name.Equals("PositiveButton", StringComparison.OrdinalIgnoreCase)) positive = b;
			else if (b.name.Equals("NegativeButton", StringComparison.OrdinalIgnoreCase)) negative = b;
		}
		if (positive == null && buttons.Length > 0) positive = buttons[0];
		if (negative == null && buttons.Length > 1) negative = buttons[1];
		if (positive != null)
		{
			positive.onClick.RemoveAllListeners();
			positive.onClick.AddListener(() => OnPlayerAnswer(true));
			// 设置按钮文本
			var posText = positive.GetComponentInChildren<TextMeshProUGUI>(true);
			if (posText != null)
			{
				posText.text = GetPlayerChoiceText("1");
			}
			// 悬停缩放
			var hover = positive.gameObject.GetComponent<UIHoverScale>();
			if (hover == null) hover = positive.gameObject.AddComponent<UIHoverScale>();
			hover.targetScale = hoverScale;
			hover.duration = hoverDuration;
			hover.ease = hoverEase;
		}
		if (negative != null)
		{
			negative.onClick.RemoveAllListeners();
			negative.onClick.AddListener(() => OnPlayerAnswer(false));
			var negText = negative.GetComponentInChildren<TextMeshProUGUI>(true);
			if (negText != null)
			{
				negText.text = GetPlayerChoiceText("2");
			}
			var hover2 = negative.gameObject.GetComponent<UIHoverScale>();
			if (hover2 == null) hover2 = negative.gameObject.AddComponent<UIHoverScale>();
			hover2.targetScale = hoverScale;
			hover2.duration = hoverDuration;
			hover2.ease = hoverEase;
		}

		ForceLayoutRebuild(chatRegion);
		if (autoScrollToBottom) ScrollToBottom();
	}

	private void OnPlayerAnswer(bool affirmative)
	{
		awaitingPlayerAnswer = false;
		if (delayedAnswerButtonsRoutine != null)
		{
			StopCoroutine(delayedAnswerButtonsRoutine);
			delayedAnswerButtonsRoutine = null;
		}
		string branch = affirmative ? "2-1" : "2-2";
		Debug.Log($"[ChatBubbleController] Player Answer: {(affirmative ? "YES" : "NO")} -> enter stage {branch}");
		// 先将玩家选择以右侧文字泡形式显示
		string chosenText = GetPlayerChoiceText(affirmative ? "1" : "2");
		if (!string.IsNullOrEmpty(chosenText))
		{
			SpawnPlayerTextBubble(chosenText);
		}
		// 移除当前按钮UI
		if (currentAnswerUI != null)
		{
			Destroy(currentAnswerUI);
			currentAnswerUI = null;
		}
		// 切换到二阶段分支对话
		var nextList = affirmative ? loadedDialogue.stage2_1 : loadedDialogue.stage2_2;
		if (nextList == null || nextList.Count == 0)
		{
			Debug.LogWarning("[ChatBubbleController] 目标分支无对话内容。");
			// 没有阶段2内容：保持静默
			ExtendRegionAfterEnd();
			return;
		}
		activeDialogList = nextList;
		startDialogId = null; // 二阶段从第一条开始
		ResetProgressToStart();
		manualAdvanceByButtons = false; // 改为自动定时推进
		// 等待 2 秒后再播放二阶段第一句（按全局间隔）
		StartCoroutine(DelayedSpawnNext(messageIntervalSeconds));
	}

	private IEnumerator DelayedSpawnNext(float delay)
	{
		yield return new WaitForSeconds(delay);
		SpawnNextBubble();
	}

	private IEnumerator ExtendAfterFrame()
	{
		yield return null;
		ExtendRegionAfterEnd();
	}

	private IEnumerator DelayedSpawnAnswerButtons(float delay)
	{
		yield return new WaitForSeconds(delay);
		// 仍在等待玩家回答且尚未生成UI时生成
		if (awaitingPlayerAnswer && currentAnswerUI == null)
		{
			SpawnPlayerAnswerButtons();
		}
		delayedAnswerButtonsRoutine = null;
	}

	private void ExtendRegionAfterEnd()
	{
		if (endExtended) return;
		endExtended = true;
		float current = chatRegion != null ? chatRegion.rect.height : 0f;
		float baseHeight = Mathf.Max(current, topPadding + accumulatedHeight);
		float target = baseHeight + Mathf.Max(0f, endExtraHeight);
		UpdateChatRegionHeight(target);
		ForceLayoutRebuild(chatRegion);
		if (autoScrollToBottom) ScrollToBottom();
	}

	private void ConfigureAnswerButtonsForAdvance()
	{
		if (currentAnswerUI == null) return;
		var buttons = currentAnswerUI.GetComponentsInChildren<Button>(true);
		Button positive = null, negative = null;
		for (int i = 0; i < buttons.Length; i++)
		{
			var b = buttons[i];
			if (b.name.Equals("PositiveButton", StringComparison.OrdinalIgnoreCase)) positive = b;
			else if (b.name.Equals("NegativeButton", StringComparison.OrdinalIgnoreCase)) negative = b;
		}
		if (positive == null && buttons.Length > 0) positive = buttons[0];
		if (negative == null && buttons.Length > 1) negative = buttons[1];
		if (positive != null)
		{
			positive.onClick.RemoveAllListeners();
			positive.onClick.AddListener(AdvanceStage2Next);
			var posText = positive.GetComponentInChildren<TextMeshProUGUI>(true);
			if (posText != null) posText.text = "继续";
		}
		if (negative != null)
		{
			negative.onClick.RemoveAllListeners();
			negative.onClick.AddListener(AdvanceStage2Next);
			var negText = negative.GetComponentInChildren<TextMeshProUGUI>(true);
			if (negText != null) negText.text = "继续";
		}
	}

	private void AdvanceStage2Next()
	{
		if (!manualAdvanceByButtons)
		{
			return;
		}
		int before = currentDialogIndex;
		SpawnNextBubble();
		// 若已到末尾，禁用按钮
		if (activeDialogList == null || currentDialogIndex >= (activeDialogList?.Count ?? 0))
		{
			manualAdvanceByButtons = false;
			if (currentAnswerUI != null)
			{
				var buttons = currentAnswerUI.GetComponentsInChildren<Button>(true);
				for (int i = 0; i < buttons.Length; i++)
				{
					buttons[i].interactable = false;
				}
			}
		}
	}

	private void AlignTopRight(RectTransform r)
	{
		if (r == null) return;
		r.anchorMin = new Vector2(1f, 1f);
		r.anchorMax = new Vector2(1f, 1f);
		r.pivot = new Vector2(1f, 1f);
	}

	private void SpawnPlayerTextBubble(string message)
	{
		if (playerTextBubblePrefab == null || chatRegion == null)
		{
			Debug.LogWarning("[ChatBubbleController] playerTextBubblePrefab 或 chatRegion 未设置。");
			return;
		}
		GameObject instance = Instantiate(playerTextBubblePrefab, chatRegion);
		var rect = instance.GetComponent<RectTransform>();
		var tmp = instance.GetComponentInChildren<TextMeshProUGUI>(true);
		if (tmp != null) tmp.text = message;

		// 设置右侧头像（玩家头像）
		var iconTrans = instance.transform.Find(playerAvatarImageChildName);
		if (iconTrans != null)
		{
			var icon = iconTrans.GetComponent<Image>();
			if (icon != null)
			{
				icon.sprite = playerAvatarSprite;
				icon.enabled = playerAvatarSprite != null;
			}
		}

		if (rect != null)
		{
			AlignTopRight(rect);
			ForceLayoutRebuild(rect);
			float height = GetBubbleHeight(rect, tmp);
			Vector2 finalPos = new Vector2(-rightPadding, -(topPadding + accumulatedHeight));
			rect.anchoredPosition = finalPos;
			accumulatedHeight += height + verticalSpacing;
			UpdateChatRegionHeight(topPadding + accumulatedHeight);

			// 入场动画（与按钮一致，从右侧滑入 + 淡入）
			var cg = instance.GetComponent<CanvasGroup>();
			if (cg == null) cg = instance.AddComponent<CanvasGroup>();
			cg.alpha = 0f;
			rect.localScale = Vector3.one * 0.95f;
			rect.anchoredPosition = finalPos + new Vector2(answerSlideOffset, 0f);
			Sequence seq = DOTween.Sequence();
			seq.Join(rect.DOAnchorPos(finalPos, answerTweenDuration).SetEase(answerEase))
			   .Join(cg.DOFade(1f, answerTweenDuration).SetEase(Ease.Linear))
			   .Join(rect.DOScale(1f, answerTweenDuration).SetEase(answerEase));
		}

		ForceLayoutRebuild(chatRegion);
		if (autoScrollToBottom) ScrollToBottom();
	}

// 删除结束标记生成函数

	private void ScrollToBottom()
	{
		if (scrollRect == null)
		{
			return;
		}
		StartCoroutine(ScrollToBottomNextFrame());
	}

	private IEnumerator ScrollToBottomNextFrame()
	{
		yield return null; // 等待一帧以确保布局更新完成
		Canvas.ForceUpdateCanvases();
		if (scrollRect != null)
		{
			scrollRect.verticalNormalizedPosition = 0f; // 0 表示底部
		}
	}

	private void ForceLayoutRebuild(RectTransform target)
	{
		if (target == null) return;
		LayoutRebuilder.ForceRebuildLayoutImmediate(target);
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(target);
	}

	private void AlignTopLeft(RectTransform r)
	{
		if (r == null) return;
		r.anchorMin = new Vector2(0f, 1f);
		r.anchorMax = new Vector2(0f, 1f);
		r.pivot = new Vector2(0f, 1f);
	}

	private float GetBubbleHeight(RectTransform r, TextMeshProUGUI tmp)
	{
		if (r == null) return 0f;
		float h = r.rect.height;
		if (h <= 0f) h = LayoutUtility.GetPreferredHeight(r);
		if (h <= 0f && tmp != null) h = tmp.preferredHeight;
		return Mathf.Max(1f, h);
	}

	private void UpdateChatRegionHeight(float targetHeight)
	{
		if (chatRegion == null) return;
		float h = Mathf.Max(targetHeight, 0f);
		chatRegion.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
	}

	// 中文与英文常见句末符，不在顿号/逗号处切分
	private static readonly Regex sentenceChunkRegex = new Regex(
		"([^。！？!?…]+(?:[。！？!?…]+|$))",
		RegexOptions.Compiled
	);

	private IEnumerable<string> SplitSentences(string text)
	{
		var matches = sentenceChunkRegex.Matches(text);
		for (int i = 0; i < matches.Count; i++)
		{
			string chunk = matches[i].Value.Trim();
			if (!string.IsNullOrEmpty(chunk))
			{
				yield return chunk;
			}
		}
	}

	private void LoadPlayerChoices()
	{
		playerTalkData = null;
		playerChoiceTextById.Clear();
		if (string.IsNullOrEmpty(playerChoicesResourceName)) return;
		var ta = Resources.Load<TextAsset>(playerChoicesResourceName);
		if (ta == null)
		{
			Debug.LogWarning($"[ChatBubbleController] 未找到玩家选项 JSON: {playerChoicesResourceName}");
			// 至少加载玩家头像
			if (playerAvatarSprite == null && !string.IsNullOrEmpty(playerAvatarResourceName))
			{
				playerAvatarSprite = Resources.Load<Sprite>(playerAvatarResourceName);
			}
			return;
		}
		try
		{
			playerTalkData = JsonUtility.FromJson<PlayerTalkData>(ta.text);
		}
		catch (Exception e)
		{
			Debug.LogError($"[ChatBubbleController] 解析玩家选项 JSON 失败: {e.Message}");
			playerTalkData = null;
		}
		if (playerTalkData != null)
		{
			if (playerTalkData.choices != null)
			{
				for (int i = 0; i < playerTalkData.choices.Count; i++)
				{
					var c = playerTalkData.choices[i];
					if (c != null && !string.IsNullOrEmpty(c.id) && !playerChoiceTextById.ContainsKey(c.id))
					{
						playerChoiceTextById.Add(c.id, c.text ?? string.Empty);
					}
				}
			}
			// 覆盖玩家头像（若 JSON 提供）
			if (!string.IsNullOrEmpty(playerTalkData.avatar))
			{
				var spr = Resources.Load<Sprite>(playerTalkData.avatar);
				if (spr != null) playerAvatarSprite = spr;
			}
			// 若仍未有头像，使用 Inspector 的路径
			if (playerAvatarSprite == null && !string.IsNullOrEmpty(playerAvatarResourceName))
			{
				playerAvatarSprite = Resources.Load<Sprite>(playerAvatarResourceName);
			}
		}
	}

	private string GetPlayerChoiceText(string id)
	{
		if (string.IsNullOrEmpty(id)) return string.Empty;
		if (playerChoiceTextById != null && playerChoiceTextById.TryGetValue(id, out var t))
		{
			return t ?? string.Empty;
		}
		// 默认文案
		return id == "1" ? "Yes" : (id == "2" ? "No" : string.Empty);
	}

	/// <summary>
	/// 推进到第三阶段对话（任务完成）
	/// 外部调用：由 MissionSubmissionController 在任务物品提交后触发
	/// </summary>
	public void AdvanceToStage3()
	{
		if (loadedDialogue == null)
		{
			Debug.LogWarning("[ChatBubbleController] 无法推进到Stage3：未加载对话数据");
			return;
		}

		if (loadedDialogue.stage3 == null || loadedDialogue.stage3.Count == 0)
		{
			Debug.LogWarning("[ChatBubbleController] 无法推进到Stage3：该NPC没有Stage3对话");
			return;
		}

		// 切换到 Stage 3 对话
		activeDialogList = loadedDialogue.stage3;
		currentDialogIndex = 0;
		ResetProgressToStart();

		Debug.Log($"[ChatBubbleController] 推进到Stage3，共 {activeDialogList.Count} 句对话");

		// 启动自动播放
		StartAutoPlay();
	}

	/// <summary>
	/// 获取当前加载的对话数据
	/// </summary>
	public NpcTalkData GetLoadedDialogue()
	{
		return loadedDialogue;
	}

	/// <summary>
	/// 获取当前NPC的任务ID
	/// </summary>
	public int GetCurrentMissionId()
	{
		return loadedDialogue?.missionId ?? -1;
	}
}


