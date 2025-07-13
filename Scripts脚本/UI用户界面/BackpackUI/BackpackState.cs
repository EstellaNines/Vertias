using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackState : MonoBehaviour
{
    [Header("背包UI组件")]
    [SerializeField] private Canvas backpackCanvas; // 背包Canvas组件
    [SerializeField] private PlayerInputController playerInputController; // 玩家输入控制器组件
    [SerializeField] private Button closeButton; // 关闭背包UI的按钮
    [SerializeField] private ButtonOpenPlatform buttonOpenPlatform; // 按钮平台管理器

    [Header("状态变量")]
    private bool isBackpackOpen = false; // 背包是否打开
    private bool isInitialized = false; // 是否已初始化

    private void Start()
    {
        InitializeBackpack();
    }

    // 公共方法：设置玩家输入控制器（用于跨场景）
    public void SetPlayerInputController(PlayerInputController controller)
    {
        // 清理旧的事件监听
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
            Debug.Log("BackpackState: 已清理旧的事件监听");
        }

        playerInputController = controller;

        // 重新初始化
        isInitialized = false; // 重置初始化标志
        InitializeBackpack();
    }

    // 初始化背包系统
    private void InitializeBackpack()
    {
        if (isInitialized) 
        {
            Debug.Log("BackpackState: 已经初始化，跳过重复初始化");
            return;
        }

        // 初始化时关闭背包界面
        if (backpackCanvas != null)
        {
            backpackCanvas.gameObject.SetActive(false);
            isBackpackOpen = false;
        }
        else
        {
            Debug.LogError("BackpackState: 背包Canvas未设置！请在Inspector中拖拽Canvas组件");
            return;
        }

        // 为关闭按钮添加点击事件监听器
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // 清理旧监听器
            closeButton.onClick.AddListener(CloseBackpackByButton);
        }
        else
        {
            Debug.LogWarning("BackpackState: 关闭按钮未设置！请在Inspector中拖拽Button组件以便通过UI按钮关闭背包");
        }

        // 设置玩家输入控制器事件监听
        if (playerInputController != null)
        {
            // 确保不会重复添加事件监听
            playerInputController.onBackPack -= ToggleBackpack; // 先移除
            playerInputController.onBackPack += ToggleBackpack;  // 再添加
            playerInputController.EnabledUIInput();

            isInitialized = true;
            Debug.Log("BackpackState: 背包系统初始化完成");
        }
        else
        {
            Debug.LogWarning("BackpackState: PlayerInputController未设置，等待跨场景管理器设置");
        }
    }

    // 切换背包开关状态
    private void ToggleBackpack()
    {
        if (backpackCanvas == null)
        {
            Debug.LogWarning("BackpackState: 背包Canvas未设置！");
            return;
        }

        isBackpackOpen = !isBackpackOpen;
        backpackCanvas.gameObject.SetActive(isBackpackOpen);

        // 根据背包开关状态执行相应操作
        if (isBackpackOpen)
        {
            OpenBackpack();
        }
        else
        {
            CloseBackpack();
        }
    }

    // 通过UI按钮关闭背包
    private void CloseBackpackByButton()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            if (backpackCanvas != null)
            {
                backpackCanvas.gameObject.SetActive(false);
            }
            CloseBackpack();
        }
    }

    // 打开背包
    private void OpenBackpack()
    {
        // 打开背包UI时禁用游戏输入，启用UI输入
        if (playerInputController != null)
        {
            playerInputController.DisableGameplayInput();
            playerInputController.EnabledUIInput();
        }

        // 显示鼠标光标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 显示默认面板
        ShowDefaultPanel();
    }

    // 关闭背包
    private void CloseBackpack()
    {
        // 关闭背包时恢复游戏输入，保持UI输入启用以便响应Tab键
        if (playerInputController != null)
        {
            playerInputController.EnabledGameplayInput();
            // 保持UI输入启用以便响应Tab键
        }

        // 保持鼠标光标可见，但解除锁定状态
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 隐藏所有面板
        HideAllPanels();
    }

    // 显示默认面板
    private void ShowDefaultPanel()
    {
        if (buttonOpenPlatform != null)
        {
            // 默认选中第0个按钮，显示对应的第0个RawImage
            buttonOpenPlatform.SelectButton(0);
        }
    }

    // 隐藏所有面板
    private void HideAllPanels()
    {
        if (buttonOpenPlatform != null)
        {
            buttonOpenPlatform.ClearSelection();
        }
    }

    // 公共方法：强制关闭背包
    public void ForceCloseBackpack()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            if (backpackCanvas != null)
            {
                backpackCanvas.gameObject.SetActive(false);
            }
            CloseBackpack();
        }
    }

    // 公共方法：强制打开背包
    public void ForceOpenBackpack()
    {
        if (!isBackpackOpen)
        {
            isBackpackOpen = true;
            if (backpackCanvas != null)
            {
                backpackCanvas.gameObject.SetActive(true);
            }
            OpenBackpack();
        }
    }

    // 公共方法：获取背包开关状态
    public bool IsBackpackOpen()
    {
        return isBackpackOpen;
    }

    private void OnDestroy()
    {
        // 清理事件监听器以防止内存泄漏
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
        }

        // 移除按钮点击事件监听器
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseBackpackByButton);
        }
    }

    // 公共方法：重新初始化（用于跨场景）
    public void ReInitialize()
    {
        isInitialized = false;
        InitializeBackpack();
    }
}
