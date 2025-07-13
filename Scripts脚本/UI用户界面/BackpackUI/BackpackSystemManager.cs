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
    [SerializeField] private string missionDataFileName = "MissionData"; // JSON文件名
    
    [Header("玩家控制器引用")]
    [Tooltip("玩家输入控制器引用")]
    [SerializeField] private PlayerInputController playerInputController;

    // 单例实例
    private static BackpackSystemManager instance;
    public static BackpackSystemManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 尝试在场景中查找现有实例
                instance = FindObjectOfType<BackpackSystemManager>();

                if (instance == null)
                {
                    // 如果没有找到，创建新的实例
                    GameObject go = new GameObject("BackpackSystemManager");
                    instance = go.AddComponent<BackpackSystemManager>();
                }
            }
            return instance;
        }
    }

    // 背包系统实例引用
    private GameObject currentBackpackSystem;
    private BackpackState backpackState;
    private MissionManager missionManager;

    // 持久化数据
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

            // 监听场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 加载持久化数据
            LoadPersistentMissionData();

            Debug.Log("BackpackSystemManager: 单例实例已创建并设置为跨场景存在");
        }
        else if (instance != this)
        {
            // 如果已经存在实例，销毁当前对象
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 清理事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 保存持久化数据
        SavePersistentMissionData();
    }

    // 场景加载时的回调
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"BackpackSystemManager: 场景 {scene.name} 已加载，重新初始化背包系统");

        // 延迟初始化，确保场景完全加载
        StartCoroutine(InitializeBackpackSystemDelayed());
    }

    // 延迟初始化背包系统
    private IEnumerator InitializeBackpackSystemDelayed()
    {
        yield return new WaitForEndOfFrame();

        // 获取玩家输入控制器
        PlayerInputController targetPlayerController = GetPlayerInputController();

        if (targetPlayerController != null)
        {
            InitializeBackpackSystem(targetPlayerController);
        }
        else
        {
            Debug.LogWarning("BackpackSystemManager: 在当前场景中未找到PlayerInputController，将在下一帧重试");
            yield return new WaitForSeconds(0.1f);

            targetPlayerController = GetPlayerInputController();
            if (targetPlayerController != null)
            {
                InitializeBackpackSystem(targetPlayerController);
            }
            else
            {
                Debug.LogError("BackpackSystemManager: 无法找到PlayerInputController，背包系统初始化失败");
            }
        }
    }

    // 获取玩家输入控制器（优先使用检查器引用，否则动态查找）
    private PlayerInputController GetPlayerInputController()
    {
        // 如果在检查器中设置了引用，优先使用
        if (playerInputController != null)
        {
            Debug.Log("BackpackSystemManager: 使用检查器中设置的PlayerInputController引用");
            return playerInputController;
        }

        // 否则尝试动态查找
        PlayerInputController foundController = FindObjectOfType<PlayerInputController>();
        if (foundController != null)
        {
            Debug.Log("BackpackSystemManager: 通过动态查找获取PlayerInputController");
            return foundController;
        }

        return null;
    }

    // 初始化背包系统
    private void InitializeBackpackSystem(PlayerInputController targetPlayerController)
    {
        // 如果已经存在背包系统，先销毁
        if (currentBackpackSystem != null)
        {
            Destroy(currentBackpackSystem);
        }

        // 实例化背包系统预制体
        if (backpackSystemPrefab != null)
        {
            currentBackpackSystem = Instantiate(backpackSystemPrefab);
            DontDestroyOnLoad(currentBackpackSystem);

            // 获取组件引用
            backpackState = currentBackpackSystem.GetComponentInChildren<BackpackState>();
            missionManager = currentBackpackSystem.GetComponentInChildren<MissionManager>();

            // 设置玩家输入控制器引用
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
            Debug.LogError("BackpackSystemManager: 背包系统预制体未设置！");
        }
    }

    // 通过反射设置PlayerInputController引用
    private void SetPlayerInputController(BackpackState backpackState, PlayerInputController targetPlayerController)
    {
        // 直接使用公共方法设置，不需要反射
        backpackState.SetPlayerInputController(targetPlayerController);
        Debug.Log("BackpackSystemManager: PlayerInputController引用已设置");
    }

    // 加载持久化任务数据
    private void LoadPersistentMissionData()
    {
        try
        {
            // 首先尝试从持久化路径加载
            if (File.Exists(persistentDataPath))
            {
                string jsonContent = File.ReadAllText(persistentDataPath);
                persistentMissionData = JsonUtility.FromJson<MissionDataCollection>(jsonContent);
                Debug.Log($"BackpackSystemManager: 从持久化路径加载任务数据成功，共 {persistentMissionData.missions.Count} 个任务");
            }
            else
            {
                // 如果持久化文件不存在，从Resources加载并创建持久化副本
                TextAsset jsonFile = Resources.Load<TextAsset>(missionDataFileName);
                if (jsonFile != null)
                {
                    persistentMissionData = JsonUtility.FromJson<MissionDataCollection>(jsonFile.text);

                    // 创建持久化副本
                    SavePersistentMissionData();

                    Debug.Log($"BackpackSystemManager: 从Resources加载任务数据并创建持久化副本，共 {persistentMissionData.missions.Count} 个任务");
                }
                else
                {
                    Debug.LogError($"BackpackSystemManager: 无法找到任务数据文件 {missionDataFileName}.json");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"BackpackSystemManager: 加载持久化任务数据时发生错误: {e.Message}");
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
                Debug.LogError($"BackpackSystemManager: 保存持久化任务数据时发生错误: {e.Message}");
            }
        }
    }

    // 将持久化数据加载到MissionManager
    private void LoadMissionDataToManager()
    {
        if (missionManager != null && persistentMissionData != null)
        {
            // 通过反射设置MissionManager的数据
            var dataField = typeof(MissionManager).GetField("missionDataCollection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    
            var dictField = typeof(MissionManager).GetField("missionDataDict",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    
            if (dataField != null && dictField != null)
            {
                dataField.SetValue(missionManager, persistentMissionData);
    
                // 重建字典
                var dict = new Dictionary<int, MissionData>();
                foreach (MissionData mission in persistentMissionData.missions)
                {
                    dict[mission.id] = mission;
                }
                dictField.SetValue(missionManager, dict);
    
                // 重新生成任务条以确保UI同步
                var generateMethod = typeof(MissionManager).GetMethod("GenerateMissionItems",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (generateMethod != null)
                {
                    generateMethod.Invoke(missionManager, null);
                    Debug.Log("BackpackSystemManager: 已重新生成任务条以同步持久化数据");
                }
    
                Debug.Log("BackpackSystemManager: 持久化任务数据已加载到MissionManager");
            }
        }
    }

    // 公共方法：手动设置玩家输入控制器
    public void SetPlayerInputController(PlayerInputController controller)
    {
        playerInputController = controller;
        Debug.Log("BackpackSystemManager: 手动设置PlayerInputController引用");
        
        // 如果背包系统已经初始化，重新设置引用
        if (backpackState != null && controller != null)
        {
            SetPlayerInputController(backpackState, controller);
        }
    }

    // 公共方法：获取当前任务数据
    public MissionDataCollection GetMissionData()
    {
        return persistentMissionData;
    }

    // 公共方法：更新任务数据
    public void UpdateMissionData(MissionDataCollection newData)
    {
        persistentMissionData = newData;
        SavePersistentMissionData();

        // 如果MissionManager存在，同步更新
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

        // 同步到MissionManager
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
}
