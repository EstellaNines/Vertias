using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class BackpackSystemManager : MonoBehaviour
{
    [Header("背包系统设置")]
    [SerializeField] private GameObject backpackSystemPrefab; // 背包系统预制体
    [SerializeField] private string missionDataFileName = "MissionData"; // JSON任务数据文件名

    [Header("玩家输入控制器")]
    [Tooltip("用于绑定玩家输入控制器")]
    [SerializeField] private PlayerInputController playerInputController;

    [Header("背包网格管理器")]

    [SerializeField] private BackpackGridManager backpackGridManager; // 背包网格管理器

    // 移除了对InventoryController的引用，因为网格系统已被删除


    // 单例实例
    private static BackpackSystemManager instance;
    public static BackpackSystemManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 如果没有找到实例，则创建一个新的
                instance = FindObjectOfType<BackpackSystemManager>();

                if (instance == null)
                {
                    // 如果仍未找到，则创建一个新的GameObject并添加组件
                    GameObject go = new GameObject("BackpackSystemManager");
                    instance = go.AddComponent<BackpackSystemManager>();
                }
            }
            return instance;
        }
    }

    // 当前背包系统实例
    private GameObject currentBackpackSystem;
    private BackpackState backpackState;
    private MissionManager missionManager;

    // 持久化任务数据
    private MissionDataCollection persistentMissionData;
    private string persistentDataPath;

    private void Awake()
    {
        // 确保只有一个实例存在
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化持久化数据路径
            persistentDataPath = Path.Combine(Application.persistentDataPath, "MissionData.json");

            // 注册场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;

            LoadPersistentMissionData();

            Debug.Log("BackpackSystemManager: 单例实例已初始化并设置为DontDestroyOnLoad");
        }
        else if (instance != this)
        {
            // 如果已存在其他实例，则销毁当前游戏对象
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 注销场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 保存持久化任务数据
        SavePersistentMissionData();
    }

    // 场景加载完成后的回调
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"BackpackSystemManager: 场景 {scene.name} 已加载完成，开始初始化背包系统");

        // 延迟初始化以确保所有对象已加载
        StartCoroutine(InitializeBackpackSystemDelayed());
    }

    // 延迟初始化背包系统
    private IEnumerator InitializeBackpackSystemDelayed()
    {
        yield return new WaitForEndOfFrame();

        // 获取目标玩家输入控制器
        PlayerInputController targetPlayerController = GetPlayerInputController();

        if (targetPlayerController != null)
        {
            InitializeBackpackSystem(targetPlayerController);
        }
        else
        {
            Debug.LogWarning("BackpackSystemManager: 首次尝试找不到PlayerInputController，进行延迟重试");
            yield return new WaitForSeconds(0.1f);

            targetPlayerController = GetPlayerInputController();
            if (targetPlayerController != null)
            {
                InitializeBackpackSystem(targetPlayerController);
            }
            else
            {
                Debug.LogError("BackpackSystemManager: 最终找不到PlayerInputController，初始化失败");
            }
        }
    }

    // 获取玩家输入控制器，如果序列化字段为空则从场景中查找
    private PlayerInputController GetPlayerInputController()
    {
        // 如果序列化字段已设置，则直接返回
        if (playerInputController != null)
        {
            Debug.Log("BackpackSystemManager: 使用序列化字段中的PlayerInputController");
            return playerInputController;
        }

        // 否则从场景中查找
        PlayerInputController foundController = FindObjectOfType<PlayerInputController>();
        if (foundController != null)
        {
            Debug.Log("BackpackSystemManager: 通过查找获取到PlayerInputController");
            return foundController;
        }

        return null;
    }

    // 初始化背包系统
    private void InitializeBackpackSystem(PlayerInputController targetPlayerController)
    {
        // 如果已存在当前背包系统，则销毁它
        if (currentBackpackSystem != null)
        {
            Destroy(currentBackpackSystem);
        }

        // 实例化背包系统预制体
        if (backpackSystemPrefab != null)
        {
            currentBackpackSystem = Instantiate(backpackSystemPrefab);
            DontDestroyOnLoad(currentBackpackSystem);

            // 获取组件
            backpackState = currentBackpackSystem.GetComponentInChildren<BackpackState>();
            missionManager = currentBackpackSystem.GetComponentInChildren<MissionManager>();

            // 设置玩家输入控制器
            if (backpackState != null)
            {
                SetPlayerInputController(backpackState, targetPlayerController);
            }

            // 加载持久化任务数据到MissionManager
            if (missionManager != null && persistentMissionData != null)
            {
                LoadMissionDataToManager();
            }

            Debug.Log("BackpackSystemManager: 背包系统已成功初始化");
        }
        else
        {
            Debug.LogError("BackpackSystemManager: 背包系统预制体未设置");
        }

        // 获取背包管理器组件
        backpackGridManager = currentBackpackSystem.GetComponentInChildren<BackpackGridManager>();

        // 初始化背包系统
        if (backpackGridManager != null)
        {
            Debug.Log("BackpackSystemManager: 背包系统已初始化");
        }
    }

    // 设置玩家输入控制器到背包状态
    private void SetPlayerInputController(BackpackState backpackState, PlayerInputController targetPlayerController)
    {
        // 将目标玩家输入控制器设置到背包状态中
        backpackState.SetPlayerInputController(targetPlayerController);
        Debug.Log("BackpackSystemManager: PlayerInputController已设置");
    }

    // 加载持久化任务数据
    private void LoadPersistentMissionData()
    {
        try
        {
            // 检查持久化数据文件是否存在
            if (File.Exists(persistentDataPath))
            {
                string jsonContent = File.ReadAllText(persistentDataPath);
                persistentMissionData = JsonUtility.FromJson<MissionDataCollection>(jsonContent);
                Debug.Log($"BackpackSystemManager: 从持久化路径加载了 {persistentMissionData.missions.Count} 个任务数据");
            }
            else
            {
                // 如果持久化文件不存在，则从Resources加载默认任务数据
                TextAsset jsonFile = Resources.Load<TextAsset>(missionDataFileName);
                if (jsonFile != null)
                {
                    persistentMissionData = JsonUtility.FromJson<MissionDataCollection>(jsonFile.text);

                    // 保存默认数据到持久化路径
                    SavePersistentMissionData();

                    Debug.Log($"BackpackSystemManager: 从Resources加载并保存了 {persistentMissionData.missions.Count} 个任务数据");
                }
                else
                {
                    Debug.LogError($"BackpackSystemManager: 无法找到默认任务文件 {missionDataFileName}.json");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"BackpackSystemManager: 加载持久化任务数据时出错: {e.Message}");
        }
    }

    // 保存持久化任务数据
    private void SavePersistentMissionData()
    {
        if (persistentMissionData != null)
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(persistentMissionData, true);
                File.WriteAllText(persistentDataPath, jsonContent);
                Debug.Log("BackpackSystemManager: 持久化任务数据已保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"BackpackSystemManager: 保存持久化任务数据时出错: {e.Message}");
            }
        }
    }

    // 加载任务数据到MissionManager
    private void LoadMissionDataToManager()
    {
        if (missionManager != null && persistentMissionData != null)
        {
            // 使用反射设置MissionManager的私有字段
            var dataField = typeof(MissionManager).GetField("missionDataCollection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var dictField = typeof(MissionManager).GetField("missionDataDict",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (dataField != null && dictField != null)
            {
                dataField.SetValue(missionManager, persistentMissionData);

                // 创建任务数据字典
                Dictionary<int, MissionData> dict = new Dictionary<int, MissionData>();
                foreach (MissionData mission in persistentMissionData.missions)
                {
                    dict[mission.id] = mission;
                }
                dictField.SetValue(missionManager, dict);

                var generateMethod = typeof(MissionManager).GetMethod("GenerateMissionItems",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (generateMethod != null)
                {
                    generateMethod.Invoke(missionManager, null);
                    Debug.Log("BackpackSystemManager: 已调用GenerateMissionItems加载任务数据");
                }

                Debug.Log("BackpackSystemManager: 持久化任务数据已加载到MissionManager");
            }
        }
    }

    // 公共方法：设置玩家输入控制器
    public void SetPlayerInputController(PlayerInputController controller)
    {
        Debug.Log("BackpackSystemManager: 设置外部PlayerInputController");

        // 如果背包状态存在且控制器不为空，则设置它
        if (backpackState != null && controller != null)
        {
            SetPlayerInputController(backpackState, controller);
        }
    }

    // 公共方法：获取任务数据
    public MissionDataCollection GetMissionData()
    {
        return persistentMissionData;
    }

    // 公共方法：更新任务数据
    public void UpdateMissionData(MissionDataCollection newData)
    {
        persistentMissionData = newData;
        SavePersistentMissionData();

        // 如果MissionManager存在，则重新加载数据
        if (missionManager != null)
        {
            LoadMissionDataToManager();
        }
    }

    // 公共方法：添加新任务
    public void AddMission(MissionData newMission)
    {
        if (persistentMissionData == null)
        {
            persistentMissionData = new MissionDataCollection();
            persistentMissionData.missions = new List<MissionData>();
        }

        persistentMissionData.missions.Add(newMission);
        SavePersistentMissionData();

        // 更新MissionManager
        if (missionManager != null)
        {
            LoadMissionDataToManager();
        }
    }

    // 公共方法：获取背包状态
    public BackpackState GetBackpackState()
    {
        return backpackState;
    }

    // 公共方法：获取任务管理器
    public MissionManager GetMissionManager()
    {
        return missionManager;
    }

    public BackpackGridManager GetBackpackGridManager()
    {
        return backpackGridManager;
    }

    // GetInventoryController方法已移除，因为InventoryController组件已被删除
    // 如果需要背包功能，请使用GetBackpackGridManager()方法
}