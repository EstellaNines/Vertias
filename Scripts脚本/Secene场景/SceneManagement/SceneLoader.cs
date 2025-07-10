using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    // 单例模式，不会销毁
    public static SceneLoader instance { get; private set; }

    [Header("加载界面设置")]
    [Tooltip("加载进度条（可选）")]
    public Slider loadingSlider;

    [Tooltip("加载文本（可选）")]
    public TextMeshProUGUI loadingText;

    [Tooltip("加载画布（可选）")]
    public GameObject loadingCanvas;
    
    [Header("加载时间设置")]
    [Tooltip("强制加载时间（秒）")]
    public float forceLoadingTime = 5f;

    private bool isLoading = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 不会被销毁
            
            // 确保加载画布也不会被销毁
            if (loadingCanvas != null)
            {
                DontDestroyOnLoad(loadingCanvas);
                // 初始时隐藏加载画布
                loadingCanvas.SetActive(false);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 加载场景
    public void TransitionToScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("正在加载场景中，请等待...");
            return;
        }
        
        // 检查场景名称是否有效
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称为空！");
            return;
        }

        Debug.Log($"玩家确认切换场景: {sceneName}");
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    // 切换场景协程
    public IEnumerator TransitionCoroutine(string NewsceneName)
    {
        isLoading = true;
        
        Debug.Log($"开始加载场景: {NewsceneName}");

        // 显示加载界面
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            Debug.Log("显示加载画布");
            
            // 确保画布在最前面
            Canvas canvas = loadingCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 1000; // 设置高优先级
            }
        }
        else
        {
            Debug.LogWarning("加载画布未设置！");
        }

        // 保存所有持久化数据
        Debug.Log("保存游戏数据...");

        // 淡出场景（可以在这里添加淡出效果）
        Debug.Log("开始场景切换...");

        // 记录开始时间
        float startTime = Time.time;
        
        // 异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(NewsceneName);
        
        if (asyncLoad == null)
        {
            Debug.LogError($"无法加载场景: {NewsceneName}，请检查场景是否存在于Build Settings中！");
            isLoading = false;
            if (loadingCanvas != null)
                loadingCanvas.SetActive(false);
            yield break;
        }

        // 防止场景在加载完成后立即激活
        asyncLoad.allowSceneActivation = false;
        
        bool sceneLoaded = false;
        bool timeCompleted = false;

        // 等待场景加载和强制加载时间
        while (!sceneLoaded || !timeCompleted)
        {
            // 计算经过的时间
            float elapsedTime = Time.time - startTime;
            
            // 计算场景加载进度
            float sceneProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // 计算时间进度
            float timeProgress = Mathf.Clamp01(elapsedTime / forceLoadingTime);
            
            // 使用较小的进度值来确保不会超过实际加载进度
            float displayProgress = Mathf.Min(sceneProgress, timeProgress);
            
            // 更新UI
            if (loadingSlider != null)
                loadingSlider.value = displayProgress;

            if (loadingText != null)
                loadingText.text = $"加载中... {(displayProgress * 100):F0}%";

            Debug.Log($"加载进度: {(displayProgress * 100):F1}% (场景: {(sceneProgress * 100):F1}%, 时间: {(timeProgress * 100):F1}%)");

            // 检查场景是否加载完成
            if (asyncLoad.progress >= 0.9f && !sceneLoaded)
            {
                sceneLoaded = true;
                Debug.Log("场景加载完成！");
            }
            
            // 检查时间是否完成
            if (elapsedTime >= forceLoadingTime && !timeCompleted)
            {
                timeCompleted = true;
                Debug.Log("强制加载时间完成！");
            }
            
            // 如果两个条件都满足，准备激活场景
            if (sceneLoaded && timeCompleted)
            {
                // 确保进度条显示100%
                if (loadingSlider != null)
                    loadingSlider.value = 1f;
                    
                if (loadingText != null)
                    loadingText.text = "加载完成！";
                    
                Debug.Log("准备激活场景...");
                    
                // 等待一小段时间显示完成状态
                yield return new WaitForSeconds(0.2f);
                
                // 激活场景
                asyncLoad.allowSceneActivation = true;
                Debug.Log("场景已激活，等待场景切换完成...");
                break;
            }

            yield return null;
        }
        
        // 等待场景完全激活（添加超时保护）
        float waitStartTime = Time.time;
        float maxWaitTime = 10f; // 最大等待10秒
        
        while (!asyncLoad.isDone)
        {
            if (Time.time - waitStartTime > maxWaitTime)
            {
                Debug.LogError("场景加载超时！强制继续...");
                break;
            }
            
            Debug.Log($"等待场景完全加载... 进度: {asyncLoad.progress * 100:F1}%");
            yield return new WaitForSeconds(0.1f); // 减少日志频率
        }

        Debug.Log("场景加载完成，开始后续处理...");

        // 加载所有持久化数据
        Debug.Log("加载游戏数据...");

        // 获取目标场景过渡位置
        // 这里可以添加寻找场景中特定位置的逻辑

        // 设置进入游戏对象的位置
        // 这里可以添加设置玩家位置的逻辑

        // 等待一帧确保新场景完全初始化
        yield return new WaitForEndOfFrame();

        // 隐藏加载界面
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
            Debug.Log("隐藏加载画布");
        }

        // 淡入新场景（可以在这里添加淡入效果）
        Debug.Log("场景切换完成！");

        isLoading = false;
    }
    
    // 公共方法：设置加载画布
    public void SetLoadingCanvas(GameObject canvas)
    {
        loadingCanvas = canvas;
        if (loadingCanvas != null)
        {
            DontDestroyOnLoad(loadingCanvas);
            loadingCanvas.SetActive(false);
        }
    }
}
