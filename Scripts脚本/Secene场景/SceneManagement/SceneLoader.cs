using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    // 单例模式实例
    public static SceneLoader instance { get; private set; }

    [Header("加载界面UI组件")]
    [Tooltip("加载进度条滑块组件")]
    public Slider loadingSlider;

    [Tooltip("加载进度文本组件")]
    public TextMeshProUGUI loadingText;

    [Tooltip("加载界面画布组件")]
    public GameObject loadingCanvas;

    [Header("强制加载时间设置")]
    [Tooltip("强制最小加载时间（秒）")]
    public float forceLoadingTime = 5f;

    [Header("场景管理设置")]
    [Tooltip("可切换的场景列表")]
    public List<string> availableScenes = new List<string>();

    // 添加缺少的isLoading变量
    private bool isLoading = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 保持对象不被销毁

            // 确保加载界面画布也保持不被销毁
            if (loadingCanvas != null)
            {
                DontDestroyOnLoad(loadingCanvas);
                // 初始状态隐藏加载界面
                loadingCanvas.SetActive(false);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 场景切换
    public void TransitionToScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("正在加载场景中，请稍等...");
            return;
        }

        // 检查场景名称是否有效
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称不能为空");
            return;
        }

        Debug.Log($"准备切换到场景: {sceneName}");
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    // 场景切换协程
    public IEnumerator TransitionCoroutine(string NewsceneName)
    {
        isLoading = true;

        Debug.Log($"开始加载场景: {NewsceneName}");

        // 显示加载界面
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            Debug.Log("显示加载界面");

            // 确保加载界面在最顶层
            Canvas canvas = loadingCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 1000; // 设置最高层级
            }
        }
        else
        {
            Debug.LogWarning("加载界面画布未设置");
        }

        // 这里可以添加一些预加载逻辑
        Debug.Log("执行预加载操作...");

        // 这里可以添加一些额外的预加载操作，比如清理内存等
        Debug.Log("开始异步加载...");

        // 记录开始时间
        float startTime = Time.time;

        // 异步加载场景 - 使用UnityEngine.SceneManagement.SceneManager
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(NewsceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"无法加载场景: {NewsceneName}，请检查场景是否已添加到Build Settings中");
            isLoading = false;
            if (loadingCanvas != null)
                loadingCanvas.SetActive(false);
            yield break;
        }

        // 阻止场景自动激活，等待我们手动激活
        asyncLoad.allowSceneActivation = false;

        bool sceneLoaded = false;
        bool timeCompleted = false;

        // 等待场景加载完成和强制最小时间
        while (!sceneLoaded || !timeCompleted)
        {
            // 计算已过去的时间
            float elapsedTime = Time.time - startTime;

            // 计算真实场景加载进度
            float sceneProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 计算时间进度
            float timeProgress = Mathf.Clamp01(elapsedTime / forceLoadingTime);

            // 取较小值作为显示进度，确保不会超过真实加载进度
            float displayProgress = Mathf.Min(sceneProgress, timeProgress);

            // 更新UI
            if (loadingSlider != null)
                loadingSlider.value = displayProgress;

            if (loadingText != null)
                loadingText.text = $"Loading... {(displayProgress * 100):F0}%";

            Debug.Log($"加载进度: {(displayProgress * 100):F1}% (场景: {(sceneProgress * 100):F1}%, 时间: {(timeProgress * 100):F1}%)");

            // 检查场景是否加载完成
            if (asyncLoad.progress >= 0.9f && !sceneLoaded)
            {
                sceneLoaded = true;
                Debug.Log("场景加载完成");
            }

            // 检查时间是否达到
            if (elapsedTime >= forceLoadingTime && !timeCompleted)
            {
                timeCompleted = true;
                Debug.Log("强制最小时间完成");
            }

            // 当场景加载完成且时间也达到时，准备激活场景
            if (sceneLoaded && timeCompleted)
            {
                // 确保进度条显示100%
                if (loadingSlider != null)
                    loadingSlider.value = 1f;

                if (loadingText != null)
                    loadingText.text = "LoadingComplete";

                Debug.Log("准备激活场景...");

                // 等待一小段时间显示完成状态
                yield return new WaitForSeconds(0.2f);

                // 激活场景
                asyncLoad.allowSceneActivation = true;
                Debug.Log("场景已允许激活，等待场景切换...");
                break;
            }

            yield return null;
        }

        // 等待场景真正完成激活
        float waitStartTime = Time.time;
        float maxWaitTime = 10f; // 最大等待时间

        while (!asyncLoad.isDone)
        {
            if (Time.time - waitStartTime > maxWaitTime)
            {
                Debug.LogError("场景激活超时，强制结束...");
                break;
            }

            Debug.Log($"等待场景激活... 进度: {asyncLoad.progress * 100:F1}%");
            yield return new WaitForSeconds(0.1f); // 减少日志频率
        }

        Debug.Log("场景激活完成，开始清理...");

        // 这里可以添加一些后处理逻辑
        Debug.Log("执行后处理操作...");

        // 可以在这里添加一些场景切换后的初始化工作
        // 比如重新设置玩家位置、初始化UI等

        // 可以添加一些清理工作
        // 比如清理旧场景的残留对象等

        // 等待一帧确保所有初始化完成
        yield return new WaitForEndOfFrame();

        // 隐藏加载界面
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
            Debug.Log("隐藏加载界面");
        }

        // 这里可以触发一些场景切换完成的事件
        Debug.Log("场景切换完成");

        isLoading = false;
    }

    // 外部设置加载画布的方法
    public void SetLoadingCanvas(GameObject canvas)
    {
        loadingCanvas = canvas;
        if (loadingCanvas != null)
        {
            DontDestroyOnLoad(loadingCanvas);
            loadingCanvas.SetActive(false);
        }
    }

    // 通过场景名称切换（支持外部调用，主要用于UI按钮）
    public static void SwitchScene(string sceneName)
    {
        if (instance != null)
        {
            instance.TransitionToScene(sceneName);
        }
        else
        {
            Debug.LogError("SceneLoader实例不存在");
        }
    }

    // 检查场景是否在可用列表中
    public bool IsSceneAvailable(string sceneName)
    {
        return availableScenes.Contains(sceneName);
    }

    // 获取可用场景列表
    public List<string> GetAvailableScenes()
    {
        return new List<string>(availableScenes);
    }
}
