using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 对话触发器 - 玩家进入触发器范围后按F键打开/关闭对话系统
/// 支持选择不同NPC以加载对应的JSON对话数据
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
	[Header("NPC设置")]
	[FieldLabel("NPC标识")]
	[Tooltip("选择要对话的NPC，将自动加载对应的JSON对话文本")]
	[SerializeField] private NPCIdentifier npcId = NPCIdentifier.AsherMyles;
	
	[Header("对话系统引用")]
	[FieldLabel("对话系统控制器")]
	[Tooltip("DialogueSystemController组件，留空则自动查找")]
	[SerializeField] private DialogueSystemController dialogueSystemController;
	
	[FieldLabel("对话控制器")]
	[Tooltip("ChatBubbleController组件，留空则自动查找")]
	[SerializeField] private ChatBubbleController chatController;

	[Header("交易生成")]
	[FieldLabel("对话商店生成器")]
	[SerializeField] private DialogueShopGenerator shopGenerator;
	
	[Header("输入系统")]
	[FieldLabel("玩家输入控制器")]
	[Tooltip("用于监听F键（Operate）输入，留空则自动查找")]
	[SerializeField] private PlayerInputController playerInputController;
	
	[Header("UI提示")]
	[FieldLabel("提示文本组件")]
	[Tooltip("显示'[F] Talk'提示的TextMeshPro组件")]
	[SerializeField] private TextMeshProUGUI hintText;
	
	[FieldLabel("提示文本内容")]
	[SerializeField] private string hintMessage = "[F] Talk";
	
	[Header("调试设置")]
	[FieldLabel("显示调试日志")]
	[SerializeField] private bool showDebugLog = true;
	
	// NPC标识枚举
	public enum NPCIdentifier
	{
		[InspectorName("Asher Myles")] AsherMyles,
		[InspectorName("Logan Stryke")] LoganStryke,
		[InspectorName("其他NPC")] Other
	}
	
	// 运行时状态
	private bool playerInRange = false;
	private bool isDialogueOpen = false;
	private BoxCollider2D triggerCollider;
	
	#region Unity生命周期
	
	private void Awake()
	{
		// 获取BoxCollider2D组件
		triggerCollider = GetComponent<BoxCollider2D>();
		if (triggerCollider != null)
		{
			triggerCollider.isTrigger = true; // 确保是触发器
		}
		
		// 自动查找引用
		AutoFindReferences();
		
		// 验证必要组件
		ValidateReferences();
	}
	
	private void Start()
	{
		// 延迟一帧确保所有组件初始化完成
		StartCoroutine(InitializeDialogueSystemDelayed());
		
		// 隐藏提示文本
		if (hintText != null)
		{
			hintText.gameObject.SetActive(false);
		}
	}
	
	/// <summary>
	/// 延迟初始化对话系统（确保初始隐藏）
	/// </summary>
	private IEnumerator InitializeDialogueSystemDelayed()
	{
		yield return null; // 等待一帧
		
		// 确保对话系统初始为隐藏状态
		if (dialogueSystemController != null)
		{
			dialogueSystemController.CloseDialogue();
		}
		isDialogueOpen = false;
		
		LogDebug("对话系统初始化完成并已隐藏");
	}
	
	private void OnTriggerEnter2D(Collider2D other)
	{
		// 检查是否是玩家的BoxCollider2D进入
		if (other is BoxCollider2D && other.CompareTag("Player"))
		{
			playerInRange = true;
			LogDebug($"玩家进入对话触发器范围 - NPC: {npcId}");
			
			// 显示提示文本
			if (hintText != null)
			{
				hintText.gameObject.SetActive(true);
				hintText.text = hintMessage;
			}
			
			// 绑定F键事件
			BindOperateEvent();
		}
	}
	
	private void OnTriggerExit2D(Collider2D other)
	{
		// 检查是否是玩家的BoxCollider2D离开
		if (other is BoxCollider2D && other.CompareTag("Player"))
		{
			playerInRange = false;
			LogDebug("玩家离开对话触发器范围");
			
			// 隐藏提示文本
			if (hintText != null)
			{
				hintText.gameObject.SetActive(false);
			}
			
			// 解绑F键事件
			UnbindOperateEvent();
			
			// 离开时自动关闭对话（可选，根据需求调整）
			if (isDialogueOpen)
			{
				CloseDialogue();
			}
		}
	}
	
	private void OnDestroy()
	{
		// 清理事件绑定
		UnbindOperateEvent();
	}
	
	#endregion
	
	#region 自动查找引用
	
	/// <summary>
	/// 自动查找必要的组件引用
	/// </summary>
	private void AutoFindReferences()
	{
		// 查找对话系统控制器
		if (dialogueSystemController == null)
		{
			// 1. 按类型查找（查找场景中的DialogueSystemController）
			dialogueSystemController = FindObjectOfType<DialogueSystemController>(true);
			
			if (dialogueSystemController != null)
			{
				LogDebug($"自动找到对话系统控制器: {dialogueSystemController.gameObject.name}");
			}
			else
			{
				Debug.LogWarning("[DialogueTrigger] 未找到DialogueSystemController，请确保NPCSystem预制体中包含该组件");
			}
		}
		
		// 查找对话控制器（ChatBubbleController）
		if (chatController == null)
		{
			// 优先在对话系统控制器的层级内查找
			if (dialogueSystemController != null)
			{
				chatController = dialogueSystemController.GetComponentInChildren<ChatBubbleController>(true);
			}
			// 回退到全局查找
			if (chatController == null)
			{
				chatController = FindObjectOfType<ChatBubbleController>(true);
			}
			if (chatController != null)
			{
				LogDebug($"自动找到对话控制器: {chatController.gameObject.name}");
			}
		}
		
		// 查找玩家输入控制器
		if (playerInputController == null)
		{
			// 先从场景中查找组件实例
			playerInputController = FindObjectOfType<PlayerInputController>();
			
			if (playerInputController == null)
			{
				// 回退：查找所有已加载的ScriptableObject实例
				var all = Resources.FindObjectsOfTypeAll<PlayerInputController>();
				if (all != null && all.Length > 0)
				{
					playerInputController = all[0];
				}
			}
			
			if (playerInputController != null)
			{
				LogDebug("自动找到玩家输入控制器");
			}
		}

		// 查找商店生成器
		if (shopGenerator == null)
		{
			shopGenerator = FindObjectOfType<DialogueShopGenerator>(true);
		}
	}
	
	/// <summary>
	/// 验证必要的引用
	/// </summary>
	private void ValidateReferences()
	{
		bool hasErrors = false;
		
		if (dialogueSystemController == null)
		{
			Debug.LogError("[DialogueTrigger] 对话系统控制器未找到！请确保场景中存在DialogueSystemController组件", this);
			hasErrors = true;
		}
		
		if (chatController == null)
		{
			Debug.LogWarning("[DialogueTrigger] 对话控制器未找到，请检查场景中是否包含ChatBubbleController组件", this);
			hasErrors = true;
		}
		
		if (playerInputController == null)
		{
			Debug.LogWarning("[DialogueTrigger] 玩家输入控制器未找到，F键功能将不可用", this);
		}
		
		if (triggerCollider == null)
		{
			Debug.LogError("[DialogueTrigger] BoxCollider2D组件缺失！", this);
			hasErrors = true;
		}
		
		if (hasErrors)
		{
			enabled = false;
		}
	}
	
	#endregion
	
	#region F键事件绑定
	
	/// <summary>
	/// 绑定F键（Operate）事件
	/// </summary>
	private void BindOperateEvent()
	{
		if (playerInputController != null)
		{
			playerInputController.onOperate += OnOperatePressed;
			LogDebug("已绑定F键（Operate）事件");
		}
	}
	
	/// <summary>
	/// 解绑F键（Operate）事件
	/// </summary>
	private void UnbindOperateEvent()
	{
		if (playerInputController != null)
		{
			playerInputController.onOperate -= OnOperatePressed;
			LogDebug("已解绑F键（Operate）事件");
		}
	}
	
	/// <summary>
	/// F键按下时的处理
	/// </summary>
	private void OnOperatePressed()
	{
		if (!playerInRange) return;
		
		ToggleDialogue();
	}
	
	#endregion
	
	#region 对话系统控制
	
	/// <summary>
	/// 切换对话系统的打开/关闭状态
	/// </summary>
	private void ToggleDialogue()
	{
		if (isDialogueOpen)
		{
			CloseDialogue();
		}
		else
		{
			OpenDialogue();
		}
	}
	
	/// <summary>
	/// 打开对话系统
	/// </summary>
	private void OpenDialogue()
	{
		if (dialogueSystemController == null || chatController == null)
		{
			Debug.LogWarning("[DialogueTrigger] 无法打开对话：缺少必要引用");
			return;
		}
		// 预热层级：在任何启用与协程之前，先确保关键层级激活
		PrewarmChatHierarchy();
		
		// 在打开前请求控制器跳过初始隐藏（与UITrigger一致）
		dialogueSystemController.PrepareInitialOpen();
		
		// 在打开前先设置NPC资源并清空历史，避免跨NPC混杂
		ApplyNpcBeforeOpen();
		
		// 生成对应NPC的交易物品
		GenerateNpcTradeItems();
		
		// 使用控制器打开对话系统
		dialogueSystemController.OpenDialogue();
		isDialogueOpen = true;
		
		// 再次保障（打开后）
		EnsureChatUIActive();
		
		// 设置NPC对话数据
		SetNPCDialogue();
		
		// 显示鼠标光标（对话时需要）
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		
		// 禁用玩家游戏输入（可选，根据需求调整）
		if (playerInputController != null)
		{
			playerInputController.DisableGameplayInput();
			playerInputController.EnabledUIInput();
		}
		
		LogDebug($"对话系统已打开 - NPC: {npcId}");
	}

	/// <summary>
	/// 在打开前设置 NPC 对话资源并清空历史，避免跨NPC错乱
	/// </summary>
	private void ApplyNpcBeforeOpen()
	{
		if (chatController == null) return;
		// 清空历史
		chatController.ClearAll();
		// 设置 NPC 对话资源
		SetNPCDialogue();
	}

	private void GenerateNpcTradeItems()
	{
		if (shopGenerator == null || chatController == null) return;
		// 从已选择的NPC枚举转成字符串Id（与ChatBubbleController一致）
		string npcIdStr = GetNpcStringId(this.npcId);
		shopGenerator.GenerateForNpc(npcIdStr);
	}

	private string GetNpcStringId(NPCIdentifier id)
	{
		switch (id)
		{
			case NPCIdentifier.AsherMyles: return "AsherMyles";
			case NPCIdentifier.LoganStryke: return "LoganStryke";
			default: return string.Empty;
		}
	}

	/// <summary>
	/// 在打开之前，先把 Canvas / ChatBubbleController / ChattingRegion 激活，避免 OnEnable 内协程失败
	/// </summary>
	private void PrewarmChatHierarchy()
	{
		// Canvas
		if (dialogueSystemController != null)
		{
			var canvas = dialogueSystemController.GetComponentInChildren<Canvas>(true);
			if (canvas != null)
			{
				if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
				canvas.enabled = true;
			}
		}
		// ChatBubbleController 宿主
		if (chatController != null && !chatController.gameObject.activeSelf)
		{
			chatController.gameObject.SetActive(true);
		}
		// 常见聊天区域
		if (chatController != null)
		{
			var region = chatController.transform.Find("ChattingRegion");
			if (region != null && !region.gameObject.activeSelf)
			{
				region.gameObject.SetActive(true);
			}
		}
	}

	/// <summary>
	/// 确保 ChatBubbleController 及关键子层级（如 ChattingRegion）已被激活
	/// </summary>
	private void EnsureChatUIActive()
	{
		if (chatController != null)
		{
			if (!chatController.gameObject.activeInHierarchy)
			{
				chatController.gameObject.SetActive(true);
			}
			// 尝试激活常见命名的聊天区域
			var region = chatController.transform.Find("ChattingRegion");
			if (region != null && !region.gameObject.activeSelf)
			{
				region.gameObject.SetActive(true);
			}
		}
	}
	
	/// <summary>
	/// 关闭对话系统
	/// </summary>
	private void CloseDialogue()
	{
		if (dialogueSystemController == null)
		{
			Debug.LogWarning("[DialogueTrigger] 无法关闭对话：对话系统控制器为空");
			return;
		}
		
		// 使用控制器关闭对话系统
		dialogueSystemController.CloseDialogue();
		isDialogueOpen = false;
		
		// 恢复游戏输入
		if (playerInputController != null)
		{
			playerInputController.EnabledGameplayInput();
			// 保持UI输入启用以支持其他UI
		}
		
		LogDebug("对话系统已关闭");
	}
	
	/// <summary>
	/// 根据选择的NPC设置对话数据
	/// </summary>
	private void SetNPCDialogue()
	{
		if (chatController == null) return;
		
		string resourceName = GetDialogueResourceName();
		if (string.IsNullOrEmpty(resourceName))
		{
			Debug.LogWarning($"[DialogueTrigger] 未找到NPC '{npcId}' 对应的对话资源");
			return;
		}
		
		// 设置对话资源
		chatController.SetDialogueResource(resourceName);
		
		LogDebug($"已设置NPC对话资源: {resourceName}");
	}
	
	/// <summary>
	/// 根据NPC标识获取对话资源名称
	/// </summary>
	private string GetDialogueResourceName()
	{
		switch (npcId)
		{
			case NPCIdentifier.AsherMyles:
				return "AsherMylesTalkData";
			
			case NPCIdentifier.LoganStryke:
				return "LoganStrykeTalkData";
			
			case NPCIdentifier.Other:
				Debug.LogWarning("[DialogueTrigger] 选择了'其他NPC'，但未配置对应的对话资源");
				return null;
			
			default:
				return null;
		}
	}
	
	#endregion
	
	#region 公共接口
	
	/// <summary>
	/// 强制打开对话（外部调用）
	/// </summary>
	public void ForceOpenDialogue()
	{
		if (!isDialogueOpen)
		{
			OpenDialogue();
		}
	}
	
	/// <summary>
	/// 强制关闭对话（外部调用）
	/// </summary>
	public void ForceCloseDialogue()
	{
		if (isDialogueOpen)
		{
			CloseDialogue();
		}
	}
	
	/// <summary>
	/// 设置NPC标识（外部调用）
	/// </summary>
	public void SetNPC(NPCIdentifier npc)
	{
		npcId = npc;
		LogDebug($"NPC标识已设置为: {npcId}");
	}
	
	/// <summary>
	/// 获取当前对话是否打开
	/// </summary>
	public bool IsDialogueOpen()
	{
		return isDialogueOpen;
	}
	
	/// <summary>
	/// 获取当前NPC标识
	/// </summary>
	public NPCIdentifier GetCurrentNPC()
	{
		return npcId;
	}
	
	#endregion
	
	#region 调试工具
	
	/// <summary>
	/// 调试日志
	/// </summary>
	private void LogDebug(string message)
	{
		if (showDebugLog)
		{
			Debug.Log($"<color=#9C27B0>[DialogueTrigger]</color> {message}");
		}
	}
	
	/// <summary>
	/// 手动测试打开对话
	/// </summary>
	[ContextMenu("测试打开对话")]
	private void TestOpenDialogue()
	{
		OpenDialogue();
	}
	
	/// <summary>
	/// 手动测试关闭对话
	/// </summary>
	[ContextMenu("测试关闭对话")]
	private void TestCloseDialogue()
	{
		CloseDialogue();
	}
	
	/// <summary>
	/// 显示当前状态
	/// </summary>
	[ContextMenu("显示当前状态")]
	private void ShowCurrentStatus()
	{
		Debug.Log("=== 对话触发器状态 ===");
		Debug.Log($"NPC标识: {npcId}");
		Debug.Log($"对话资源名: {GetDialogueResourceName()}");
		Debug.Log($"玩家在范围内: {playerInRange}");
		Debug.Log($"对话已打开: {isDialogueOpen}");
		Debug.Log($"对话系统控制器: {(dialogueSystemController != null ? dialogueSystemController.gameObject.name : "未设置")}");
		Debug.Log($"  - 激活状态: {(dialogueSystemController != null ? dialogueSystemController.gameObject.activeInHierarchy.ToString() : "N/A")}");
		Debug.Log($"  - 对话打开状态: {(dialogueSystemController != null ? dialogueSystemController.IsDialogueOpen().ToString() : "N/A")}");
		Debug.Log($"对话控制器: {(chatController != null ? "已找到" : "未找到")}");
		Debug.Log($"  - 激活状态: {(chatController != null ? chatController.gameObject.activeInHierarchy.ToString() : "N/A")}");
		Debug.Log($"输入控制器: {(playerInputController != null ? "已找到" : "未找到")}");
		Debug.Log($"提示文本: {(hintText != null ? "已找到" : "未找到")}");
	}
	
	/// <summary>
	/// 强制重新查找所有引用（调试用）
	/// </summary>
	[ContextMenu("重新查找所有引用")]
	private void ForceRefindReferences()
	{
		dialogueSystemController = null;
		chatController = null;
		playerInputController = null;
		
		AutoFindReferences();
		ValidateReferences();
		
		Debug.Log("[DialogueTrigger] 已重新查找所有引用");
		ShowCurrentStatus();
	}
	
	#endregion
	
	#region 编辑器辅助
	
#if UNITY_EDITOR
	/// <summary>
	/// Inspector值改变时的验证
	/// </summary>
	private void OnValidate()
	{
		// 自动查找引用（仅在编辑器中）
		if (!Application.isPlaying)
		{
			AutoFindReferences();
		}
		
		// 确保提示文本不为空
		if (string.IsNullOrEmpty(hintMessage))
		{
			hintMessage = "[F] Talk";
		}
	}
	
	/// <summary>
	/// 在Scene视图中绘制触发范围
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		BoxCollider2D col = GetComponent<BoxCollider2D>();
		if (col == null) return;
		
		// 绘制触发器范围
		Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.3f); // 紫色半透明
		Vector3 center = transform.position + (Vector3)col.offset;
		Vector3 size = col.size;
		size.z = 0.1f; // 2D碰撞器在Z轴很薄
		Gizmos.DrawCube(center, size);
		
		// 绘制边框
		Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.8f); // 紫色
		Gizmos.DrawWireCube(center, size);
		
		// 绘制NPC标识文本（使用UnityEditor.Handles）
		#if UNITY_EDITOR
		UnityEditor.Handles.Label(center + Vector3.up * (size.y * 0.5f + 0.5f), 
			$"对话触发器\nNPC: {npcId}", 
			new GUIStyle() { 
				normal = new GUIStyleState() { textColor = Color.white },
				alignment = TextAnchor.MiddleCenter,
				fontSize = 12
			});
		#endif
	}
#endif
	
	#endregion
}

