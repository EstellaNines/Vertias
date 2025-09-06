using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Linq;

/// <summary>
/// CMD扫盘风格的代码行滚动效果
/// 模拟系统扫描、检查文件、加载模块等过程
/// </summary>
public class CMDScannerEffect : MonoBehaviour
{
    [Header("组件引用")]
    [FieldLabel("文本显示组件")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [FieldLabel("容器变换组件")]
    [SerializeField] private RectTransform container;
    
    [Header("动画设置")]
    [FieldLabel("每行显示速度")]
    [Tooltip("每行代码显示的间隔时间 - 数值越大显示越慢，数值越小显示越快")]
    [SerializeField] private float lineDisplaySpeed = 0.05f;        // 每行显示速度
    [FieldLabel("文本滚动速度")]
    [Tooltip("单个字符打字机效果的速度 - 数值越大打字越慢，数值越小打字越快")]
    [SerializeField] private float scrollSpeed = 0.02f;             // 文本滚动速度
    [FieldLabel("最大可见行数")]
    [SerializeField] private int maxVisibleLines = 15;              // 最大可见行数
    [FieldLabel("自动开始")]
    [SerializeField] private bool autoStart = true;                 // 自动开始
    [FieldLabel("循环动画")]
    [SerializeField] private bool loopAnimation = true;             // 循环动画
    
    [Header("视觉效果")]
    [FieldLabel("普通文本颜色")]
    [SerializeField] private Color normalTextColor = Color.green;   // 普通文本颜色
    [FieldLabel("重要文本颜色")]
    [SerializeField] private Color importantTextColor = Color.cyan; // 重要文本颜色
    [FieldLabel("错误文本颜色")]
    [SerializeField] private Color errorTextColor = Color.red;      // 错误文本颜色
    [FieldLabel("成功文本颜色")]
    [SerializeField] private Color successTextColor = Color.yellow; // 成功文本颜色
    [FieldLabel("使用打字机效果")]
    [SerializeField] private bool useTypewriterEffect = true;       // 使用打字机效果
    
    [Header("音效设置")]
    [FieldLabel("音频源")]
    [SerializeField] private AudioSource audioSource;
    [FieldLabel("打字音效")]
    [SerializeField] private AudioClip typingSound;
    [FieldLabel("音效音量")]
    [SerializeField] private float soundVolume = 0.3f;
    
    [Header("调试")]
    [FieldLabel("显示调试信息")]
    [SerializeField] private bool showDebugInfo = false;
    
    private List<string> codeLines = new List<string>();
    private List<string> displayedLines = new List<string>();
    private Coroutine scannerCoroutine;
    private bool isRunning = false;
    private int currentLineIndex = 0;
    
    void Start()
    {
        InitializeCodeLines();
        
        if (textDisplay == null)
            textDisplay = GetComponent<TextMeshProUGUI>();
            
        if (autoStart)
        {
            StartScanner();
        }
    }
    
    /// <summary>
    /// 初始化代码行数据
    /// </summary>
    private void InitializeCodeLines()
    {
        codeLines = new List<string>
        {
            // 系统启动序列
            "<color=#00FF00>[SYSTEM]</color> Initializing L.S.S. Mission Terminal...",
            "<color=#FFFF00>[BOOT]</color> Loading kernel modules... OK",
            "<color=#00FFFF>[NETWORK]</color> Establishing secure connection... 192.168.1.100:8443",
            "<color=#FFFFFF>[AUTH]</color> Authenticating user credentials...",
            "<color=#FFFF00>[AUTH]</color> Access granted - Agent clearance level: CLASSIFIED",
            
            // 文件系统扫描
            "<color=#00FF00>[SCANNER]</color> Scanning mission database...",
            "<color=#FFFFFF>[FS]</color> /missions/active/*.dat - 3 files found",
            "<color=#FFFFFF>[FS]</color> /missions/pending/*.dat - 7 files found",
            "<color=#FFFFFF>[FS]</color> /missions/classified/*.enc - 2 files found",
            "<color=#00FFFF>[DECRYPT]</color> Decrypting classified mission data...",
            
            // 安全检查
            "<color=#FFFF00>[SECURITY]</color> Running integrity checks...",
            "<color=#00FF00>[SECURITY]</color> File signatures verified",
            "<color=#00FF00>[SECURITY]</color> No malicious code detected",
            "<color=#FFFFFF>[FIREWALL]</color> Port 8443: OPEN - Secure tunnel established",
            
            // 模块加载
            "<color=#00FFFF>[MODULE]</color> Loading mission_parser.dll... OK",
            "<color=#00FFFF>[MODULE]</color> Loading crypto_engine.dll... OK",
            "<color=#00FFFF>[MODULE]</color> Loading ui_renderer.dll... OK",
            "<color=#00FFFF>[MODULE]</color> Loading audio_system.dll... OK",
            
            // 数据库连接
            "<color=#FFFFFF>[DATABASE]</color> Connecting to mission server...",
            "<color=#FFFF00>[DATABASE]</color> Server response: 200 OK",
            "<color=#00FF00>[DATABASE]</color> Mission data synchronized",
            
            // 系统检查
            "<color=#FFFFFF>[SYSTEM]</color> Checking hardware compatibility...",
            "<color=#00FF00>[HARDWARE]</color> GPU: Compatible - DirectX 12 supported",
            "<color=#00FF00>[HARDWARE]</color> RAM: 16GB available",
            "<color=#00FF00>[HARDWARE]</color> Storage: 2.3TB free space",
            
            // 网络诊断
            "<color=#FFFFFF>[NETWORK]</color> Running network diagnostics...",
            "<color=#00FF00>[PING]</color> mission-server.lss.gov: 23ms",
            "<color=#00FF00>[PING]</color> backup-server.lss.gov: 45ms",
            "<color=#FFFF00>[BANDWIDTH]</color> Download: 1.2Gbps | Upload: 800Mbps",
            
            // 加密系统
            "<color=#00FFFF>[CRYPTO]</color> Initializing AES-256 encryption...",
            "<color=#00FFFF>[CRYPTO]</color> Generating session keys...",
            "<color=#00FF00>[CRYPTO]</color> Secure channel established",
            
            // 任务系统
            "<color=#FFFFFF>[MISSION_SYS]</color> Loading mission templates...",
            "<color=#FFFFFF>[MISSION_SYS]</color> Parsing objective parameters...",
            "<color=#00FF00>[MISSION_SYS]</color> Mission system ready",
            
            // 用户界面
            "<color=#00FFFF>[UI]</color> Rendering user interface components...",
            "<color=#00FFFF>[UI]</color> Loading mission briefing assets...",
            "<color=#00FFFF>[UI]</color> Initializing interactive elements...",
            
            // 最终检查
            "<color=#FFFF00>[FINAL_CHECK]</color> Running final system verification...",
            "<color=#00FF00>[VERIFICATION]</color> All systems nominal",
            "<color=#00FF00>[STATUS]</color> Mission terminal ready for operation",
            "<color=#FFFF00>[READY]</color> Awaiting mission selection...",
            
            // 循环内容 - 保持活跃状态
            "<color=#FFFFFF>[MONITOR]</color> System monitoring active...",
            "<color=#FFFFFF>[HEARTBEAT]</color> Server connection stable",
            "<color=#FFFFFF>[IDLE]</color> Standby mode - All systems operational"
        };
        
        if (showDebugInfo)
        {
            Debug.Log($"CMDScannerEffect: 已加载 {codeLines.Count} 行代码");
        }
    }
    
    /// <summary>
    /// 开始扫描动画
    /// </summary>
    public void StartScanner()
    {
        if (isRunning) return;
        
        isRunning = true;
        currentLineIndex = 0;
        displayedLines.Clear();
        
        if (textDisplay != null)
        {
            textDisplay.text = "";
            textDisplay.color = normalTextColor;
        }
        
        scannerCoroutine = StartCoroutine(ScannerAnimation());
        
        if (showDebugInfo)
        {
            Debug.Log("CMDScannerEffect: 扫描动画开始");
        }
    }
    
    /// <summary>
    /// 停止扫描动画
    /// </summary>
    public void StopScanner()
    {
        if (!isRunning) return;
        
        isRunning = false;
        
        if (scannerCoroutine != null)
        {
            StopCoroutine(scannerCoroutine);
            scannerCoroutine = null;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("CMDScannerEffect: 扫描动画停止");
        }
    }
    
    /// <summary>
    /// 扫描动画协程
    /// </summary>
    private IEnumerator ScannerAnimation()
    {
        while (isRunning)
        {
            // 添加新行
            if (currentLineIndex < codeLines.Count)
            {
                string newLine = codeLines[currentLineIndex];
                
                // 使用打字机效果显示行
                if (useTypewriterEffect)
                {
                    yield return StartCoroutine(TypewriterLine(newLine));
                }
                else
                {
                    AddLine(newLine);
                    PlayTypingSound();
                }
                
                currentLineIndex++;
            }
            else if (loopAnimation)
            {
                // 重置循环
                currentLineIndex = 0;
                yield return new WaitForSeconds(1f); // 循环间隔
                continue;
            }
            else
            {
                break; // 结束动画
            }
            
            yield return new WaitForSeconds(lineDisplaySpeed);
        }
        
        isRunning = false;
    }
    
    /// <summary>
    /// 打字机效果显示单行
    /// </summary>
    private IEnumerator TypewriterLine(string line)
    {
        string displayLine = "";
        
        // 移除富文本标签来计算实际字符数
        string plainText = System.Text.RegularExpressions.Regex.Replace(line, "<.*?>", string.Empty);
        
        for (int i = 0; i <= line.Length; i++)
        {
            if (i < line.Length)
            {
                displayLine = line.Substring(0, i + 1);
            }
            else
            {
                displayLine = line;
            }
            
            // 更新显示
            var tempLines = new List<string>(displayedLines) { displayLine };
            UpdateDisplay(tempLines);
            
            // 播放打字音效
            if (i < plainText.Length && char.IsLetterOrDigit(plainText[Mathf.Min(i, plainText.Length - 1)]))
            {
                PlayTypingSound();
            }
            
            yield return new WaitForSeconds(scrollSpeed);
        }
        
        // 添加完整行到显示列表
        AddLine(line);
    }
    
    /// <summary>
    /// 添加新行到显示列表
    /// </summary>
    private void AddLine(string line)
    {
        displayedLines.Add(line);
        
        // 限制可见行数
        if (displayedLines.Count > maxVisibleLines)
        {
            displayedLines.RemoveAt(0);
        }
        
        UpdateDisplay(displayedLines);
    }
    
    /// <summary>
    /// 更新文本显示
    /// </summary>
    private void UpdateDisplay(List<string> lines)
    {
        if (textDisplay != null)
        {
            textDisplay.text = string.Join("\n", lines);
        }
    }
    
    /// <summary>
    /// 播放打字音效
    /// </summary>
    private void PlayTypingSound()
    {
        if (audioSource != null && typingSound != null)
        {
            audioSource.PlayOneShot(typingSound, soundVolume);
        }
    }
    
    /// <summary>
    /// 设置扫描速度
    /// </summary>
    public void SetScanSpeed(float lineSpeed, float scrollSpeed)
    {
        this.lineDisplaySpeed = lineSpeed;
        this.scrollSpeed = scrollSpeed;
    }
    
    /// <summary>
    /// 设置最大可见行数
    /// </summary>
    public void SetMaxVisibleLines(int maxLines)
    {
        this.maxVisibleLines = maxLines;
    }
    
    /// <summary>
    /// 清空显示
    /// </summary>
    public void ClearDisplay()
    {
        displayedLines.Clear();
        if (textDisplay != null)
        {
            textDisplay.text = "";
        }
    }
    
    /// <summary>
    /// 添加自定义代码行
    /// </summary>
    public void AddCustomLine(string line)
    {
        codeLines.Add(line);
    }
    
    /// <summary>
    /// 设置循环模式
    /// </summary>
    public void SetLoopMode(bool loop)
    {
        loopAnimation = loop;
    }
    
    void OnDestroy()
    {
        StopScanner();
    }
    
    // 编辑器调试方法
    [ContextMenu("开始扫描")]
    private void TestStartScanner()
    {
        StartScanner();
    }
    
    [ContextMenu("停止扫描")]
    private void TestStopScanner()
    {
        StopScanner();
    }
    
    [ContextMenu("清空显示")]
    private void TestClearDisplay()
    {
        ClearDisplay();
    }
}
