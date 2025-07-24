using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

[System.Serializable]
public class MapData
{
    public int id;
    public string name;
    public string thumbnail;
    public string description;
    public string[] items;
    public string difficulty;
    public string lootLevel;
    public string enemyCount;
}

[System.Serializable]
public class MapDataCollection
{
    public MapData[] Map;
}

public class MapDisplayController : MonoBehaviour
{
    [Header("地图信息展示")]
    [FieldLabel("地图名称文本")] public TextMeshProUGUI mapNameText;
    [FieldLabel("地图描述文本")] public TextMeshProUGUI mapDescriptionText;
    [FieldLabel("地图缩略图")] public Image mapThumbnailImage;
    [FieldLabel("描述滚动区域")] public ScrollRect descriptionScrollView;

    [Header("难度/掉落/敌人")]
    [FieldLabel("难度文本")] public TextMeshProUGUI difficultyText;
    [FieldLabel("掉落等级文本")] public TextMeshProUGUI lootLevelText;
    [FieldLabel("敌人数量文本")] public TextMeshProUGUI enemyCountText;

    // 新增：图标引用
    [Header("图标显示")]
    [FieldLabel("难度图标")] public Image difficultyIcon;
    [FieldLabel("掉落等级图标")] public Image lootLevelIcon;
    [FieldLabel("敌人数量图标")] public Image enemyCountIcon;
    [FieldLabel("图标容器")] public GameObject iconContainer; // 包含所有图标的容器

    // 新增：确认按钮相关
    [Header("确认按钮")]
    [FieldLabel("确认按钮图片")] public Image confirmButton; // 改为Image组件
    [FieldLabel("确认按钮文本")] public TextMeshProUGUI confirmButtonText; // 新增：确认按钮文本组件
    [FieldLabel("解锁状态精灵(第一个)")] public Sprite unlockedSprite;
    [FieldLabel("锁定状态精灵(第二个)")] public Sprite lockedSprite;
    [FieldLabel("悬停/按下精灵(第三个)")] public Sprite hoverPressedSprite;

    [Header("确认按钮颜色配置")]
    [FieldLabel("解锁状态文本颜色")] public Color unlockedTextColor = Color.white;
    [FieldLabel("锁定状态文本颜色")] public Color lockedTextColor = Color.gray;
    [FieldLabel("悬停/按下文本颜色")] public Color hoverPressedTextColor = Color.yellow;
    [Header("默认值")]
    [FieldLabel("默认地图名称")] public string defaultMapName = "未知地图";
    [FieldLabel("默认地图描述")] public string defaultMapDescription = "暂无描述";
    [FieldLabel("默认缩略图")] public Sprite defaultThumbnail;
    [FieldLabel("默认难度")] public string defaultDifficulty = "未知";
    [FieldLabel("默认掉落等级")] public string defaultLootLevel = "未知";
    [FieldLabel("默认敌人数量")] public string defaultEnemyCount = "未知";

    [Header("拖拽灵敏度")]
    [FieldLabel("拖拽灵敏度")] public float dragSensitivity = 2f;

    private MapDataCollection mapDataCollection;
    private Dictionary<int, MapData> mapDataDict = new Dictionary<int, MapData>();
    private bool isMouseOverScrollArea = false;
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private RectTransform contentRectTransform;
    private Vector2 initialContentPosition;
    private Sprite originalConfirmSprite; // 存储确认按钮的原始精灵
    private int currentMapId = -1; // 当前显示的地图ID
    private bool currentMapUnlocked = false; // 当前地图的解锁状态

    [Header("场景切换集成")]
    [FieldLabel("场景切换控制器")] public SceneTransitionController sceneTransitionController;
    [FieldLabel("地图ID到场景名称映射")] public List<MapSceneMapping> mapSceneMappings = new List<MapSceneMapping>();

    [System.Serializable]
    public class MapSceneMapping
    {
        public int mapId;
        public string sceneName;
        public string displayName;
    }

    private void Awake()
    {
        LoadMapData();
        ShowDefaultContent();
        SetupScrollAreaDetection();
        HideAllIcons();
        InitializeConfirmButton();
        if (sceneTransitionController == null)
        {
            sceneTransitionController = FindObjectOfType<SceneTransitionController>();
        }
        if (descriptionScrollView != null && descriptionScrollView.content != null)
        {
            contentRectTransform = descriptionScrollView.content;
            initialContentPosition = contentRectTransform.anchoredPosition;
        }
    }

    // 初始化确认按钮
    private void InitializeConfirmButton()
    {
        if (confirmButton != null)
        {
            // 存储原始精灵
            originalConfirmSprite = confirmButton.sprite;

            // 添加点击事件监听器
            Button buttonComponent = confirmButton.GetComponent<Button>();
            if (buttonComponent == null)
            {
                buttonComponent = confirmButton.gameObject.AddComponent<Button>();
            }

            // 清除现有监听器并添加新的
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.onClick.AddListener(OnConfirmButtonClick);

            // 初始时隐藏确认按钮
            confirmButton.gameObject.SetActive(false);
        }
    }

    // 隐藏所有图标
    private void HideAllIcons()
    {
        if (iconContainer != null)
        {
            iconContainer.SetActive(false);
        }
        else
        {
            // 如果没有容器，单独隐藏每个图标
            if (difficultyIcon != null) difficultyIcon.gameObject.SetActive(false);
            if (lootLevelIcon != null) lootLevelIcon.gameObject.SetActive(false);
            if (enemyCountIcon != null) enemyCountIcon.gameObject.SetActive(false);
        }
    }

    // 显示所有图标
    private void ShowAllIcons()
    {
        if (iconContainer != null)
        {
            iconContainer.SetActive(true);
        }
        else
        {
            // 如果没有容器，单独显示每个图标
            if (difficultyIcon != null) difficultyIcon.gameObject.SetActive(true);
            if (lootLevelIcon != null) lootLevelIcon.gameObject.SetActive(true);
            if (enemyCountIcon != null) enemyCountIcon.gameObject.SetActive(true);
        }
    }

    // 修正确认按钮点击事件 - 这里才是真正的场景切换逻辑
    private void OnConfirmButtonClick()
    {
        if (currentMapUnlocked)
        {
            Debug.Log($"确认进入已解锁地图: {currentMapId}");

            // 通过场景切换控制器切换场景
            string sceneName = GetSceneNameByMapId(currentMapId);
            if (!string.IsNullOrEmpty(sceneName) && sceneTransitionController != null)
            {
                Debug.Log($"CheckButton确认切换到场景: {sceneName}");
                sceneTransitionController.SwitchSceneByButton(sceneName);
            }
            else
            {
                Debug.LogWarning($"未找到地图ID {currentMapId} 对应的场景名称或场景切换控制器未设置");
            }
        }
        else
        {
            Debug.Log($"尝试进入未解锁地图: {currentMapId} - 显示解锁提示");
            // 这里可以添加提示玩家地图未解锁的UI逻辑
            ShowMapLockedMessage();
        }
    }

    // 新增：显示地图锁定提示
    private void ShowMapLockedMessage()
    {
        // 可以在这里添加提示UI，比如弹窗或者文本提示
        Debug.Log("地图未解锁，无法进入！");
        // 示例：可以播放一个提示音效或显示提示文本
    }

    // 根据地图ID获取场景名称
    private string GetSceneNameByMapId(int mapId)
    {
        foreach (var mapping in mapSceneMappings)
        {
            if (mapping.mapId == mapId)
            {
                return mapping.sceneName;
            }
        }
        Debug.LogWarning($"未找到地图ID {mapId} 的场景名称，请检查MapSceneIntegrator配置");
        return string.Empty;
    }

    // 显示地图信息时更新当前选中的地图状态
    public void DisplayMapInfo(int mapId)
    {
        currentMapId = mapId;

        // 查找地图数据
        if (mapDataDict.ContainsKey(mapId))
        {
            MapData mapData = mapDataDict[mapId];
            currentMapUnlocked = IsMapUnlocked(mapId);

            // 更新UI显示
            UpdateMapInfoDisplay(mapData);

            // 显示图标
            ShowAllIcons();

            // 更新确认按钮状态
            UpdateConfirmButtonState();

            // 显示确认按钮
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
            }

            // 重置滚动位置
            if (contentRectTransform != null)
            {
                contentRectTransform.anchoredPosition = new Vector2(contentRectTransform.anchoredPosition.x, 0);
            }

            Debug.Log($"展示地图信息: {mapData.name}, 解锁状态: {currentMapUnlocked}");
        }
        else
        {
            Debug.LogWarning($"未找到地图ID {mapId} 的数据");
            ShowDefaultContent();
        }
    }

    // 新增：检查地图是否解锁
    private bool IsMapUnlocked(int mapId)
    {
        // 这里可以从MapButtonManager或其他地方获取解锁状态
        MapButtonManager buttonManager = FindObjectOfType<MapButtonManager>();
        if (buttonManager != null)
        {
            MapButton button = buttonManager.FindButtonByMapId(mapId);
            if (button != null)
            {
                return button.IsUnlocked();
            }
        }
        return false; // 默认锁定
    }

    // 新增：更新地图信息显示
    private void UpdateMapInfoDisplay(MapData mapData)
    {
        // 更新地图名称
        if (mapNameText != null)
        {
            mapNameText.text = mapData.name;
        }

        // 更新地图描述
        if (mapDescriptionText != null)
        {
            mapDescriptionText.text = mapData.description;
        }

        // 更新缩略图
        if (mapThumbnailImage != null && !string.IsNullOrEmpty(mapData.thumbnail))
        {
            // 加载缩略图逻辑
            StartCoroutine(LoadThumbnail(mapData.thumbnail));
        }

        // 更新其他信息
        if (difficultyText != null)
        {
            difficultyText.text = mapData.difficulty;
        }

        if (lootLevelText != null)
        {
            lootLevelText.text = mapData.lootLevel;
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = mapData.enemyCount;
        }
    }

    // 新增：更新确认按钮状态
    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null) return;

        if (currentMapUnlocked)
        {
            // 解锁状态：使用解锁精灵和颜色
            if (unlockedSprite != null)
            {
                confirmButton.sprite = unlockedSprite;
            }
            if (confirmButtonText != null)
            {
                confirmButtonText.text = "Confirm";
                confirmButtonText.color = unlockedTextColor;
            }
        }
        else
        {
            // 锁定状态：使用锁定精灵和颜色
            if (lockedSprite != null)
            {
                confirmButton.sprite = lockedSprite;
            }
            if (confirmButtonText != null)
            {
                confirmButtonText.text = "Locked";
                confirmButtonText.color = lockedTextColor;
            }
        }
    }

    // 公共方法：设置场景切换控制器
    public void SetSceneTransitionController(SceneTransitionController controller)
    {
        sceneTransitionController = controller;
    }

    private void SetupScrollAreaDetection()
    {
        if (descriptionScrollView != null)
        {
            Debug.Log("开始设置滚动区域监听");

            EventTrigger eventTrigger = descriptionScrollView.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = descriptionScrollView.gameObject.AddComponent<EventTrigger>();
                Debug.Log("手动添加 EventTrigger");
            }

            eventTrigger.triggers.Clear();

            // 鼠标进入
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) =>
            {
                isMouseOverScrollArea = true;
                Debug.Log("鼠标进入滚动区域");
            });
            eventTrigger.triggers.Add(pointerEnter);

            // 鼠标离开
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) =>
            {
                isMouseOverScrollArea = false;
                isDragging = false;
                Debug.Log("鼠标离开滚动区域");
            });
            eventTrigger.triggers.Add(pointerExit);

            // 鼠标按下
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) =>
            {
                if (isMouseOverScrollArea)
                {
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                    Debug.Log("开始拖拽");
                }
            });
            eventTrigger.triggers.Add(pointerDown);

            // 鼠标抬起
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) =>
            {
                isDragging = false;
                Debug.Log("结束拖拽");
            });
            eventTrigger.triggers.Add(pointerUp);
        }
        else
        {
            Debug.LogError("未找到 ScrollRect 组件，无法设置滚动监听");
        }
    }

    private void Update()
    {
        if (isDragging && isMouseOverScrollArea && contentRectTransform != null)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 mouseDelta = currentMousePosition - lastMousePosition;

            float verticalDelta = mouseDelta.y;
            if (Mathf.Abs(verticalDelta) > 0.1f)
            {
                Vector2 currentPos = contentRectTransform.anchoredPosition;
                float newY = currentPos.y + verticalDelta * dragSensitivity;

                float contentHeight = contentRectTransform.rect.height;
                float viewportHeight = descriptionScrollView.viewport.rect.height;
                float maxY = Mathf.Max(0, contentHeight - viewportHeight);
                newY = Mathf.Clamp(newY, 0, maxY);

                contentRectTransform.anchoredPosition = new Vector2(currentPos.x, newY);
                Debug.Log($"拖拽中: delta={verticalDelta}, 新Y={newY}");
            }

            lastMousePosition = currentMousePosition;
        }

        if (!Input.GetMouseButton(0))
            isDragging = false;
    }

    private void LoadMapData()
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("MapData");
            if (jsonFile != null)
            {
                string jsonContent = jsonFile.text;
                mapDataCollection = JsonUtility.FromJson<MapDataCollection>(jsonContent);

                mapDataDict.Clear();
                foreach (var mapData in mapDataCollection.Map)
                    mapDataDict[mapData.id] = mapData;

                Debug.Log($"成功加载 {mapDataCollection.Map.Length} 张地图数据");
            }
            else
            {
                Debug.LogError("未找到 MapData.json 文件");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载地图数据出错: {e.Message}");
        }
    }

    private void ShowDefaultContent()
    {
        if (mapNameText != null)
            mapNameText.text = defaultMapName;

        if (mapDescriptionText != null)
            mapDescriptionText.text = defaultMapDescription;

        if (mapThumbnailImage != null && defaultThumbnail != null)
            mapThumbnailImage.sprite = defaultThumbnail;

        if (difficultyText != null)
            difficultyText.text = defaultDifficulty;

        if (lootLevelText != null)
            lootLevelText.text = defaultLootLevel;

        if (enemyCountText != null)
            enemyCountText.text = defaultEnemyCount;

        // 显示默认内容时隐藏图标和确认按钮
        HideAllIcons();
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }

        currentMapId = -1;
        currentMapUnlocked = false;
    }

    // 加载缩略图的协程方法
    private IEnumerator LoadThumbnail(string thumbnailPath)
    {
        string resourcePath = thumbnailPath.Replace(".png", "");
        Sprite thumbnailSprite = Resources.Load<Sprite>(resourcePath);

        if (thumbnailSprite != null)
        {
            mapThumbnailImage.sprite = thumbnailSprite;
        }
        else
        {
            Debug.LogWarning($"未找到缩略图: {resourcePath}");
            if (defaultThumbnail != null)
                mapThumbnailImage.sprite = defaultThumbnail;
        }

        yield return null;
    }

    public void DisplayMapInfoByName(string mapName)
    {
        foreach (var mapData in mapDataCollection.Map)
        {
            if (mapData.name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
            {
                DisplayMapInfo(mapData.id);
                return;
            }
        }

        Debug.LogWarning($"未找到地图: {mapName}");
        ShowDefaultContent();
    }

    public MapData[] GetAllMapData()
    {
        return mapDataCollection?.Map;
    }

    public MapData GetMapData(int mapId)
    {
        return mapDataDict.ContainsKey(mapId) ? mapDataDict[mapId] : null;
    }

    public void ReloadMapData()
    {
        LoadMapData();
    }

    public MapData[] GetMapsByDifficulty(string difficulty)
    {
        List<MapData> result = new List<MapData>();
        if (mapDataCollection != null)
        {
            foreach (var mapData in mapDataCollection.Map)
            {
                if (mapData.difficulty == difficulty)
                    result.Add(mapData);
            }
        }
        return result.ToArray();
    }

    public bool MapHasItem(int mapId, string itemName)
    {
        if (mapDataDict.ContainsKey(mapId))
        {
            MapData mapData = mapDataDict[mapId];
            if (mapData.items != null)
            {
                foreach (string item in mapData.items)
                {
                    if (item.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        return false;
    }
}