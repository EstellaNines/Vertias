using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using GlobalMessaging;

// 场景加载开始消息
public struct SceneLoadStartMessage : IMessage
{
    public string SceneName;

    public SceneLoadStartMessage(string sceneName)
    {
        SceneName = sceneName;
    }
}

// 场景加载完成消息
public struct SceneLoadCompleteMessage : IMessage
{
    public string SceneName;
    public bool Success;

    public SceneLoadCompleteMessage(string sceneName, bool success)
    {
        SceneName = sceneName;
        Success = success;
    }
}

// 场景加载进度消息
public struct SceneLoadProgressMessage : IMessage
{
    public string SceneName;
    public float Progress;

    public SceneLoadProgressMessage(string sceneName, float progress)
    {
        SceneName = sceneName;
        Progress = progress;
    }
}

public class SceneLoader : MonoBehaviour
{
    [Header("加载界面UI组件")]
    [SerializeField] private GameObject loadingPanel;           // 加载界面面板
    [SerializeField] private Slider progressBar;               // 进度条
    [SerializeField] private TextMeshProUGUI loadingText;      // 加载文本
    [SerializeField] private TextMeshProUGUI progressText;     // 进度百分比文本
    [SerializeField] private LoadingUIController loadingUIController; // 加载UI控制器

    [Header("自定义文本内容")]
    [SerializeField] private string customLoadingText = "Loading...";     // 自定义加载文本
    [SerializeField] private string customProgressFormat = "{0}%";        // 自定义进度文本格式，{0}为百分比

    [Header("加载设置")]
    [SerializeField] private float minimumLoadTime = 2f;       // 最小加载时间（秒）
    [SerializeField] private bool allowSceneActivation = true; // 是否允许场景激活
    [SerializeField] private bool forceMinimumLoadTime = true; // 强制最小加载时间

    private static SceneLoader instance;

    public static SceneLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneLoader>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneLoader");
                    instance = go.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 确保强制加载时间为2秒
            if (forceMinimumLoadTime)
            {
                minimumLoadTime = 2f;
            }

            // 确保UI界面默认为隐藏状态
            InitializeUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 异步加载场景
    public void LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: 场景名称为空，无法加载场景");
            MessagingCenter.Instance.Send(new SceneLoadCompleteMessage(sceneName, false));
            return;
        }

        Debug.Log($"SceneLoader: 开始异步加载场景: {sceneName}");

        // 发送场景加载开始消息
        MessagingCenter.Instance.Send(new SceneLoadStartMessage(sceneName));

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    // 场景加载协程
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        float startTime = Time.time;
        float totalLoadTime = minimumLoadTime; // 使用最小加载时间作为总时间
    
        // 显示加载界面
        ShowLoadingUI(true);
        UpdateLoadingText(customLoadingText);
        
        // 显示随机加载内容（图片和描述）
        if (loadingUIController != null)
        {
            loadingUIController.DisplayRandomLoadingContent();
        }
        else
        {
            Debug.LogWarning("SceneLoader: LoadingUIController 引用为空，无法显示随机加载内容");
        }
    
        // 开始异步加载场景
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
    
        if (asyncOperation == null)
        {
            Debug.LogError($"SceneLoader: 无法加载场景 {sceneName}，请检查场景是否存在于Build Settings中");
            ShowLoadingUI(false);
            MessagingCenter.Instance.Send(new SceneLoadCompleteMessage(sceneName, false));
            yield break;
        }
    
        // 防止场景自动激活（可选）
        asyncOperation.allowSceneActivation = allowSceneActivation;
        
        bool sceneLoadComplete = false;
    
        // 等待场景加载完成或达到最小加载时间
        while (Time.time - startTime < totalLoadTime)
        {
            // 计算基于时间的进度（从0%到100%线性增长）
            float timeProgress = (Time.time - startTime) / totalLoadTime;
            timeProgress = Mathf.Clamp01(timeProgress);
            
            // 检查场景加载状态
            if (!sceneLoadComplete && asyncOperation.progress >= 0.9f)
            {
                sceneLoadComplete = true;
                // 如果场景加载完成但不允许激活，在这里激活
                if (!allowSceneActivation)
                {
                    asyncOperation.allowSceneActivation = true;
                }
            }
            
            // 使用时间进度作为显示进度，确保从0%开始
            float displayProgress = timeProgress;
            
            // 更新进度条和文本
            UpdateProgressBar(displayProgress);
            UpdateProgressText(displayProgress);
    
            // 发送进度消息
            MessagingCenter.Instance.Send(new SceneLoadProgressMessage(sceneName, displayProgress));
    
            yield return null;
        }
    
        // 确保最终进度为100%
        UpdateProgressBar(1f);
        UpdateProgressText(1f);
        MessagingCenter.Instance.Send(new SceneLoadProgressMessage(sceneName, 1f));
    
        // 等待场景完全加载完成
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
    
        // 隐藏加载界面
        ShowLoadingUI(false);
    
        // 发送加载完成消息
        MessagingCenter.Instance.Send(new SceneLoadCompleteMessage(sceneName, true));
    
        Debug.Log($"SceneLoader: 场景 {sceneName} 加载完成");
    }

    // 显示/隐藏加载界面
    private void ShowLoadingUI(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
        
        // 如果隐藏加载界面，清空加载内容
        if (!show && loadingUIController != null)
        {
            loadingUIController.ClearLoadingContent();
        }
    }

    // 更新加载文本
    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    // 更新进度条
    private void UpdateProgressBar(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    // 更新进度百分比文本
    // 更新进度百分比文本
    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            try
            {
                // 验证自定义格式是否包含占位符
                if (string.IsNullOrEmpty(customProgressFormat) || !customProgressFormat.Contains("{0}"))
                {
                    // 使用默认格式
                    progressText.text = Mathf.RoundToInt(progress * 100) + "%";
                    Debug.LogWarning("SceneLoader: customProgressFormat格式无效，使用默认格式");
                }
                else
                {
                    // 使用自定义格式
                    progressText.text = string.Format(customProgressFormat, Mathf.RoundToInt(progress * 100));
                }
            }
            catch (System.FormatException e)
            {
                // 格式化异常时使用默认格式
                progressText.text = Mathf.RoundToInt(progress * 100) + "%";
                Debug.LogError($"SceneLoader: 进度文本格式化错误: {e.Message}，使用默认格式");
            }
        }
        else
        {
            Debug.LogWarning("SceneLoader: progressText组件引用为空，无法更新进度文本");
        }
    }

    // 公共方法：设置最小加载时间
    public void SetMinimumLoadTime(float time)
    {
        minimumLoadTime = time;
    }

    // 公共方法：设置是否允许场景激活
    public void SetAllowSceneActivation(bool allow)
    {
        allowSceneActivation = allow;
    }

    // 公共方法：设置是否强制最小加载时间
    public void SetForceMinimumLoadTime(bool force)
    {
        forceMinimumLoadTime = force;
        if (force)
        {
            minimumLoadTime = 2f;
        }
    }

    // 公共方法：设置UI组件引用
    public void SetUIComponents(GameObject panel, Slider slider, TextMeshProUGUI loadText, TextMeshProUGUI progText)
    {
        loadingPanel = panel;
        progressBar = slider;
        loadingText = loadText;
        progressText = progText;
        
        // 设置组件后立即初始化UI状态
        InitializeUI();
    }

    // 公共方法：设置自定义文本
    public void SetCustomTexts(string loadingTextContent, string progressFormat)
    {
        if (!string.IsNullOrEmpty(loadingTextContent))
            customLoadingText = loadingTextContent;
            
        if (!string.IsNullOrEmpty(progressFormat))
        {
            if (IsValidProgressFormat(progressFormat))
            {
                customProgressFormat = progressFormat;
            }
            else
            {
                Debug.LogError($"SceneLoader: 无效的进度格式 '{progressFormat}'，保持原格式");
            }
        }
    }

    // 公共方法：获取当前加载文本
    public string GetLoadingText()
    {
        return customLoadingText;
    }

    // 公共方法：获取当前进度格式
    public string GetProgressFormat()
    {
        return customProgressFormat;
    }

    // 验证进度格式字符串
    private bool IsValidProgressFormat(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;
            
        try
        {
            // 测试格式化
            string test = string.Format(format, 50);
            return format.Contains("{0}");
        }
        catch
        {
            return false;
        }
    }
    
    // 初始化UI状态
    private void InitializeUI()
    {
        // 确保加载界面默认为隐藏状态
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("SceneLoader: 加载界面已初始化为隐藏状态");
        }
        
        // 初始化进度条为0
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        
        // 初始化文本内容
        if (loadingText != null)
        {
            loadingText.text = customLoadingText;
        }
        
        if (progressText != null)
        {
            progressText.text = string.Format(customProgressFormat, 0);
        }
    }

    // 公共方法：设置LoadingUIController引用
    public void SetLoadingUIController(LoadingUIController controller)
    {
        loadingUIController = controller;
    }
}