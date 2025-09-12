using System.Collections;
using UnityEngine;

/// <summary>
/// 管理 Mission 接受界面的面板切换：先显示 Loading 若干秒，再切换到 Mission 面板。
/// 将本脚本挂在 Canvas 下的 Manager GameObject 上。
/// </summary>
public class MissionReceiveUIManager : MonoBehaviour
{
    [Header("根Canvas（可选）")]
    [Tooltip("Mission 接受界面的根 Canvas，若提供会在流程开始时确保激活")] public Canvas rootCanvas;

    [Header("面板绑定")]
    [Tooltip("加载中面板（流程开始时显示）")] public GameObject loadingPanel;
    [Tooltip("任务面板（加载结束后显示）")] public GameObject missionPanel;

    [Header("动效组件（可选）")]
    [Tooltip("打字机效果组件（可选）")] public TypingTextTMP typingText;
    [Tooltip("CMD 扫描/代码流效果组件（可选）")] public AutoCMDScanEffect cmdScanEffect;
    [Tooltip("进度条控制器（可选）")] public ProgressBarController progressBarController;

    [Header("行为设置")]
    [Tooltip("Loading 显示时长（秒）")] public float loadingSeconds = 5f;
    [Tooltip("Manager 激活时是否自动开始流程（无需外部调用）")] public bool autoStartOnEnable = true;

    [Header("CMD动效设置（可选）")]
    [Tooltip("CMD动效高速模式（更快显示）")] public bool cmdFastMode = true;
    [Tooltip("CMD动效每行基础间隔（秒）")] public float cmdLineSpeed = 0.02f;

    private Coroutine flowCoroutine;

    /// <summary>
    /// 对外入口：显示Loading并在计时后切换到Mission面板。
    /// 可重复调用，后一次会覆盖前一次流程。
    /// </summary>
    public void ShowMissionWithLoading()
    {
        EnsureRootActive();
        if (flowCoroutine != null)
        {
            StopCoroutine(flowCoroutine);
        }
        flowCoroutine = StartCoroutine(ShowFlowCoroutine());
    }

    private void OnEnable()
    {
        if (autoStartOnEnable)
        {
            ShowMissionWithLoading();
        }
    }

    /// <summary>
    /// 直接显示Mission面板（跳过Loading）。
    /// </summary>
    public void ShowMissionDirect()
    {
        EnsureRootActive();
        SetPanelState(loading: false, mission: true);
    }

    /// <summary>
    /// 隐藏所有面板。
    /// </summary>
    public void HideAll()
    {
        // 停止进行中的动效
        if (progressBarController != null && progressBarController.IsLoading())
        {
            progressBarController.StopProgress();
        }
        if (cmdScanEffect != null)
        {
            cmdScanEffect.StopScan();
        }
        if (typingText != null)
        {
            typingText.SkipToEnd();
        }

        SetPanelState(loading: false, mission: false);
    }

    private IEnumerator ShowFlowCoroutine()
    {
        SetPanelState(loading: true, mission: false);

        // 流程开始：统一触发所有动效
        // 1) 进度条（时长对齐）
        if (progressBarController != null)
        {
            progressBarController.SetLoadingDuration(Mathf.Max(0.01f, loadingSeconds));
            progressBarController.SetupImageFillProperties();
            progressBarController.StartProgress();
        }

        // 2) 打字机效果（时长对齐）
        if (typingText != null)
        {
            typingText.duration = Mathf.Max(0.01f, loadingSeconds);
            typingText.Play();
        }

        // 3) CMD 扫描动效
        if (cmdScanEffect != null)
        {
            cmdScanEffect.SetFastMode(cmdFastMode);
            cmdScanEffect.SetLineSpeed(Mathf.Max(0.01f, cmdLineSpeed));
            cmdScanEffect.RestartScan();
        }

        // 等待 Loading 时长 + 1s 缓冲
        yield return new WaitForSeconds(loadingSeconds);
        yield return new WaitForSeconds(1f);

        // 收尾：停止持续型动效
        if (cmdScanEffect != null)
        {
            cmdScanEffect.StopScan();
        }

        SetPanelState(loading: false, mission: true);
        flowCoroutine = null;
    }

    private void EnsureRootActive()
    {
        if (rootCanvas != null)
        {
            rootCanvas.gameObject.SetActive(true);
            rootCanvas.enabled = true;
        }
    }

    private void SetPanelState(bool loading, bool mission)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(loading);
        }
        if (missionPanel != null)
        {
            missionPanel.SetActive(mission);
        }
    }
}


