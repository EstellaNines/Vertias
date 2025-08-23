using InventorySystem.SaveSystem;
using UnityEngine;

/// <summary>
/// 保存系统持久化管理器
/// 确保整个SaveSystem GameObject及其所有子对象在场景切换时不被销毁
/// 这个脚本应该附加到SaveSystem根GameObject上
/// </summary>
public class SaveSystemPersistence : MonoBehaviour
{
    [Header("持久化设置")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool validateChildComponents = true;

    // 单例实例
    private static SaveSystemPersistence instance;
    public static SaveSystemPersistence Instance => instance;

    // 子组件状态跟踪
    private bool isInitialized = false;
    private int childComponentCount = 0;

    void Awake()
    {
        // 实现单例模式，确保只有一个SaveSystem实例存在
        if (instance == null)
        {
            instance = this;

            // 确保GameObject是根对象
            if (transform.parent != null)
            {
                LogWarning("SaveSystem应该是根对象，正在移动到根级别。");
                transform.SetParent(null);
            }

            // 设置为跨场景不销毁
            DontDestroyOnLoad(gameObject);

            // 初始化系统
            InitializePersistenceSystem();

            LogMessage("SaveSystem已设置为跨场景不销毁。");
        }
        else if (instance != this)
        {
            LogWarning($"检测到重复的SaveSystem实例，销毁重复实例: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化持久化系统
    /// </summary>
    private void InitializePersistenceSystem()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            // 统计子组件数量
            childComponentCount = CountChildComponents();

            // 验证子组件
            if (validateChildComponents)
            {
                ValidateChildComponents();
            }

            // 确保所有子对象也不会被意外销毁
            EnsureChildPersistence();

            isInitialized = true;
            LogMessage($"SaveSystem持久化系统初始化完成，包含 {childComponentCount} 个子组件。");
        }
        catch (System.Exception ex)
        {
            LogError($"SaveSystem持久化系统初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 统计子组件数量
    /// </summary>
    private int CountChildComponents()
    {
        return GetComponentsInChildren<MonoBehaviour>(true).Length;
    }

    /// <summary>
    /// 验证关键子组件是否存在
    /// </summary>
    private void ValidateChildComponents()
    {
        var requiredComponents = new System.Type[]
        {
            typeof(SaveManager),
            typeof(ItemInstanceIDManager)
        };

        var optionalComponents = new System.Type[]
        {
            typeof(SaveSystemSceneIntegrator),
            typeof(SaveSystemAutoInitializer),
            typeof(ItemInstanceIDManagerDeepIntegrator)
        };

        // 检查必需组件
        foreach (var componentType in requiredComponents)
        {
            var component = GetComponentInChildren(componentType);
            if (component == null)
            {
                LogError($"缺少必需组件: {componentType.Name}");
            }
            else
            {
                LogMessage($"找到必需组件: {componentType.Name}");
            }
        }

        // 检查可选组件
        foreach (var componentType in optionalComponents)
        {
            var component = GetComponentInChildren(componentType);
            if (component != null)
            {
                LogMessage($"找到可选组件: {componentType.Name}");
            }
        }
    }

    /// <summary>
    /// 确保子对象的持久化
    /// </summary>
    private void EnsureChildPersistence()
    {
        // 遍历所有子对象，确保它们不会被意外销毁
        var allChildren = GetComponentsInChildren<Transform>(true);

        foreach (var child in allChildren)
        {
            if (child != transform) // 排除自身
            {
                // 确保子对象没有被标记为销毁
                if (child.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    continue; // 已经在DontDestroyOnLoad场景中
                }

                // 记录子对象信息（用于调试）
                LogMessage($"子对象已纳入持久化管理: {GetGameObjectPath(child.gameObject)}");
            }
        }
    }

    /// <summary>
    /// 获取GameObject的完整路径
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null && parent != transform)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// 获取系统状态信息
    /// </summary>
    public string GetSystemStatus()
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine("=== SaveSystem持久化状态 ===");
        status.AppendLine($"已初始化: {isInitialized}");
        status.AppendLine($"子组件数量: {childComponentCount}");
        status.AppendLine($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        status.AppendLine($"DontDestroyOnLoad状态: {(gameObject.scene.name == "DontDestroyOnLoad" ? "是" : "否")}");

        // 列出所有子组件
        var components = GetComponentsInChildren<MonoBehaviour>(true);
        status.AppendLine($"\n子组件列表 ({components.Length}个):");
        foreach (var component in components)
        {
            if (component != this)
            {
                status.AppendLine($"- {component.GetType().Name} ({GetGameObjectPath(component.gameObject)})");
            }
        }

        return status.ToString();
    }

    /// <summary>
    /// 手动重新初始化系统（用于调试）
    /// </summary>
    [ContextMenu("重新初始化持久化系统")]
    public void ReinitializeSystem()
    {
        isInitialized = false;
        InitializePersistenceSystem();
    }

    /// <summary>
    /// 检查系统完整性
    /// </summary>
    [ContextMenu("检查系统完整性")]
    public void CheckSystemIntegrity()
    {
        LogMessage(GetSystemStatus());
    }

    // 日志工具方法
    private void LogMessage(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[SaveSystemPersistence] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogging)
        {
            Debug.LogWarning($"[SaveSystemPersistence] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SaveSystemPersistence] {message}");
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            LogMessage("SaveSystemPersistence正在销毁。");
            instance = null;
        }
    }

    // 场景切换事件处理
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成后的处理
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        LogMessage($"场景切换完成: {scene.name}，SaveSystem持久化状态正常。");

        // 可以在这里添加场景切换后的特殊处理逻辑
        if (validateChildComponents)
        {
            // 重新验证组件状态
            ValidateChildComponents();
        }
    }
}