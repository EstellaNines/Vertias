using System.Collections;
using UnityEngine;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存系统初始化器
    /// 负责在游戏启动时自动初始化保存系统
    /// </summary>
    public class SaveSystemInitializer : MonoBehaviour
    {
        [Header("保存系统配置")]
        [SerializeField] private bool autoInitializeOnStart = true; // 是否在Start时自动初始化
        [SerializeField] private bool enableAutoSave = true; // 是否启用自动保存
        [SerializeField] private float autoSaveInterval = 300f; // 自动保存间隔（秒）
        [SerializeField] private bool enableDebugLogging = true; // 是否启用调试日志

        [Header("初始化延迟设置")]
        [SerializeField] private float initializationDelay = 1f; // 初始化延迟时间
        [SerializeField] private bool waitForSceneLoad = true; // 是否等待场景完全加载

        private SaveManager saveManager;
        private bool isInitialized = false;

        /// <summary>
        /// Unity Start方法
        /// </summary>
        private void Start()
        {
            if (autoInitializeOnStart)
            {
                StartCoroutine(InitializeSaveSystemCoroutine());
            }
        }

        /// <summary>
        /// 初始化保存系统的协程
        /// </summary>
        private IEnumerator InitializeSaveSystemCoroutine()
        {
            // 等待场景完全加载
            if (waitForSceneLoad)
            {
                yield return new WaitForEndOfFrame();
            }

            // 等待指定的延迟时间
            if (initializationDelay > 0)
            {
                yield return new WaitForSeconds(initializationDelay);
            }

            // 初始化保存系统
            InitializeSaveSystem();
        }

        /// <summary>
        /// 手动初始化保存系统
        /// </summary>
        public void InitializeSaveSystem()
        {
            if (isInitialized)
            {
                LogWarning("保存系统已经初始化，跳过重复初始化");
                return;
            }

            try
            {
                // 获取或创建SaveManager实例
                saveManager = SaveManager.Instance;
                if (saveManager == null)
                {
                    LogError("无法获取SaveManager实例");
                    return;
                }

                // 初始化SaveManager
                saveManager.Initialize();

                // 配置自动保存
                if (enableAutoSave)
                {
                    saveManager.SetAutoSaveEnabled(true);
                    saveManager.SetAutoSaveInterval(autoSaveInterval);
                }

                // 验证ISaveable对象集成
                ValidateISaveableIntegration();

                isInitialized = true;
                LogMessage("保存系统初始化完成");

                // 触发初始化完成事件
                OnSaveSystemInitialized();
            }
            catch (System.Exception ex)
            {
                LogError($"保存系统初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证ISaveable对象集成
        /// </summary>
        private void ValidateISaveableIntegration()
        {
            // 查找场景中所有的ISaveable对象
            var saveableObjects = FindObjectsOfType<MonoBehaviour>();
            int saveableCount = 0;

            foreach (var obj in saveableObjects)
            {
                if (obj is ISaveable saveable)
                {
                    saveableCount++;

                    // 验证SaveID是否有效
                    string saveId = saveable.GetSaveID();
                    if (string.IsNullOrEmpty(saveId))
                    {
                        LogWarning($"发现无效的SaveID: {obj.name}");
                    }

                    // 验证是否已注册到SaveManager
                    if (!saveManager.IsObjectRegistered(saveable))
                    {
                        LogWarning($"ISaveable对象未注册到SaveManager: {obj.name}");
                    }
                }
            }

            LogMessage($"发现 {saveableCount} 个ISaveable对象");
        }

        /// <summary>
        /// 保存系统初始化完成事件
        /// </summary>
        protected virtual void OnSaveSystemInitialized()
        {
            // 子类可以重写此方法来处理初始化完成事件
            LogMessage("保存系统初始化完成事件触发");
        }

        /// <summary>
        /// 获取保存系统状态
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 获取SaveManager实例
        /// </summary>
        public SaveManager GetSaveManager() => saveManager;

        /// <summary>
        /// 设置自动保存配置
        /// </summary>
        public void SetAutoSaveConfig(bool enabled, float interval = 300f)
        {
            enableAutoSave = enabled;
            autoSaveInterval = interval;

            if (saveManager != null && isInitialized)
            {
                saveManager.SetAutoSaveEnabled(enabled);
                saveManager.SetAutoSaveInterval(interval);
            }
        }

        /// <summary>
        /// 强制重新初始化保存系统
        /// </summary>
        public void ForceReinitialize()
        {
            isInitialized = false;
            InitializeSaveSystem();
        }

        /// <summary>
        /// Unity应用程序暂停时的处理
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && saveManager != null && enableAutoSave)
            {
                // 应用程序暂停时自动保存
                saveManager.SaveGame("AutoSave_OnPause");
            }
        }

        /// <summary>
        /// Unity应用程序退出时的处理
        /// </summary>
        private void OnApplicationQuit()
        {
            if (saveManager != null && enableAutoSave)
            {
                // 应用程序退出时自动保存
                _ = saveManager.SaveGame("AutoSave_OnQuit");
            }
        }

        /// <summary>
        /// 日志输出方法
        /// </summary>
        private void LogMessage(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[SaveSystemInitializer] {message}");
            }
        }

        /// <summary>
        /// 警告日志输出方法
        /// </summary>
        private void LogWarning(string message)
        {
            if (enableDebugLogging)
            {
                Debug.LogWarning($"[SaveSystemInitializer] {message}");
            }
        }

        /// <summary>
        /// 错误日志输出方法
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[SaveSystemInitializer] {message}");
        }

        // ==================== 编辑器支持方法 ====================

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的验证方法
        /// </summary>
        private void OnValidate()
        {
            // 确保自动保存间隔不小于30秒
            if (autoSaveInterval < 30f)
            {
                autoSaveInterval = 30f;
            }

            // 确保初始化延迟不为负数
            if (initializationDelay < 0f)
            {
                initializationDelay = 0f;
            }
        }

        /// <summary>
        /// 编辑器中手动初始化保存系统
        /// </summary>
        [ContextMenu("手动初始化保存系统")]
        private void EditorInitializeSaveSystem()
        {
            if (Application.isPlaying)
            {
                ForceReinitialize();
            }
            else
            {
                Debug.Log("只能在运行时初始化保存系统");
            }
        }

        /// <summary>
        /// 编辑器中显示保存系统状态
        /// </summary>
        [ContextMenu("显示保存系统状态")]
        private void EditorShowSaveSystemStatus()
        {
            if (Application.isPlaying)
            {
                Debug.Log($"保存系统状态: {(isInitialized ? "已初始化" : "未初始化")}");
                if (saveManager != null)
                {
                    Debug.Log($"注册的ISaveable对象数量: {saveManager.RegisteredObjectCount}");
                }
            }
            else
            {
                Debug.Log("只能在运行时查看保存系统状态");
            }
        }
#endif
    }
}