using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackState : MonoBehaviour
{
    [Header("背包UI组件")]
    [SerializeField] private Canvas backpackCanvas; // 背包Canvas组件
    [SerializeField] private PlayerInputController playerInputController; // 玩家输入控制器组件
    // ButtonOpenPlatform组件已移除，统一使用TopNavigationTransform进行面板管理
    [SerializeField] private TopNavigationTransform topNav; // TopNavigationTransform组件引用
    
    // 公共属性，供外部脚本访问TopNavigationTransform
    public TopNavigationTransform topNavigationTransform => topNav;

    [Header("状态变量")]
    private bool isBackpackOpen = false; // 背包是否打开
    private bool isInitialized = false; // 是否已初始化

    [Header("面板控制器")]
    [SerializeField] private BackpackPanelController backpackPanelController; // 背包面板控制器

    private void Start()
    {
        InitializeBackpack();
    }

    /// <summary>
    /// 验证面板控制器
    /// </summary>
    private void ValidatePanelController()
    {
        if (backpackPanelController == null)
        {
            Debug.LogWarning("BackpackState: backpackPanelController字段为空，尝试自动查找...");
            
            // 尝试自动查找BackpackPanelController
            backpackPanelController = FindObjectOfType<BackpackPanelController>();
            
            if (backpackPanelController == null)
            {
                Debug.LogError("BackpackState: 未找到BackpackPanelController，请在Inspector中设置或确保场景中存在该组件！");
                Debug.LogError("BackpackState: 请将BackpackPanel上的BackpackPanelController组件拖拽到BackpackState的'Backpack Panel Controller'字段中");
            }
            else
            {
                Debug.Log($"BackpackState: 自动找到BackpackPanelController - {backpackPanelController.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"BackpackState: BackpackPanelController已正确设置 - {backpackPanelController.gameObject.name}");
        }
    }

    // 设置玩家输入控制器，这个方法可以被外部调用来动态设置输入控制器
    public void SetPlayerInputController(PlayerInputController controller)
    {
        // 先解除旧的事件绑定
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
            Debug.Log("BackpackState: 已解除旧的事件绑定");
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
            Debug.Log("BackpackState: 已经初始化过了，跳过重复初始化");
            return;
        }

        // 验证面板控制器
        ValidatePanelController();

        // 初始化时确保背包是关闭状态
        if (backpackCanvas != null)
        {
            backpackCanvas.gameObject.SetActive(false);
            isBackpackOpen = false;
        }
        else
        {
            Debug.LogError("BackpackState: 背包Canvas未设置！请在Inspector中设置Canvas组件");
            return;
        }

        // 绑定玩家输入事件（如果输入控制器存在）
        if (playerInputController != null)
        {
            // 先解除可能存在的重复绑定
            playerInputController.onBackPack -= ToggleBackpack; // 先解除
            playerInputController.onBackPack += ToggleBackpack;  // 再绑定
            
            playerInputController.EnabledUIInput(); // 修复方法名

            isInitialized = true;
            Debug.Log("BackpackState: 背包系统初始化完成，已绑定Tab键事件");
        }
        else
        {
            Debug.LogWarning("BackpackState: PlayerInputController未设置，无法绑定输入事件");
        }

        // 设置TopNavigationTransform的BackpackState引用
        if (topNav != null)
        {
            topNav.SetBackpackState(this);
        }
    }

    // 切换背包开关状态（Tab键）
    private void ToggleBackpack()
    {
        if (topNav != null)
        {
            topNav.ToggleBackpack(); // 委托给TopNavigationTransform处理
        }
    }



    // 打开背包
    public void OpenBackpack()
    {
        if (isBackpackOpen) return;
        
        isBackpackOpen = true;
        
        // 打开背包UI时，禁用游戏玩法输入，启用UI输入
        if (playerInputController != null)
        {
            playerInputController.DisableGameplayInput();
            playerInputController.EnabledUIInput(); // 修复方法名
            Debug.Log("BackpackState: 背包打开，GamePlay输入已禁用");
        }

        // 显示鼠标光标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 显示默认面板
        ShowDefaultPanel();

        // 激活面板控制器
        if (backpackPanelController != null)
        {
            // 检查是否在仓库范围内
            bool isInWarehouse = WarehouseTrigger.isInWarehouse;
            // 检查是否在货架范围内
            bool isInShelf = ShelfTrigger.isInShelf;
            
            if (isInShelf)
            {
                // 在货架范围内，激活Container网格
                backpackPanelController.ActivatePanel(GridType.Container);
                Debug.Log($"BackpackState: 背包打开，货架模式: {isInShelf}");
            }
            else if (isInWarehouse)
            {
                // 在仓库范围内，激活Storage网格
                backpackPanelController.ActivatePanel(GridType.Storage);
                Debug.Log($"BackpackState: 背包打开，仓库模式: {isInWarehouse}");
            }
            else
            {
                // 其他情况，激活Ground网格
                backpackPanelController.ActivatePanel(GridType.Ground);
                Debug.Log("BackpackState: 背包打开，地面模式");
            }
        }
        else
        {
            Debug.LogError("BackpackState: BackpackPanelController未设置，无法激活面板！");
        }
    }

    // 关闭背包
    public void CloseBackpack()
    {
        if (!isBackpackOpen) return;
        
        isBackpackOpen = false;
        
        // 关闭背包时，恢复游戏玩法输入，保持UI输入启用以支持其他UI和Tab键
        if (playerInputController != null)
        {
            playerInputController.EnabledGameplayInput();
            // 保持UI输入启用以支持其他UI和Tab键
            Debug.Log("BackpackState: 背包关闭，GamePlay输入已恢复");
        }

        // 根据游戏需要决定是否隐藏光标，这里保持显示状态
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 关闭面板控制器
        if (backpackPanelController != null)
        {
            backpackPanelController.DeactivatePanel();
            Debug.Log("BackpackState: 背包关闭，面板已停用");
        }

        // 隐藏所有面板
        HideAllPanels();
    }

    // 显示默认面板
    private void ShowDefaultPanel()
    {
        if (topNav != null)
        {
            // 默认显示第一个面板，通常是背包主界面
            topNav.SwitchToPanel(0);
        }
    }

    // 隐藏所有面板
    private void HideAllPanels()
    {
        // TopNavigationTransform会在背包关闭时自动处理面板隐藏
        // 这里不需要额外操作
    }

    // 强制关闭背包（外部调用）
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

    // 强制打开背包（外部调用）
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

    // 获取背包是否打开的状态
    public bool IsBackpackOpen()
    {
        return isBackpackOpen;
    }

    private void OnDestroy()
    {
        // 组件销毁时解除事件绑定，防止内存泄漏
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
        }
    }

    // 重新初始化方法（外部调用）
    public void ReInitialize()
    {
        isInitialized = false;
        InitializeBackpack();
    }


}

