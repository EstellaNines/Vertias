using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapSceneConfig
{
    [Header("地图配置")]
    public int mapId;
    public string mapName;
    public string sceneName;
    public bool isUnlocked = false;
}

public class MapSceneIntegrator : MonoBehaviour
{
    public static MapSceneIntegrator Instance { get; private set; }
    [Header("场景切换系统")]
    [Tooltip("场景切换控制器引用")]
    public SceneTransitionController sceneTransitionController;

    [Header("地图UI系统")]
    [Tooltip("地图按钮管理器引用")]
    public MapButtonManager mapButtonManager;

    [Tooltip("地图显示控制器引用")]
    public MapDisplayController mapDisplayController;

    [Header("地图场景配置")]
    [Tooltip("地图与场景的映射配置")]
    public List<MapSceneConfig> mapSceneConfigs = new List<MapSceneConfig>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // 不用Instance，直接FindObjectOfType
        if (sceneTransitionController == null)
            sceneTransitionController = FindObjectOfType<SceneTransitionController>();
        if (mapButtonManager == null)
            mapButtonManager = FindObjectOfType<MapButtonManager>();
        if (mapDisplayController == null)
            mapDisplayController = FindObjectOfType<MapDisplayController>();
        InitializeIntegration();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    private void InitializeIntegration()
    {
        if (mapButtonManager == null || mapDisplayController == null || sceneTransitionController == null)
        {
            Debug.LogError("[MapSceneIntegrator] 缺少必要的组件引用");
            return;
        }

        // 为地图显示控制器设置场景切换控制器
        mapDisplayController.SetSceneTransitionController(sceneTransitionController);

        // 配置地图场景映射
        ConfigureMapSceneMappings();

        // 配置地图按钮
        ConfigureMapButtons();
    }

    private void ConfigureMapSceneMappings()
    {
        var mappings = new List<MapDisplayController.MapSceneMapping>();

        foreach (var config in mapSceneConfigs)
        {
            mappings.Add(new MapDisplayController.MapSceneMapping
            {
                mapId = config.mapId,
                sceneName = config.sceneName,
                displayName = config.mapName
            });
        }

        mapDisplayController.mapSceneMappings = mappings;

    }

    private void ConfigureMapButtons()
    {
        var buttons = mapButtonManager.buttons;

        for (int i = 0; i < buttons.Count && i < mapSceneConfigs.Count; i++)
        {
            if (buttons[i] != null)
            {
                var config = mapSceneConfigs[i];

                // 设置地图ID
                buttons[i].SetMapId(config.mapId);

                // 设置解锁状态
                buttons[i].SetUnlockedState(config.isUnlocked);

                Debug.Log($"配置地图按钮: {config.mapName} (ID: {config.mapId})");
            }
        }
    }

    // 公共方法：解锁地图
    public void UnlockMap(int mapId)
    {
        foreach (var config in mapSceneConfigs)
        {
            if (config.mapId == mapId)
            {
                config.isUnlocked = true;

                // 更新对应的按钮状态
                var button = mapButtonManager.FindButtonByMapId(mapId);
                if (button != null)
                {
                    button.SetUnlockedState(true);
                }

                Debug.Log($"解锁地图: {config.mapName}");
                break;
            }
        }
    }

    // 公共方法：锁定地图
    public void LockMap(int mapId)
    {
        foreach (var config in mapSceneConfigs)
        {
            if (config.mapId == mapId)
            {
                config.isUnlocked = false;

                // 更新对应的按钮状态
                var button = mapButtonManager.FindButtonByMapId(mapId);
                if (button != null)
                {
                    button.SetUnlockedState(false);
                }

                Debug.Log($"锁定地图: {config.mapName}");
                break;
            }
        }
    }

    // 公共方法：直接切换到指定地图场景
    public void SwitchToMapScene(int mapId)
    {
        foreach (var config in mapSceneConfigs)
        {
            if (config.mapId == mapId && config.isUnlocked)
            {
                if (sceneTransitionController != null)
                {
                    sceneTransitionController.SwitchSceneByButton(config.sceneName);
                }
                return;
            }
        }

        Debug.LogWarning($"无法切换到地图场景，地图ID: {mapId} 可能未解锁或不存在");
    }
}
