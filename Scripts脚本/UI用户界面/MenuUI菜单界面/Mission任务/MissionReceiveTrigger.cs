using UnityEngine;

/// <summary>
/// 进入触发器后按 F（Operate） 打开/关闭 Mission 接受系统界面。
/// - 不切换输入图集，仅订阅 F 事件；Tab（背包）不受影响
/// - 通过配置 Canvas 或 Panel 根对象来控制显隐；
///   若仅提供 MissionManager，则默认激活其 GameObject
/// </summary>
public class MissionReceiveTrigger : BaseContainerTrigger
{
    [Header("Mission 接受界面绑定")]
    [Tooltip("任务系统管理器，可选。若无其他面板引用，将激活此对象")] public MissionManager missionManager;
    [Tooltip("Mission 接受界面的 Canvas，可选")]
    public Canvas missionCanvas;
    [Tooltip("Mission 接受界面的根对象（任意 GameObject），可选")]
    public GameObject missionPanelRoot;

    [Header("UI流程管理")]
    [Tooltip("面板切换管理器（Canvas下的Manager）")]
    public MissionReceiveUIManager uiManager;
    
    [Header("进度条控制")]
    [Tooltip("进度条控制器（可选） - 打开Canvas时自动开始进度")]
    public ProgressBarController progressBarController;

    protected override void OnChildStart()
    {
        // 默认提示文案
        if (string.IsNullOrEmpty(displayText))
        {
            displayText = "[F] Accept Mission";
        }

        // 初始关闭界面（若已绑定具体面板）
        if (missionCanvas != null)
        {
            missionCanvas.enabled = false;
            missionCanvas.gameObject.SetActive(false);
        }
        else if (missionPanelRoot != null)
        {
            missionPanelRoot.SetActive(false);
        }
    }

    protected override string GetContainerTypeName()
    {
        return "任务接受";
    }

    protected override bool IsContainerOpen()
    {
        if (missionCanvas != null) return missionCanvas.enabled && missionCanvas.gameObject.activeInHierarchy;
        if (missionPanelRoot != null) return missionPanelRoot.activeInHierarchy;
        if (missionManager != null) return missionManager.gameObject.activeInHierarchy;
        return false;
    }

    protected override void ToggleContainer()
    {
        if (IsContainerOpen())
        {
            CloseMissionUI();
        }
        else
        {
            OpenMissionUI();
        }
    }

    protected override void ForceCloseContainer()
    {
        CloseMissionUI();
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        base.OnPlayerEnterTrigger(playerCollider);
        // 进入范围时绑定 F（Operate）
        if (playerInputController != null)
        {
            playerInputController.onOperate += HandleOperate;
        }
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);
        // 离开范围时解绑 F（Operate）
        if (playerInputController != null)
        {
            playerInputController.onOperate -= HandleOperate;
            // 兜底：确保离开后两套输入都可用，避免 Tab 无法响应
            playerInputController.EnableAllInput();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 兜底解绑，避免残留
        if (playerInputController != null)
        {
            playerInputController.onOperate -= HandleOperate;
        }
    }

    private void HandleOperate()
    {
        if (!playerInRange) return;
        ToggleContainer();
    }

    private void OpenMissionUI()
    {
        if (missionCanvas != null)
        {
            missionCanvas.gameObject.SetActive(true);
            missionCanvas.enabled = true;
        }
        else if (missionPanelRoot != null)
        {
            missionPanelRoot.SetActive(true);
        }
        else if (missionManager != null)
        {
            missionManager.gameObject.SetActive(true);
        }

        // 自动启动进度条（如果配置了进度条控制器）
        if (progressBarController != null)
        {
            progressBarController.StartProgress();
            if (debugLog) LogInfo("已自动启动进度条");
        }

        // 首选采用 UI 管理器的"Loading→Mission"流程
        if (uiManager != null)
        {
            uiManager.ShowMissionWithLoading();
        }
        else if (missionManager != null)
        {
            // 回退：无管理器则直接显示任务面板并生成任务
            missionManager.GenerateMissionItems();
        }

        if (debugLog) LogInfo("打开 Mission 接受界面");
    }

    private void CloseMissionUI()
    {
        // 停止进度条（如果配置了进度条控制器）
        if (progressBarController != null)
        {
            progressBarController.StopProgress();
            if (debugLog) LogInfo("已停止进度条");
        }

        if (missionCanvas != null)
        {
            missionCanvas.enabled = false;
            missionCanvas.gameObject.SetActive(false);
        }
        else if (missionPanelRoot != null)
        {
            missionPanelRoot.SetActive(false);
        }
        else if (missionManager != null)
        {
            missionManager.gameObject.SetActive(false);
        }

        // 兜底：关闭任务界面后，确保 Tab 所需输入映射可用
        if (playerInputController != null)
        {
            playerInputController.EnableAllInput();
        }

        if (debugLog) LogInfo("关闭 Mission 接受界面");
    }
}


