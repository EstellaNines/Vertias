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

    [SerializeField] private GameObject warehouseGridPrefab;
    [SerializeField] private GameObject groundGridPrefab;
    [SerializeField] private RectTransform rightPanelTransform;

    private GameObject currentGrid;
    private GridSaveManager gridSaveManager; // 网格保存管理器

    private void Start()
    {
        // 确保保存管理器存在
        EnsureSaveManagerExists();
        
        // 创建网格保存管理器
        InitializeGridSaveManager();
        
        InitializeBackpack();
    }

    /// <summary>
    /// 初始化网格保存管理器
    /// </summary>
    private void InitializeGridSaveManager()
    {
        if (gridSaveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GridSaveManager");
            saveManagerObj.transform.SetParent(this.transform);
            gridSaveManager = saveManagerObj.AddComponent<GridSaveManager>();
            Debug.Log("BackpackState: 已创建GridSaveManager实例");
        }
    }

    /// <summary>
    /// 确保保存管理器存在
    /// </summary>
    private void EnsureSaveManagerExists()
    {
        if (InventorySaveManager.Instance == null)
        {
            GameObject saveManager = new GameObject("InventorySaveManager");
            saveManager.AddComponent<InventorySaveManager>();
            Debug.Log("BackpackState: 已创建InventorySaveManager实例");
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

        // 清理旧网格
        CleanupCurrentGrid();

        // 创建新网格
        CreateAndSetupGrid();
        
        Debug.Log("Backpack opened. isInWarehouse: " + WarehouseTrigger.isInWarehouse + ", Instantiated grid: " + currentGrid.name);
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

        // 清理当前网格
        CleanupCurrentGrid();

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

    /// <summary>
    /// 创建并设置网格
    /// </summary>
    private void CreateAndSetupGrid()
    {
        // 根据是否在仓库内选择不同的预制件
        if (WarehouseTrigger.isInWarehouse)
        {
            currentGrid = Instantiate(warehouseGridPrefab, rightPanelTransform);
        }
        else
        {
            currentGrid = Instantiate(groundGridPrefab, rightPanelTransform);
        }

        // 根据网格类型设置不同的位置和尺寸
        RectTransform gridRT = currentGrid.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0, 0);
        gridRT.anchorMax = new Vector2(0, 1);
        
        if (WarehouseTrigger.isInWarehouse)
        {
            // 仓库网格位置和尺寸
            gridRT.anchoredPosition = new Vector2(15, -52);
            gridRT.sizeDelta = new Vector2(640, 896);
        }
        else
        {
            // 地面网格位置和尺寸
            gridRT.anchoredPosition = new Vector2(15, -42);
            gridRT.sizeDelta = new Vector2(640, 512);
        }

        // 确保网格被激活显示
        currentGrid.SetActive(true);
        
        // 设置保存管理器并注册网格
        SetupGridSaveLoad();
    }

    /// <summary>
    /// 设置网格的保存和加载功能
    /// </summary>
    private void SetupGridSaveLoad()
    {
        if (currentGrid == null || gridSaveManager == null) return;

        // 获取ItemGrid组件
        ItemGrid itemGrid = currentGrid.GetComponent<ItemGrid>();
        if (itemGrid == null)
        {
            Debug.LogError("BackpackState: 当前网格缺少ItemGrid组件！");
            return;
        }

        // 设置网格到保存管理器
        string gridGUID = WarehouseTrigger.isInWarehouse ? "warehouse_grid_main" : "ground_grid_main";
        gridSaveManager.SetCurrentGrid(itemGrid, gridGUID);

        // 注册并加载网格数据
        gridSaveManager.RegisterAndLoadGrid(WarehouseTrigger.isInWarehouse);
    }



    /// <summary>
    /// 清理当前网格
    /// </summary>
    private void CleanupCurrentGrid()
    {
        // 使用GridSaveManager清理并保存
        if (gridSaveManager != null)
        {
            gridSaveManager.CleanupAndSave(true); // 强制保存
        }

        // 销毁游戏对象
        if (currentGrid != null)
        {
            Destroy(currentGrid);
            currentGrid = null;
        }
    }
}

