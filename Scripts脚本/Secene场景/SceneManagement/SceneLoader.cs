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
    [Header("加载界面UI预制体")]
    [SerializeField] private GameObject loadingUIPrefab;        // 加载界面UI预制体

    [Header("加载界面UI组件（可选，如果使用预制体则忽略）")]
    [SerializeField] private GameObject loadingPanel;           // 加载界面面板
    [SerializeField] private Slider progressBar;               // 进度条
    [SerializeField] private TextMeshProUGUI loadingText;      // 加载文本
    [SerializeField] private TextMeshProUGUI progressText;     // 进度百分比文本

    [Header("加载设置")]
    [SerializeField] private float minimumLoadTime = 2f;       // 最小加载时间（秒）
    [SerializeField] private bool allowSceneActivation = true; // 是否允许场景激活
    [SerializeField] private bool forceMinimumLoadTime = true; // 强制最小加载时间

    private static SceneLoader instance;
    private GameObject currentLoadingUI;                        // 当前实例化的加载UI
    private Slider currentProgressBar;                          // 当前进度条引用
    private TextMeshProUGUI currentLoadingText;               // 当前加载文本引用
    private TextMeshProUGUI currentProgressText;              // 当前进度文本引用

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

        // 创建并显示加载界面
        CreateLoadingUI();
        ShowLoadingUI(true);
        UpdateLoadingText($"正在加载 {sceneName}...");

        // 开始异步加载场景
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        if (asyncOperation == null)
        {
            Debug.LogError($"SceneLoader: 无法加载场景 {sceneName}，请检查场景是否存在于Build Settings中");
            DestroyLoadingUI();
            MessagingCenter.Instance.Send(new SceneLoadCompleteMessage(sceneName, false));
            yield break;
        }

        // 防止场景自动激活（可选）
        asyncOperation.allowSceneActivation = allowSceneActivation;

        // 等待场景加载完成
        while (!asyncOperation.isDone)
        {
            // 计算加载进度（0-0.9是加载，0.9-1.0是激活）
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);

            // 更新进度条和文本
            UpdateProgressBar(progress);
            UpdateProgressText(progress);

            // 发送进度消息
            MessagingCenter.Instance.Send(new SceneLoadProgressMessage(sceneName, progress));

            // 如果加载完成但场景未激活，且达到最小加载时间
            if (asyncOperation.progress >= 0.9f && !allowSceneActivation)
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime >= minimumLoadTime)
                {
                    UpdateLoadingText("按任意键继续...");

                    // 等待用户输入
                    if (Input.anyKeyDown)
                    {
                        asyncOperation.allowSceneActivation = true;
                    }
                }
            }

            yield return null;
        }

        // 强制等待最小加载时间
        if (forceMinimumLoadTime)
        {
            float totalElapsedTime = Time.time - startTime;
            if (totalElapsedTime < minimumLoadTime)
            {
                float remainingTime = minimumLoadTime - totalElapsedTime;
                UpdateLoadingText($"加载完成，剩余等待时间: {remainingTime:F1}秒");

                // 在剩余时间内继续更新进度为100%
                while (remainingTime > 0)
                {
                    UpdateProgressBar(1f);
                    UpdateProgressText(1f);
                    remainingTime -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 销毁加载界面
        DestroyLoadingUI();

        // 发送加载完成消息
        MessagingCenter.Instance.Send(new SceneLoadCompleteMessage(sceneName, true));

        Debug.Log($"SceneLoader: 场景 {sceneName} 加载完成");
    }

    // 创建加载UI
    private void CreateLoadingUI()
    {
        // 如果已存在加载UI，先销毁
        if (currentLoadingUI != null)
        {
            DestroyLoadingUI();
        }

        // 优先使用预制体
        if (loadingUIPrefab != null)
        {
            currentLoadingUI = Instantiate(loadingUIPrefab);
            DontDestroyOnLoad(currentLoadingUI);

            // 从预制体中获取组件引用
            currentProgressBar = currentLoadingUI.GetComponentInChildren<Slider>();
            currentLoadingText = currentLoadingUI.GetComponentInChildren<TextMeshProUGUI>();

            // 如果有多个TextMeshProUGUI，尝试找到进度文本
            TextMeshProUGUI[] texts = currentLoadingUI.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 1)
            {
                // 假设第一个是加载文本，第二个是进度文本
                currentLoadingText = texts[0];
                currentProgressText = texts[1];
            }
            else if (texts.Length == 1)
            {
                currentLoadingText = texts[0];
                currentProgressText = null;
            }

            Debug.Log("SceneLoader: 使用预制体创建加载UI");
        }
        else
        {
            // 使用原有的组件引用
            currentProgressBar = progressBar;
            currentLoadingText = loadingText;
            currentProgressText = progressText;

            Debug.Log("SceneLoader: 使用现有组件作为加载UI");
        }
    }

    // 销毁加载UI
    private void DestroyLoadingUI()
    {
        if (currentLoadingUI != null)
        {
            Destroy(currentLoadingUI);
            currentLoadingUI = null;
        }

        // 清空组件引用
        currentProgressBar = null;
        currentLoadingText = null;
        currentProgressText = null;

        // 如果使用的是原有组件，隐藏它们
        ShowLoadingUI(false);
    }

    // 显示/隐藏加载界面
    private void ShowLoadingUI(bool show)
    {
        if (currentLoadingUI != null)
        {
            currentLoadingUI.SetActive(show);
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }

    // 更新加载文本
    private void UpdateLoadingText(string text)
    {
        if (currentLoadingText != null)
        {
            currentLoadingText.text = text;
        }
    }

    // 更新进度条
    private void UpdateProgressBar(float progress)
    {
        if (currentProgressBar != null)
        {
            currentProgressBar.value = progress;
        }
    }

    // 更新进度百分比文本
    private void UpdateProgressText(float progress)
    {
        if (currentProgressText != null)
        {
            currentProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    // 公共方法：设置加载UI预制体
    public void SetLoadingUIPrefab(GameObject prefab)
    {
        loadingUIPrefab = prefab;
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
}