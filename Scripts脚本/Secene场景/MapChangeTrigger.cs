using UnityEngine;
using TMPro; // 引入TextMeshPro命名空间
using System; // 引入System命名空间以支持反射功能

/// <summary>
/// 地图触发器 - 继承自BaseContainerTrigger基类
/// 实现进入地图区域时按Tab键直接打开背包的地图面板功能
/// 支持TMP文本自动定位到玩家附近位置显示
/// </summary>
public class MapChangeTrigger : BaseContainerTrigger
{
    [Header("背包系统")]
    public BackpackState backpackState; // 背包状态管理器引用

    [Header("地图面板设置")]
    [SerializeField] private int mapPanelIndex = 2; // 地图面板索引2（BackpackPanel=0, MissionPanel=1, MapPanelUI=2）

    [Header("TMP文本显示设置")]
    [SerializeField] private Vector3 textOffset = new Vector3(0, 50, 0); // 文本相对于玩家的偏移量（屏幕坐标）
    [SerializeField] private Camera playerCamera; // 玩家相机引用
    [SerializeField] private Transform playerTransform; // 玩家Transform引用

    [Header("调试设置")]
    [SerializeField] private bool debugMode = false; // 调试模式开关

    /// <summary>
    /// 重写基类的OnChildStart方法，执行地图面板特定的初始化逻辑
    /// </summary>
    protected override void OnChildStart()
    {
        // 如果没有手动设置BackpackState，尝试自动查找
        if (backpackState == null)
        {
            backpackState = FindObjectOfType<BackpackState>();
            if (backpackState == null)
            {
                Debug.LogWarning("MapChangeTrigger: 未找到BackpackState组件！请确保场景中存在BackpackState脚本。");
            }
        }

        // 自动查找玩家Transform
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (debugMode)
                {
                    Debug.Log("MapChangeTrigger: 自动找到玩家Transform");
                }
            }
            else
            {
                Debug.LogWarning("MapChangeTrigger: 未找到标签为'Player'的游戏对象！");
            }
        }

        // 自动查找玩家相机
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
            if (playerCamera != null && debugMode)
            {
                Debug.Log("MapChangeTrigger: 自动找到玩家相机");
            }
            else if (playerCamera == null)
            {
                Debug.LogWarning("MapChangeTrigger: 未找到相机组件！");
            }
        }

        // 初始化TMP文本
        InitializeTMPText();
        
        // 设置地图触发器的提示文本为F键
        displayText = "[F] Open Map";

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 初始化完成，地图面板索引设置为 {mapPanelIndex}，使用TopNavigationTransform进行面板管理");
        }
    }

    /// <summary>
    /// 实现基类抽象方法：检查背包是否已打开且当前显示的是地图面板
    /// </summary>
    /// <returns>背包是否已打开且显示地图面板</returns>
    protected override bool IsContainerOpen()
    {
        if (backpackState == null)
        {
            return false;
        }

        bool isBackpackOpen = backpackState.IsBackpackOpen();

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 背包打开状态={isBackpackOpen}");
        }

        // 由于TopNavigationTransform没有直接的获取当前面板索引的方法
        // 我们简化逻辑，只检查背包是否打开
        return isBackpackOpen;
    }

    /// <summary>
    /// 实现基类抽象方法：切换地图面板状态
    /// 如果背包未打开，则直接打开到地图面板
    /// 如果背包已打开，则关闭背包
    /// </summary>
    protected override void ToggleContainer()
    {
        if (backpackState == null || backpackState.topNavigationTransform == null)
        {
            Debug.LogError("MapChangeTrigger: BackpackState或TopNavigationTransform组件为空，无法切换地图面板！");
            return;
        }

        bool isBackpackOpen = backpackState.IsBackpackOpen();

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 切换前状态 - 背包打开={isBackpackOpen}");
        }

        if (!isBackpackOpen)
        {
            // 背包未打开，直接打开到地图面板
            backpackState.topNavigationTransform.OpenToSpecificPanel(mapPanelIndex);

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: 直接打开到地图面板");
            }
        }
        else
        {
            // 背包已打开，关闭背包
            backpackState.ForceCloseBackpack();

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: 关闭背包");
            }
        }
    }

    /// <summary>
    /// 实现基类抽象方法：强制关闭背包
    /// </summary>
    protected override void ForceCloseContainer()
    {
        if (backpackState != null)
        {
            backpackState.ForceCloseBackpack();

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: 强制关闭背包");
            }
        }
    }

    /// <summary>
    /// 重写基类虚方法：返回容器类型名称
    /// </summary>
    /// <returns>容器类型名称</returns>
    protected override string GetContainerTypeName()
    {
        return "地图";
    }



    /// <summary>
    /// 公共方法：设置地图面板索引（供外部调用）
    /// </summary>
    /// <param name="index">地图面板索引</param>
    public void SetMapPanelIndex(int index)
    {
        mapPanelIndex = index;

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 地图面板索引已设置为 {mapPanelIndex}");
        }
    }

    /// <summary>
    /// 公共方法：获取当前地图面板索引
    /// </summary>
    /// <returns>地图面板索引</returns>
    public int GetMapPanelIndex()
    {
        return mapPanelIndex;
    }

    /// <summary>
    /// 公共方法：启用/禁用调试模式
    /// </summary>
    /// <param name="enabled">是否启用调试模式</param>
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        Debug.Log($"MapChangeTrigger: 调试模式已{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 调试方法：测试所有面板索引（仅在Unity编辑器中可用）
    /// 右键点击此组件选择"Test All Panel Indices"来测试
    /// </summary>
    [ContextMenu("测试所有面板索引")]
    private void TestAllPanelIndices()
    {
        if (Application.isPlaying && backpackState != null && backpackState.topNavigationTransform != null)
        {
            StartCoroutine(TestPanelIndicesCoroutine());
        }
        else
        {
            Debug.LogWarning("MapChangeTrigger: 请在运行时使用此功能，并确保BackpackState和TopNavigationTransform组件已设置。");
        }
    }

    /// <summary>
    /// 测试面板索引的协程
    /// </summary>
    /// <returns>协程迭代器</returns>
    private System.Collections.IEnumerator TestPanelIndicesCoroutine()
    {
        Debug.Log("MapChangeTrigger: 开始测试所有面板索引...");

        // 确保背包是打开的
        if (!backpackState.IsBackpackOpen())
        {
            backpackState.ForceOpenBackpack();
            yield return null; // 等待一帧
        }

        // 测试索引0到2
        for (int i = 0; i <= 2; i++)
        {
            Debug.Log($"MapChangeTrigger: 测试面板索引 {i}");
            backpackState.topNavigationTransform.SwitchToPanel(i);
            yield return new WaitForSeconds(2f); // 等待2秒观察效果
        }

        Debug.Log("MapChangeTrigger: 面板索引测试完成。请根据观察结果设置正确的mapPanelIndex值。");
    }

    /// <summary>
    /// 初始化TMP文本组件
    /// </summary>
    private void InitializeTMPText()
    {
        if (tmpText != null)
        {
            // 设置文本内容为地图相关的提示（F键）
            displayText = "[F] Open Map";
            tmpText.text = displayText;
            // 默认隐藏文本
            tmpText.gameObject.SetActive(false);

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: TMP文本初始化完成");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("MapChangeTrigger: TMP文本组件未设置！");
        }
    }

    /// <summary>
    /// 更新TMP文本位置，使其跟随玩家位置
    /// </summary>
    private void UpdateTMPTextPosition()
    {
        if (tmpText == null || playerTransform == null || playerCamera == null)
        {
            return;
        }

        // 将玩家世界坐标转换为屏幕坐标
        Vector3 playerScreenPosition = playerCamera.WorldToScreenPoint(playerTransform.position);

        // 添加偏移量
        Vector3 textScreenPosition = playerScreenPosition + textOffset;

        // 获取Canvas组件来正确设置UI位置
        Canvas canvas = tmpText.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 对于Screen Space - Overlay模式，直接使用屏幕坐标
            RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.position = textScreenPosition;
            }
        }
        else
        {
            // 对于其他模式，将屏幕坐标转换回世界坐标
            Vector3 worldPosition = playerCamera.ScreenToWorldPoint(new Vector3(textScreenPosition.x, textScreenPosition.y, playerCamera.nearClipPlane + 1f));
            tmpText.transform.position = worldPosition;
        }

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: TMP文本位置更新 - 玩家屏幕坐标: {playerScreenPosition}, 文本屏幕坐标: {textScreenPosition}");
        }
    }

    /// <summary>
    /// 重写基类的OnPlayerEnterTrigger方法，添加TMP文本显示逻辑
    /// </summary>
    /// <param name="playerCollider">玩家的碰撞体</param>
    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        base.OnPlayerEnterTrigger(playerCollider);

        // 显示TMP文本
        ShowTMPText();
    }

    /// <summary>
    /// 重写基类的OnPlayerExitTrigger方法，添加TMP文本隐藏逻辑
    /// </summary>
    /// <param name="playerCollider">玩家的碰撞体</param>
    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);

        // 隐藏TMP文本
        HideTMPText();
    }

    /// <summary>
    /// 显示TMP文本
    /// </summary>
    private void ShowTMPText()
    {
        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(true);
            UpdateTMPTextPosition();

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: TMP文本已显示");
            }
        }
    }

    /// <summary>
    /// 隐藏TMP文本
    /// </summary>
    private void HideTMPText()
    {
        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(false);

            if (debugMode)
            {
                Debug.Log("MapChangeTrigger: TMP文本已隐藏");
            }
        }
    }

    /// <summary>
    /// Update方法，处理F键输入和更新TMP文本位置
    /// </summary>
    private void Update()
    {
        // 检查是否按下了F键
        if (Input.GetKeyDown(KeyCode.F))
        {
            // 如果玩家在范围内，或者地图面板已经打开，都可以使用F键
            if (playerInRange || IsContainerOpen())
            {
                ToggleContainer();
                if (debugMode)
                {
                    Debug.Log("MapChangeTrigger: 通过F键切换地图面板");
                }
            }
        }
        
        // 如果TMP文本处于激活状态且玩家在触发器范围内，持续更新文本位置
        if (tmpText != null && tmpText.gameObject.activeInHierarchy && playerInRange)
        {
            UpdateTMPTextPosition();
        }
    }

    /// <summary>
    /// 公共方法：设置TMP文本内容
    /// </summary>
    /// <param name="text">要显示的文本内容</param>
    public void SetDisplayText(string text)
    {
        displayText = text;
        if (tmpText != null)
        {
            tmpText.text = displayText;
        }

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 显示文本已设置为: {displayText}");
        }
    }

    /// <summary>
    /// 公共方法：设置TMP文本偏移量
    /// </summary>
    /// <param name="offset">文本相对于玩家的偏移量（屏幕坐标）</param>
    public void SetTextOffset(Vector3 offset)
    {
        textOffset = offset;

        if (debugMode)
        {
            Debug.Log($"MapChangeTrigger: 文本偏移量已设置为: {textOffset}");
        }
    }
}
