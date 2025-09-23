using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间
using System; // 引入System命名空间以支持反射功能
using InventorySystem; // 引入以访问 GridType 等类型

/// <summary>
/// 容器触发器基类 - 提供进入特定碰撞触发器时按F键打开容器UI的通用功能
/// 子类需要实现具体的容器打开/关闭逻辑
/// </summary>
public abstract class BaseContainerTrigger : MonoBehaviour
{
    [Header("UI设置")]
    public TextMeshProUGUI tmpText; // 要激活的TMP文本组件
    public string displayText = "[Tab] Open"; // 显示的文本内容

    [Header("输入控制")]
    public PlayerInputController playerInputController; // 玩家输入控制器引用

    [Header("调试设置")]
    [SerializeField] protected bool debugLog = false; // 调试日志开关

    // 受保护的属性，供子类访问
    protected bool playerInRange = false; // 玩家是否在范围内
    protected bool wasContainerOpenWhenEntered = false; // 记录玩家进入时容器是否已打开
    // isOperateEventBound 变量已移除，因为F键功能已停用

    /// <summary>
    /// 抽象方法：检查容器是否已打开
    /// 子类必须实现此方法来返回对应容器的打开状态
    /// </summary>
    /// <returns>容器是否已打开</returns>
    protected abstract bool IsContainerOpen();

    /// <summary>
    /// 抽象方法：切换容器状态
    /// 子类必须实现此方法来处理容器的打开/关闭逻辑
    /// </summary>
    protected abstract void ToggleContainer();

    /// <summary>
    /// 抽象方法：强制关闭容器
    /// 子类必须实现此方法来强制关闭容器
    /// </summary>
    protected abstract void ForceCloseContainer();

    /// <summary>
    /// 虚方法：获取容器类型名称（用于日志输出）
    /// 子类可以重写此方法来提供更具体的容器类型名称
    /// </summary>
    /// <returns>容器类型名称</returns>
    protected virtual string GetContainerTypeName()
    {
        return "容器";
    }

    protected virtual void Start()
    {
        // 确保文本初始状态为隐藏
        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(false);
        }

		// 如果没有手动设置PlayerInputController，尝试自动查找（兼容 ScriptableObject 实例）
		if (playerInputController == null)
		{
			// 先尝试场景中的组件（极少数情况下可能挂在物体上）
			playerInputController = FindObjectOfType<PlayerInputController>();
			if (playerInputController == null)
			{
				// 回退：查找所有已加载的 ScriptableObject 实例
				var all = Resources.FindObjectsOfTypeAll<PlayerInputController>();
				if (all != null && all.Length > 0)
				{
					playerInputController = all[0];
				}
			}

			if (playerInputController == null)
			{
				Debug.LogWarning("未找到 PlayerInputController 实例！请在任一资源中创建并在相关系统中引用。");
			}
		}

        // 调用子类的初始化方法
        OnChildStart();
    }

    /// <summary>
    /// 虚方法：子类可重写此方法来执行额外的初始化逻辑
    /// </summary>
    protected virtual void OnChildStart()
    {
        // 默认为空，子类可以重写
    }

    /// <summary>
    /// 子类可重写：告知进入该触发器后希望右侧网格刷新为哪种类型
    /// 默认 Ground，以保证向后兼容
    /// </summary>
    protected virtual GridType GetGridTypeForRefresh()
    {
        return GridType.Ground;
    }

    /// <summary>
    /// 提供一个在基类中可复用的 BackPackState 引用获取逻辑
    /// 子类可重写以优先使用自身字段
    /// </summary>
    protected virtual BackpackState GetBackpackStateReference()
    {
        // 优先通过单例管理器获取
        var systemManager = BackpackSystemManager.Instance;
        if (systemManager != null)
        {
            var state = systemManager.GetBackpackState();
            if (state != null) return state;
        }

        // 回退：场景中直接查找（包含非激活对象）
        var found = FindObjectOfType<BackpackState>(true);
        return found;
    }

    /// <summary>
    /// 当背包当前处于关闭状态时，强制将右侧网格预刷新为目标类型
    /// 目的：解决跨场景后旧网格残留导致第一次 Tab 显示错误的问题
    /// </summary>
    protected void ForceRefreshGridIfBackpackClosed()
    {
        var backpackState = GetBackpackStateReference();
        if (backpackState == null)
        {
            if (debugLog) Debug.LogWarning("[BaseContainer] 未找到 BackpackState，跳过预刷新");
            return;
        }

        // 背包已打开则不打断当前逻辑
        if (backpackState.IsBackpackOpen())
        {
            if (debugLog) Debug.Log("[BaseContainer] 背包已打开，跳过预刷新");
            return;
        }

        var panelController = FindObjectOfType<BackpackPanelController>();
        if (panelController == null)
        {
            if (debugLog) Debug.LogWarning("[BaseContainer] 未找到 BackpackPanelController，跳过预刷新");
            return;
        }

        var targetType = GetGridTypeForRefresh();
        if (panelController.GetCurrentGridType() != targetType)
        {
            panelController.ActivatePanel(targetType);
            if (debugLog) Debug.Log($"[BaseContainer] 强制预刷新网格 → {targetType}");
        }
        else
        {
            if (debugLog) Debug.Log($"[BaseContainer] 当前网格已是 {targetType}，无需预刷新");
        }
    }

    // 当其他碰撞体进入触发器时调用
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLog) LogInfo($"触发器检测到对象: {other.name}, 标签: {other.tag}, 碰撞体类型: {other.GetType().Name}");

        // 检查是否是玩家的BoxCollider2D进入
        if (other is BoxCollider2D && other.CompareTag("Player"))
        {
            playerInRange = true;
            if (debugLog) LogInfo($"玩家进入{GetContainerTypeName()}触发器范围，可以使用Tab键打开{GetContainerTypeName()}");

            // 记录玩家进入时容器的状态
            wasContainerOpenWhenEntered = IsContainerOpen();

            // 激活TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(true);
                tmpText.text = displayText;
            }

            // F键功能已移除，仅保留Tab键打开功能

            // 调用子类的进入处理方法
            OnPlayerEnterTrigger(other);
        }
    }

    // 当其他碰撞体离开触发器时调用
    private void OnTriggerExit2D(Collider2D other)
    {
        // 检查是否是玩家的BoxCollider2D离开
        if (other is BoxCollider2D && other.CompareTag("Player"))
        {
            playerInRange = false;
            if (debugLog) LogInfo($"玩家离开{GetContainerTypeName()}触发器范围");

            // 隐藏TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(false);
            }

            // F键功能已移除，无需解绑事件

            // 如果玩家进入时容器是关闭的，离开时如果容器是打开的，则关闭它
            if (!wasContainerOpenWhenEntered && IsContainerOpen())
            {
                ForceCloseContainer();
                if (debugLog) LogWarn($"玩家离开{GetContainerTypeName()}范围，自动关闭{GetContainerTypeName()}界面");
            }

            // 调用子类的离开处理方法
            OnPlayerExitTrigger(other);
        }
    }

    /// <summary>
    /// 虚方法：玩家进入触发器时的额外处理
    /// 子类可重写此方法来执行额外的进入逻辑
    /// </summary>
    /// <param name="playerCollider">玩家的碰撞体</param>
    protected virtual void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        // 默认为空，子类可以重写
    }

    /// <summary>
    /// 虚方法：玩家离开触发器时的额外处理
    /// 子类可重写此方法来执行额外的离开逻辑
    /// </summary>
    /// <param name="playerCollider">玩家的碰撞体</param>
    protected virtual void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        // 默认为空，子类可以重写
    }

    // F键功能已移除 - 以下方法已停用
    /*
    // 绑定F键（Operate）事件
    private void BindOperateEvent()
    {
        if (playerInputController != null && !isOperateEventBound)
        {
            playerInputController.onOperate += OnOperatePressed;
            isOperateEventBound = true;
            Debug.Log($"已绑定F键（Operate）事件到{GetContainerTypeName()}触发器");
        }
    }

    // 解绑F键（Operate）事件
    private void UnbindOperateEvent()
    {
        if (playerInputController != null && isOperateEventBound)
        {
            playerInputController.onOperate -= OnOperatePressed;
            isOperateEventBound = false;
            Debug.Log($"已解绑F键（Operate）事件从{GetContainerTypeName()}触发器");
        }
    }

    // F键（Operate）按下时的处理
    private void OnOperatePressed()
    {
        if (playerInRange)
        {
            ToggleContainer();
            Debug.Log($"通过F键（Operate）切换{GetContainerTypeName()}状态");
        }
    }
    */

    /// <summary>
    /// 公共方法：检查玩家是否在范围内（供其他脚本调用）
    /// </summary>
    /// <returns>玩家是否在触发器范围内</returns>
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }

    // 组件销毁时清理事件绑定
    protected virtual void OnDestroy()
    {
        // F键功能已移除，无需清理事件绑定
    }

    // ========================= 调试输出辅助 =========================
    protected string ScriptTag()
    {
        // 蓝色脚本标签
        return $"<color=#4FC3F7>[{GetType().Name}]</color>";
    }

    protected void LogInfo(string message)
    {
        Debug.Log($"{ScriptTag()} <color=#E0E0E0>{message}</color>");
    }

    protected void LogWarn(string message)
    {
        Debug.LogWarning($"{ScriptTag()} <color=#FFC107>{message}</color>");
    }

    protected void LogError(string message)
    {
        Debug.LogError($"{ScriptTag()} <color=#FF5252>{message}</color>");
    }
}
