using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 自动CMD扫盘动效
/// 一打开界面就自动开始不断生成代码的CMD风格动效
/// 
/// 速度调整说明：
/// - lineSpeed: 数值越小显示越快（0.01 = 极快，0.1 = 较慢）
/// - fastMode: 勾选后速度提升5倍
/// - 运行时可通过 SetLineSpeed() 和 SetFastMode() 方法动态调整
/// </summary>
public class AutoCMDScanEffect : MonoBehaviour
{
    [Header("组件设置")]
    [Tooltip("显示CMD文本的TMP组件 - 如果为空会自动查找当前GameObject上的TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI textComponent;
    
    [Header("动效设置")]
    [Tooltip("每行显示间隔（秒）- 数值越小显示越快，建议范围：0.01-0.1")]
    [SerializeField] private float lineSpeed = 0.03f;  // 每行显示间隔（降低到0.03秒，更快）
    [Tooltip("最大显示行数 - 超过此数量会自动滚动删除旧行")]
    [SerializeField] private int maxVisibleLines = 15; // 最大显示行数
    [Tooltip("启用滚动效果 - 当行数超过最大值时滚动显示")]
    [SerializeField] private bool enableScrolling = true; // 滚动效果
    [Tooltip("高速模式 - 启用后显示速度提升5倍（lineSpeed * 0.2）")]
    [SerializeField] private bool fastMode = false; // 高速模式（更快显示）
    
    [Header("颜色设置")]
    [Tooltip("启用颜色效果 - 为不同类型的文本添加颜色标识（红色错误、绿色成功等）")]
    [SerializeField] private bool useColors = true;
    
    // 私有变量
    private List<string> displayedLines = new List<string>();
    private Coroutine scanCoroutine;
    private bool isRunning = false;
    
    // 代码模板池
    private readonly string[] codeTemplates = {
        ">>> Initializing system modules...",
        ">>> Loading core configuration [config.ini]",
        ">>> Detecting hardware compatibility...",
        ">>> Scanning system resources: CPU, GPU, RAM",
        ">>> Establishing network protocols...",
        ">>> Verifying user permission levels...",
        ">>> Loading mission database...",
        ">>> Initializing AI module [neural_net.dll]",
        ">>> Starting monitoring systems...",
        ">>> Configuring security firewall...",
        ">>> Synchronizing time servers...",
        ">>> Loading graphics rendering engine...",
        ">>> Initializing audio systems...",
        ">>> Establishing encrypted data channels...",
        ">>> Starting backup systems...",
        ">>> Checking CPU status... OK",
        ">>> Checking memory usage... 8.2GB/16GB",
        ">>> Checking GPU drivers... UPDATED",
        ">>> Verifying system integrity... PASSED",
        ">>> Scanning startup items... 12 services",
        ">>> Checking network adapter... CONNECTED",
        ">>> Verifying user permissions... ADMINISTRATOR",
        ">>> Checking firewall status... ENABLED",
        ">>> Scanning system registry... NO ISSUES",
        ">>> Checking disk space... C: 156GB available",
        ">>> Verifying system files... 100% INTACT",
        ">>> Checking drivers... ALL UPDATED",
        ">>> Scanning temporary files... CLEANUP COMPLETE",
        ">>> Analyzing network traffic...",
        ">>> Verifying digital signatures...",
        ">>> Checking registry entries...",
        ">>> Scanning for vulnerabilities...",
        ">>> Monitoring active processes...",
        ">>> Updating security definitions...",
        ">>> Optimizing system performance...",
        ">>> Backing up critical data...",
        ">>> Synchronizing with remote servers...",
        ">>> Calibrating sensor arrays...",
        ">>> Loading weapon systems...",
        ">>> Activating defense protocols...",
        ">>> Establishing secure channels...",
        ">>> Deploying surveillance network...",
        ">>> Initializing threat detection...",
        ">>> Configuring auto-targeting...",
        ">>> Loading tactical database...",
        ">>> Activating mission parameters...",
        ">>> Preparing for deployment...",
        "[INFO] All modules loaded successfully",
        "[INFO] System status: OPERATIONAL",
        "[INFO] Mission parameters: ACTIVE",
        "[SUCCESS] Ready for commands...",
        "[WARNING] Elevated threat level detected",
        "[ERROR] Connection timeout - retrying...",
        "[SYSTEM] Automatic backup initiated",
        "[SCAN] Detecting network intrusion attempts...",
        "[ALERT] Security protocol activated"
    };
    
    // 状态信息模板
    private readonly string[] statusTemplates = {
        "System uptime: {0} hours",
        "Memory usage: {1}%",
        "CPU load: {2}%",
        "Network traffic: {3} Mbps",
        "Active connections: {4}",
        "Processes running: {5}",
        "Disk I/O: {6} MB/s",
        "Temperature: {7}°C"
    };
    
    #region Unity生命周期
    
    private void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
    }
    
    private void Start()
    {
        // 如果游戏开始时Canvas就是激活的，则启动
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedStart());
        }
    }
    
    private void OnEnable()
    {
        // Canvas/GameObject激活时立即开始显示
        if (Application.isPlaying)
        {
            StartCoroutine(DelayedStart());
        }
    }
    
    private IEnumerator DelayedStart()
    {
        yield return null; // 等待一帧确保组件初始化完成
        
        // 停止之前的动画（如果有）
        if (isRunning)
        {
            StopScan();
        }
        
        StartScan();
    }
    
    private void OnDisable()
    {
        StopScan();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 开始扫描动效
    /// </summary>
    public void StartScan()
    {
        // 确保文本组件存在
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Debug.LogError("AutoCMDScanEffect: 找不到TextMeshProUGUI组件！请确保此脚本挂载在包含TextMeshProUGUI的GameObject上，或手动设置textComponent引用。");
                return;
            }
        }
        
        if (isRunning)
        {
            Debug.LogWarning("AutoCMDScanEffect: 扫描动效已经在运行中");
            return;
        }
        
        isRunning = true;
        displayedLines.Clear();
        textComponent.text = ">>> Initializing CMD interface..."; // 立即显示第一行
        
        scanCoroutine = StartCoroutine(ScanCoroutine());
        Debug.Log("AutoCMDScanEffect: 开始自动扫描动效");
    }
    
    /// <summary>
    /// 停止扫描动效
    /// </summary>
    public void StopScan()
    {
        if (!isRunning) return;
        
        isRunning = false;
        
        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
        }
        
        // 清理显示内容
        displayedLines.Clear();
        if (textComponent != null)
        {
            textComponent.text = "";
        }
        
        Debug.Log("AutoCMDScanEffect: 停止自动扫描动效");
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 扫描协程 - 持续生成内容
    /// </summary>
    private IEnumerator ScanCoroutine()
    {
        // 立即添加第一行到显示列表
        displayedLines.Add(">>> Initializing CMD interface...");
        UpdateDisplay();
        
        // 根据模式调整初始等待时间
        float initialWait = fastMode ? lineSpeed * 0.1f : lineSpeed * 0.5f;
        yield return new WaitForSeconds(initialWait);
        
        while (isRunning)
        {
            // 随机选择一行代码
            string newLine = GetRandomCodeLine();
            
            // 添加到显示列表
            displayedLines.Add(newLine);
            
            // 滚动效果：保持最大显示行数
            if (enableScrolling && displayedLines.Count > maxVisibleLines)
            {
                displayedLines.RemoveAt(0);
            }
            
            // 更新显示
            UpdateDisplay();
            
            // 根据模式调整等待时间
            float actualLineSpeed = fastMode ? lineSpeed * 0.2f : lineSpeed;
            float waitTime = Random.Range(actualLineSpeed * 0.3f, actualLineSpeed * 0.8f);
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    /// <summary>
    /// 获取随机代码行
    /// </summary>
    private string GetRandomCodeLine()
    {
        // 90%概率显示普通代码，10%概率显示状态信息
        if (Random.Range(0f, 1f) < 0.9f)
        {
            // 普通代码模板
            string line = codeTemplates[Random.Range(0, codeTemplates.Length)];
            return ApplyRandomColorCoding(line);
        }
        else
        {
            // 状态信息模板
            string template = statusTemplates[Random.Range(0, statusTemplates.Length)];
            string line = string.Format(template, 
                Random.Range(1, 99),    // 通用数值1
                Random.Range(10, 95),   // 通用数值2  
                Random.Range(5, 85),    // 通用数值3
                Random.Range(10, 1000), // 通用数值4
                Random.Range(5, 50),    // 通用数值5
                Random.Range(20, 200),  // 通用数值6
                Random.Range(1, 100),   // 通用数值7
                Random.Range(35, 75)    // 通用数值8
            );
            return ApplyRandomColorCoding("[STATUS] " + line);
        }
    }
    
    /// <summary>
    /// 随机应用颜色编码
    /// </summary>
    private string ApplyRandomColorCoding(string line)
    {
        if (!useColors) return line;
        
        // 根据关键词和随机因素应用颜色
        if (line.Contains("ERROR") || line.Contains("FAILED") || line.Contains("CRITICAL"))
        {
            return "<color=#FF0000>" + line + "</color>"; // 红色
        }
        else if (line.Contains("WARNING") || line.Contains("ALERT"))
        {
            return "<color=#FFFF00>" + line + "</color>"; // 黄色
        }
        else if (line.Contains("SUCCESS") || line.Contains("COMPLETE") || line.Contains("OK") || line.Contains("PASSED"))
        {
            return "<color=#00FF00>" + line + "</color>"; // 绿色
        }
        else if (line.Contains("INFO") || line.Contains("STATUS"))
        {
            return "<color=#00FFFF>" + line + "</color>"; // 青色
        }
        else if (line.Contains("SYSTEM") || line.Contains(">>>"))
        {
            return "<color=#FFFFFF>" + line + "</color>"; // 白色
        }
        else
        {
            // 随机给一些行添加轻微的绿色调
            if (Random.Range(0f, 1f) < 0.3f)
            {
                return "<color=#88FF88>" + line + "</color>"; // 浅绿色
            }
        }
        
        return line;
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (textComponent == null) return;
        
        textComponent.text = string.Join("\n", displayedLines);
    }
    
    #endregion
    
    #region 编辑器辅助
    
    /// <summary>
    /// 手动启动扫描（用于测试）
    /// </summary>
    [ContextMenu("手动启动扫描")]
    public void ManualStartScan()
    {
        StartScan();
    }
    
    /// <summary>
    /// 重置并重新开始扫描（用于Canvas重新激活）
    /// </summary>
    public void RestartScan()
    {
        StopScan();
        StartCoroutine(DelayedRestart());
    }
    
    /// <summary>
    /// 设置高速模式
    /// </summary>
    public void SetFastMode(bool enabled)
    {
        fastMode = enabled;
        Debug.Log($"AutoCMDScanEffect: 高速模式 {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 设置显示速度
    /// </summary>
    public void SetLineSpeed(float speed)
    {
        lineSpeed = Mathf.Max(0.01f, speed);
        Debug.Log($"AutoCMDScanEffect: 显示速度设置为 {lineSpeed:F3}秒/行");
    }
    
    private IEnumerator DelayedRestart()
    {
        yield return null; // 等待一帧
        StartScan();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("开始扫描")]
    private void TestStartScan()
    {
        if (Application.isPlaying)
        {
            StartScan();
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("停止扫描")]
    private void TestStopScan()
    {
        if (Application.isPlaying)
        {
            StopScan();
        }
        else
        {
            Debug.Log("请在Play模式下测试");
        }
    }
    
    [ContextMenu("检查组件状态")]
    private void CheckComponentStatus()
    {
        Debug.Log($"AutoCMDScanEffect 状态检查:");
        Debug.Log($"- textComponent: {(textComponent != null ? "已设置" : "未设置")}");
        Debug.Log($"- isRunning: {isRunning}");
        Debug.Log($"- lineSpeed: {lineSpeed:F3}秒");
        Debug.Log($"- fastMode: {(fastMode ? "启用" : "禁用")}");
        Debug.Log($"- displayedLines.Count: {displayedLines.Count}");
        Debug.Log($"- gameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
        
        if (textComponent == null)
        {
            var tmp = GetComponent<TextMeshProUGUI>();
            Debug.Log($"- GetComponent<TextMeshProUGUI>(): {(tmp != null ? "找到" : "未找到")}");
        }
    }
    
    [ContextMenu("切换高速模式")]
    private void TestToggleFastMode()
    {
        if (Application.isPlaying)
        {
            SetFastMode(!fastMode);
        }
        else
        {
            fastMode = !fastMode;
            Debug.Log($"高速模式设置为: {(fastMode ? "启用" : "禁用")} (需要在Play模式下才能看到效果)");
        }
    }
    
    [ContextMenu("设置极快速度 (0.01秒)")]
    private void TestSetVeryFastSpeed()
    {
        if (Application.isPlaying)
        {
            SetLineSpeed(0.01f);
        }
        else
        {
            lineSpeed = 0.01f;
            Debug.Log("速度设置为极快 (0.01秒/行) - 需要在Play模式下才能看到效果");
        }
    }
    
    [ContextMenu("设置快速 (0.02秒)")]
    private void TestSetFastSpeed()
    {
        if (Application.isPlaying)
        {
            SetLineSpeed(0.02f);
        }
        else
        {
            lineSpeed = 0.02f;
            Debug.Log("速度设置为快速 (0.02秒/行) - 需要在Play模式下才能看到效果");
        }
    }
    
    [ContextMenu("设置正常速度 (0.05秒)")]
    private void TestSetNormalSpeed()
    {
        if (Application.isPlaying)
        {
            SetLineSpeed(0.05f);
        }
        else
        {
            lineSpeed = 0.05f;
            Debug.Log("速度设置为正常 (0.05秒/行) - 需要在Play模式下才能看到效果");
        }
    }
    #endif
    
    #endregion
}
