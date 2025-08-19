using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间
using System; // 引入System命名空间以支持反射功能

/// <summary>
/// 容器触发器基类 - 提供进入特定碰撞触发器时按F键打开容器UI的通用功能
/// 子类需要实现具体的容器打开/关闭逻辑
/// </summary>
public abstract class BaseContainerTrigger : MonoBehaviour
{
    [Header("UI设置")]
    public TextMeshProUGUI tmpText; // 要激活的TMP文本组件
    public string displayText = "[Tab/F] Open"; // 显示的文本内容

    [Header("输入控制")]
    public PlayerInputController playerInputController; // 玩家输入控制器引用

    // 受保护的属性，供子类访问
    protected bool playerInRange = false; // 玩家是否在范围内
    protected bool wasContainerOpenWhenEntered = false; // 记录玩家进入时容器是否已打开
    protected bool isOperateEventBound = false; // 记录是否已绑定Operate事件

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

        // 如果没有手动设置PlayerInputController，尝试自动查找
        if (playerInputController == null)
        {
            playerInputController = FindObjectOfType<PlayerInputController>();
            if (playerInputController == null)
            {
                Debug.LogWarning("未找到PlayerInputController组件！请确保场景中存在PlayerInputController脚本。");
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

    // 当其他碰撞体进入触发器时调用
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"触发器检测到对象: {other.name}, 标签: {other.tag}, 碰撞体类型: {other.GetType().Name}");

        // 检查是否是玩家的BoxCollider2D进入
        if (other is BoxCollider2D && other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log($"玩家进入{GetContainerTypeName()}触发器范围，可以使用Tab或F键打开{GetContainerTypeName()}");

            // 记录玩家进入时容器的状态
            wasContainerOpenWhenEntered = IsContainerOpen();

            // 激活TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(true);
                tmpText.text = displayText;
            }

            // 绑定F键（Operate）事件
            BindOperateEvent();

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
            Debug.Log($"玩家离开{GetContainerTypeName()}触发器范围");

            // 隐藏TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(false);
            }

            // 解绑F键（Operate）事件
            UnbindOperateEvent();

            // 如果玩家进入时容器是关闭的，离开时如果容器是打开的，则关闭它
            if (!wasContainerOpenWhenEntered && IsContainerOpen())
            {
                ForceCloseContainer();
                Debug.Log($"玩家离开{GetContainerTypeName()}范围，自动关闭{GetContainerTypeName()}界面");
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
        UnbindOperateEvent();
    }
}
