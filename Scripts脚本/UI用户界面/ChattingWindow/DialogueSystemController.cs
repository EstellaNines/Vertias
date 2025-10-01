using UnityEngine;

/// <summary>
/// 对话系统状态控制器
/// 负责管理整个对话系统的显示/隐藏状态
/// 确保运行开始时对话系统处于隐藏状态
/// 与仓库触发器、货架触发器保持一致的架构
/// </summary>
public class DialogueSystemController : MonoBehaviour
{
	[Header("Canvas引用")]
	[FieldLabel("对话系统Canvas")]
	[Tooltip("对话系统的Canvas组件（NPCSystemCanvas）")]
	[SerializeField] private Canvas dialogueCanvas;
	
	[Header("状态变量")]
	private bool isDialogueOpen = false; // 对话系统是否打开

	[Header("启动选项")]
	[FieldLabel("跳过初始隐藏")]
	[SerializeField] private bool suppressInitialHide = false;
	[FieldLabel("启动即打开")]
	[SerializeField] private bool openOnStart = false;
	
	[Header("调试设置")]
	[FieldLabel("显示调试日志")]
	[SerializeField] private bool showDebugLog = true;
	
	#region Unity生命周期
	
	private void Awake()
	{
		// 自动查找Canvas引用
		if (dialogueCanvas == null)
		{
			dialogueCanvas = GetComponentInChildren<Canvas>(true);
			if (dialogueCanvas != null)
			{
				LogDebug($"自动找到对话Canvas: {dialogueCanvas.gameObject.name}");
			}
		}
	}
	
	private void Start()
	{
		// 初始化时确保对话系统是关闭状态
		InitializeDialogueSystem();
	}
	
	#endregion
	
	#region 初始化
	
	/// <summary>
	/// 初始化对话系统（确保初始为隐藏状态）
	/// </summary>
	private void InitializeDialogueSystem()
	{
		if (dialogueCanvas == null)
		{
			Debug.LogError("[DialogueSystemController] 对话Canvas未设置！请在Inspector中设置或确保对话系统包含Canvas组件", this);
			return;
		}
		
		// 外部（如触发器）请求在启动时保持打开或跳过隐藏
		if (suppressInitialHide)
		{
			if (openOnStart)
			{
				OpenDialogue();
			}
			LogDebug("对话系统初始化完成（按外部请求跳过初始隐藏）");
			return;
		}
		
		// 默认：初始化为关闭状态
		CloseDialogue();
		
		LogDebug("对话系统初始化完成（已隐藏）");
	}
	
	#endregion
	
	#region 对话系统控制
	
	/// <summary>
	/// 打开对话系统
	/// </summary>
	public void OpenDialogue()
	{
		if (isDialogueOpen)
		{
			LogDebug("对话系统已经打开，跳过重复操作");
			return;
		}
		
		if (dialogueCanvas == null)
		{
			Debug.LogWarning("[DialogueSystemController] 无法打开对话：Canvas未设置");
			return;
		}
		
		// 激活Canvas
		dialogueCanvas.gameObject.SetActive(true);
		dialogueCanvas.enabled = true;
		
		isDialogueOpen = true;
		
		LogDebug("对话系统已打开");
	}
	
	/// <summary>
	/// 关闭对话系统
	/// </summary>
	public void CloseDialogue()
	{
		if (dialogueCanvas == null)
		{
			Debug.LogWarning("[DialogueSystemController] 无法关闭对话：Canvas未设置");
			return;
		}
		
		// 禁用Canvas
		dialogueCanvas.enabled = false;
		dialogueCanvas.gameObject.SetActive(false);
		
		isDialogueOpen = false;
		
		LogDebug("对话系统已关闭");
	}
	
	/// <summary>
	/// 切换对话系统状态
	/// </summary>
	public void ToggleDialogue()
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
	/// 强制打开对话系统（外部调用）
	/// </summary>
	public void ForceOpenDialogue()
	{
		if (!isDialogueOpen)
		{
			OpenDialogue();
		}
	}

	/// <summary>
	/// 由外部调用，在实例化后但Start之前设置：启动时保持打开
	/// </summary>
	public void PrepareInitialOpen()
	{
		suppressInitialHide = true;
		openOnStart = true;
	}

	/// <summary>
	/// 可选：清除启动控制标志
	/// </summary>
	public void ClearInitialFlags()
	{
		suppressInitialHide = false;
		openOnStart = false;
	}
	
	/// <summary>
	/// 强制关闭对话系统（外部调用）
	/// </summary>
	public void ForceCloseDialogue()
	{
		if (isDialogueOpen)
		{
			CloseDialogue();
		}
	}
	
	/// <summary>
	/// 获取对话系统是否打开
	/// </summary>
	public bool IsDialogueOpen()
	{
		return isDialogueOpen;
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
			Debug.Log($"<color=#673AB7>[DialogueSystemController]</color> {message}");
		}
	}
	
	/// <summary>
	/// 显示当前状态
	/// </summary>
	[ContextMenu("显示当前状态")]
	private void ShowCurrentStatus()
	{
		Debug.Log("=== 对话系统控制器状态 ===");
		Debug.Log($"对话已打开: {isDialogueOpen}");
		Debug.Log($"对话Canvas: {(dialogueCanvas != null ? dialogueCanvas.gameObject.name : "未设置")}");
		Debug.Log($"  - GameObject激活: {(dialogueCanvas != null ? dialogueCanvas.gameObject.activeInHierarchy.ToString() : "N/A")}");
		Debug.Log($"  - Canvas启用: {(dialogueCanvas != null ? dialogueCanvas.enabled.ToString() : "N/A")}");
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
	
	#endregion
}

