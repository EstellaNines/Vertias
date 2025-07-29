using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GlobalMessaging;

// 地图ID传递消息
public struct MapIDSelectedMessage : IMessage
{
    public int MapID;
    public string MapName;
    public bool IsUnlocked;

    public MapIDSelectedMessage(int mapID, string mapName, bool isUnlocked)
    {
        MapID = mapID;
        MapName = mapName;
        IsUnlocked = isUnlocked;
    }
}

public class MapConfirmController : MonoBehaviour
{
    [Header("确认按钮精灵设置")]
    [SerializeField][FieldLabel("地图解锁精灵")] private Sprite unlockedSprite;     // 地图解锁时显示的精灵
    [SerializeField][FieldLabel("地图未解锁精灵")] private Sprite lockedSprite;       // 地图未解锁时显示的精灵
    [SerializeField][FieldLabel("悬停或点击精灵")] private Sprite hoverClickSprite;   // 悬停或点击时显示的精灵

    [Header("地图数据")]
    [SerializeField][FieldLabel("地图数据JSON文件")] private TextAsset mapDataJson;

    [Header("按钮组件")]
    [SerializeField] private Button confirmButton;

    private Dictionary<int, MapData> mapDataDict = new Dictionary<int, MapData>();
    private int currentMapID = -1;
    private Image buttonImage;
    private Sprite originalSprite;
    private bool isMapUnlocked = false;
    private bool isLoadingScene = false;  // 防止重复加载

    void Start()
    {
        LoadMapData();
        InitializeButton();
    }

    void OnEnable()
    {
        // 注册消息监听
        MessagingCenter.Instance.Register<MapIDSelectedMessage>(OnMapIDSelected);
        MessagingCenter.Instance.Register<SceneLoadCompleteMessage>(OnSceneLoadComplete);
        MessagingCenter.Instance.Register<SceneLoadStartMessage>(OnSceneLoadStart);

        // 重置加载状态，防止状态残留
        ResetLoadingState();
    }

    void OnDisable()
    {
        // 取消消息监听
        MessagingCenter.Instance.Unregister<MapIDSelectedMessage>(OnMapIDSelected);
        MessagingCenter.Instance.Unregister<SceneLoadCompleteMessage>(OnSceneLoadComplete);
        MessagingCenter.Instance.Unregister<SceneLoadStartMessage>(OnSceneLoadStart);
    }

    // 处理场景加载开始消息
    private void OnSceneLoadStart(SceneLoadStartMessage message)
    {
        isLoadingScene = true;
        Debug.Log($"MapConfirmController: 开始加载场景: {message.SceneName}");

        // 禁用确认按钮防止重复点击
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }

    // 处理场景加载完成消息
    private void OnSceneLoadComplete(SceneLoadCompleteMessage message)
    {
        isLoadingScene = false;
        Debug.Log($"MapConfirmController: 场景加载完成 - 场景: {message.SceneName}, 成功: {message.Success}");

        // 重新启用确认按钮
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }

        if (!message.Success)
        {
            Debug.LogError($"MapConfirmController: 场景 {message.SceneName} 加载失败");
        }
    }

    // 处理地图ID选择消息
    private void OnMapIDSelected(MapIDSelectedMessage message)
    {
        currentMapID = message.MapID;
        Debug.Log($"MapConfirmController: 通过MessagingCenter接收到地图ID: {message.MapID}, 名称: {message.MapName}, 解锁状态: {message.IsUnlocked}");

        // 检查地图是否解锁并更新按钮精灵
        UpdateButtonSprite();

        // 获取地图信息
        if (mapDataDict.ContainsKey(message.MapID))
        {
            MapData mapData = mapDataDict[message.MapID];
            isMapUnlocked = mapData.isUnlocked;
            Debug.Log($"MapConfirmController: 地图信息 - 名称: {mapData.name}, 解锁状态: {mapData.isUnlocked}, 场景名称: {mapData.sceneName}");
        }
        else
        {
            Debug.LogWarning($"MapConfirmController: 未找到ID为 {message.MapID} 的地图数据");
        }
    }

    // 加载地图数据
    // 加载地图数据
    private void LoadMapData()
    {
        if (mapDataJson != null)
        {
            try
            {
                MapDataCollection mapCollection = JsonUtility.FromJson<MapDataCollection>(mapDataJson.text);
                if (mapCollection != null && mapCollection.Map != null)
                {
                    mapDataDict.Clear();
                    foreach (MapData mapData in mapCollection.Map)
                    {
                        // 从PlayerPrefs加载解锁状态
                        string unlockKey = $"Map_{mapData.id}_Unlocked";
                        if (PlayerPrefs.HasKey(unlockKey))
                        {
                            mapData.isUnlocked = PlayerPrefs.GetInt(unlockKey) == 1;
                        }

                        mapDataDict[mapData.id] = mapData;
                    }
                    Debug.Log($"MapConfirmController: 成功加载 {mapDataDict.Count} 个地图数据");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapConfirmController: 加载地图数据失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("MapConfirmController: 未设置地图数据JSON文件");
        }
    }

    // 初始化按钮
    private void InitializeButton()
    {
        if (confirmButton != null)
        {
            buttonImage = confirmButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalSprite = buttonImage.sprite;
            }

            // 添加事件触发器
            EventTrigger eventTrigger = confirmButton.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = confirmButton.gameObject.AddComponent<EventTrigger>();
            }

            // 清除现有事件
            eventTrigger.triggers.Clear();

            // 添加鼠标进入事件
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnButtonHover(); });
            eventTrigger.triggers.Add(pointerEnter);

            // 添加鼠标离开事件
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnButtonExit(); });
            eventTrigger.triggers.Add(pointerExit);

            // 添加点击事件
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("MapConfirmController: 未设置确认按钮组件");
        }
    }

    // 更新按钮精灵
    private void UpdateButtonSprite()
    {
        if (buttonImage == null || currentMapID < 0) return;

        if (mapDataDict.ContainsKey(currentMapID))
        {
            MapData mapData = mapDataDict[currentMapID];

            if (mapData.isUnlocked)
            {
                // 地图已解锁，显示解锁精灵
                if (unlockedSprite != null)
                {
                    buttonImage.sprite = unlockedSprite;
                    originalSprite = unlockedSprite;
                }
            }
            else
            {
                // 地图未解锁，显示锁定精灵
                if (lockedSprite != null)
                {
                    buttonImage.sprite = lockedSprite;
                    originalSprite = lockedSprite;
                }
            }
        }
    }

    // 鼠标悬停事件
    private void OnButtonHover()
    {
        if (buttonImage != null && hoverClickSprite != null)
        {
            buttonImage.sprite = hoverClickSprite;
        }
    }

    // 鼠标离开事件
    private void OnButtonExit()
    {
        if (buttonImage != null && originalSprite != null)
        {
            buttonImage.sprite = originalSprite;
        }
    }

    // 按钮点击事件
    private void OnButtonClick()
    {
        if (buttonImage != null && hoverClickSprite != null)
        {
            buttonImage.sprite = hoverClickSprite;
        }

        // 防止在加载过程中重复点击
        if (isLoadingScene)
        {
            Debug.Log("MapConfirmController: 正在加载场景中，请稍候...");
            return;
        }

        if (currentMapID >= 0 && mapDataDict.ContainsKey(currentMapID))
        {
            MapData mapData = mapDataDict[currentMapID];
            Debug.Log($"MapConfirmController: 确认按钮被点击 - 地图ID: {currentMapID}, 解锁状态: {mapData.isUnlocked}");

            if (mapData.isUnlocked)
            {
                if (!string.IsNullOrEmpty(mapData.sceneName))
                {
                    Debug.Log($"MapConfirmController: 开始加载场景 - 场景名称: {mapData.sceneName}");

                    // 添加超时保护，防止状态永久锁定
                    StartCoroutine(LoadSceneWithTimeout(mapData.sceneName));
                }
                else
                {
                    Debug.LogWarning($"MapConfirmController: 地图 {mapData.name} 的场景名称为空，无法加载场景");
                }
            }
            else
            {
                Debug.Log("MapConfirmController: 地图未解锁，无法进入");
            }
        }
    }

    // 带超时保护的场景加载
    private IEnumerator LoadSceneWithTimeout(string sceneName)
    {
        // 使用SceneLoader异步加载场景
        SceneLoader.Instance.LoadSceneAsync(sceneName);

        // 设置超时保护（10秒后自动重置状态）
        float timeout = 10f;
        float elapsed = 0f;

        while (isLoadingScene && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 如果超时仍在加载状态，强制重置
        if (isLoadingScene && elapsed >= timeout)
        {
            Debug.LogWarning("MapConfirmController: 场景加载超时，强制重置状态");
            ResetLoadingState();
        }
    }

    // 重置加载状态
    private void ResetLoadingState()
    {
        isLoadingScene = false;
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        Debug.Log("MapConfirmController: 加载状态已重置");
    }

    // 公共方法：获取当前地图信息
    public MapData GetCurrentMapData()
    {
        if (currentMapID >= 0 && mapDataDict.ContainsKey(currentMapID))
        {
            return mapDataDict[currentMapID];
        }
        return null;
    }

    // 公共方法：获取当前地图ID
    public int GetCurrentMapID()
    {
        return currentMapID;
    }

    // 公共方法：检查当前地图是否解锁
    public bool IsCurrentMapUnlocked()
    {
        return isMapUnlocked;
    }

    // 公共方法：检查是否正在加载场景
    public bool IsLoadingScene()
    {
        return isLoadingScene;
    }
}
