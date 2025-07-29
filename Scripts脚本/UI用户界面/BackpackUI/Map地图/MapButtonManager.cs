using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using GlobalMessaging;

// 地图数据类
[System.Serializable]
public class MapData
{
    public int id;
    public string name;
    public string thumbnail;
    public string description;
    public string difficulty;
    public string lootLevel;
    public string enemyCount;
    public bool isUnlocked;
    public string sceneName;
}

// 地图数据集合类
[System.Serializable]
public class MapDataCollection
{
    public List<MapData> Map; // 注意：这里改为"Map"以匹配JSON
}

public class MapButtonManager : MonoBehaviour
{
    [Header("精灵图片设置")]
    [SerializeField] [FieldLabel("精灵图片2")]private Sprite sprite2;

    [Header("字体颜色设置")]
    [SerializeField] [FieldLabel("悬停字体颜色")]private Color hoverTextColor = Color.white;

    [Header("锁定图标设置")]
    [SerializeField] [FieldLabel("锁定图标默认精灵")]private Sprite lockIconDefault;     // 锁定图标默认精灵
    [SerializeField] [FieldLabel("锁定图标悬停精灵")]private Sprite lockIconHover;      // 锁定图标悬停精灵

    [Header("按钮列表")]
    [SerializeField] [FieldLabel("地图按钮列表")]private List<Button> mapButtons = new List<Button>();

    [Header("地图数据")]
    [SerializeField] [FieldLabel("地图数据JSON文件")]private TextAsset mapDataJson;

    private Dictionary<Button, Sprite> originalSprites = new Dictionary<Button, Sprite>();
    private Dictionary<Button, Color> originalTextColors = new Dictionary<Button, Color>();
    private Dictionary<Button, MapData> buttonMapData = new Dictionary<Button, MapData>();
    private Dictionary<int, MapData> mapDataDict = new Dictionary<int, MapData>(); // 按ID索引的地图数据
    private Dictionary<Button, Image> buttonLockIcons = new Dictionary<Button, Image>(); // 按钮锁定图标
    private Dictionary<Image, Sprite> originalLockSprites = new Dictionary<Image, Sprite>(); // 锁定图标原始精灵
    private List<MapData> mapDataList = new List<MapData>();

    void Start()
    {
        LoadMapData();
        InitializeButtons();
    }

    // 加载地图数据
    private void LoadMapData()
    {
        Debug.Log("开始加载地图数据...");

        if (mapDataJson != null)
        {
            Debug.Log($"JSON文件已设置: {mapDataJson.name}");

            try
            {
                MapDataCollection mapCollection = JsonUtility.FromJson<MapDataCollection>(mapDataJson.text);
                if (mapCollection != null && mapCollection.Map != null)
                {
                    mapDataList = mapCollection.Map;
                    Debug.Log($"成功加载 {mapDataList.Count} 个地图数据");

                    // 创建ID索引字典
                    mapDataDict.Clear();
                    foreach (MapData mapData in mapDataList)
                    {
                        mapDataDict[mapData.id] = mapData;
                        Debug.Log($"地图数据: ID={mapData.id}, 名称={mapData.name}, 场景={mapData.sceneName}, 解锁状态={mapData.isUnlocked}");
                    }
                }
                else
                {
                    Debug.LogError("地图数据为空或格式不正确");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载地图数据失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("未设置地图数据JSON文件");
        }
    }

    // 初始化所有按钮的事件监听
    private void InitializeButtons()
    {
        Debug.Log($"开始初始化按钮，按钮数量: {mapButtons.Count}, 地图数据数量: {mapDataList.Count}");

        foreach (Button button in mapButtons)
        {
            if (button != null)
            {
                // 获取或添加MapButtonID组件
                MapButtonID buttonID = button.GetComponent<MapButtonID>();
                if (buttonID == null)
                {
                    buttonID = button.gameObject.AddComponent<MapButtonID>();
                    Debug.LogWarning($"按钮 {button.name} 没有MapButtonID组件，已自动添加");
                }

                // 根据按钮ID关联地图数据
                int mapID = buttonID.MapID;
                if (mapID >= 0 && mapDataDict.ContainsKey(mapID))
                {
                    buttonMapData[button] = mapDataDict[mapID];
                    Debug.Log($"按钮 {button.name} (ID: {mapID}) 关联地图数据: {mapDataDict[mapID].name}");

                    // 更新按钮文本显示（从JSON自动读取地图名称）
                    UpdateButtonText(button, mapDataDict[mapID]);

                    // 设置锁定图标
                    SetupLockIcon(button, mapDataDict[mapID]);
                }
                else
                {
                    Debug.LogWarning($"按钮 {button.name} 的ID ({mapID}) 无效或未找到对应地图数据");
                }

                // 存储原始精灵图片和颜色
                SetupButtonEvents(button);
            }
        }

        Debug.Log($"按钮数据关联完成，关联数量: {buttonMapData.Count}");
    }

    // 设置锁定图标
    private void SetupLockIcon(Button button, MapData mapData)
    {
        // 查找按钮下的Image组件（锁定图标）
        Image[] childImages = button.GetComponentsInChildren<Image>();
        Image lockIcon = null;

        // 找到不是按钮本身的Image组件
        foreach (Image img in childImages)
        {
            if (img.gameObject != button.gameObject)
            {
                lockIcon = img;
                break;
            }
        }

        if (lockIcon != null)
        {
            buttonLockIcons[button] = lockIcon;

            // 设置锁定图标的显示状态
            if (mapData.isUnlocked)
            {
                // 地图已解锁，隐藏锁定图标
                lockIcon.gameObject.SetActive(false);
                Debug.Log($"地图 {mapData.name} 已解锁，隐藏锁定图标");
            }
            else
            {
                // 地图未解锁，显示锁定图标
                lockIcon.gameObject.SetActive(true);

                // 设置默认锁定图标精灵
                if (lockIconDefault != null)
                {
                    lockIcon.sprite = lockIconDefault;
                    originalLockSprites[lockIcon] = lockIconDefault;
                }

                Debug.Log($"地图 {mapData.name} 未解锁，显示锁定图标");
            }
        }
        else
        {
            Debug.LogWarning($"按钮 {button.name} 下未找到锁定图标Image组件");
        }
    }

    // 更新按钮文本显示ID和名称（从JSON自动读取）
    private void UpdateButtonText(Button button, MapData mapData)
    {
        if (button != null && mapData != null)
        {
            // 优先查找TextMeshPro组件
            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                // 保持原有文本不变
                string originalText = tmpText.text;
                tmpText.text = $"{originalText}";
                return;
            }
        }
    }

    // 设置按钮事件
    private void SetupButtonEvents(Button button)
    {
        // 存储原始精灵图片
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            originalSprites[button] = buttonImage.sprite;
        }

        // 存储原始字体颜色
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        Text normalText = button.GetComponentInChildren<Text>();

        if (tmpText != null)
        {
            originalTextColors[button] = tmpText.color;
        }
        else if (normalText != null)
        {
            originalTextColors[button] = normalText.color;
        }

        // 添加事件触发器
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // 清除现有事件
        eventTrigger.triggers.Clear();

        // 添加鼠标进入事件
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnButtonHover(button); });
        eventTrigger.triggers.Add(pointerEnter);

        // 添加鼠标离开事件
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonExit(button); });
        eventTrigger.triggers.Add(pointerExit);

        // 清除现有点击事件并添加新的
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnButtonClick(button));
    }

    // 鼠标悬停在按钮上时调用
    private void OnButtonHover(Button button)
    {
        ChangeButtonSprite(button, sprite2);
        ChangeButtonTextColor(button, hoverTextColor);

        // 更改锁定图标精灵
        if (buttonLockIcons.ContainsKey(button))
        {
            Image lockIcon = buttonLockIcons[button];
            if (lockIcon.gameObject.activeInHierarchy && lockIconHover != null)
            {
                lockIcon.sprite = lockIconHover;
            }
        }
    }

    private void OnButtonExit(Button button)
    {
        if (originalSprites.ContainsKey(button))
        {
            ChangeButtonSprite(button, originalSprites[button]);
        }

        if (originalTextColors.ContainsKey(button))
        {
            ChangeButtonTextColor(button, originalTextColors[button]);
        }

        // 恢复锁定图标原始精灵
        if (buttonLockIcons.ContainsKey(button))
        {
            Image lockIcon = buttonLockIcons[button];
            if (lockIcon.gameObject.activeInHierarchy && originalLockSprites.ContainsKey(lockIcon))
            {
                lockIcon.sprite = originalLockSprites[lockIcon];
            }
        }
    }

    private void OnButtonClick(Button button)
    {
        ChangeButtonSprite(button, sprite2);
        ChangeButtonTextColor(button, hoverTextColor);
        
        // 更改锁定图标精灵
        if (buttonLockIcons.ContainsKey(button))
        {
            Image lockIcon = buttonLockIcons[button];
            if (lockIcon.gameObject.activeInHierarchy && lockIconHover != null)
            {
                lockIcon.sprite = lockIconHover;
            }
        }

        if (buttonMapData.ContainsKey(button))
        {
            MapData mapData = buttonMapData[button];
            Debug.Log($"地图ID: {mapData.id}, 地图名称: {mapData.name}, 场景名称: {mapData.sceneName}");
            
            // 通知MapDescription显示地图信息
            MapDescription mapDescription = FindObjectOfType<MapDescription>();
            if (mapDescription != null)
            {
                mapDescription.ShowMapInfo(mapData.id);
            }
            
            // 使用MessagingCenter发送地图ID消息
            MapIDSelectedMessage message = new MapIDSelectedMessage(mapData.id, mapData.name, mapData.isUnlocked);
            MessagingCenter.Instance.Send(message);
            Debug.Log($"MapButtonManager: 已通过MessagingCenter发送地图ID {mapData.id} 消息");
        }
        else
        {
            Debug.Log($"按钮 {button.name} 被点击了！（无关联地图数据）");
        }
    }

    private void ChangeButtonSprite(Button button, Sprite newSprite)
    {
        if (button != null && newSprite != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = newSprite;
            }
        }
    }

    private void ChangeButtonTextColor(Button button, Color newColor)
    {
        if (button != null)
        {
            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.color = newColor;
                return;
            }

            Text normalText = button.GetComponentInChildren<Text>();
            if (normalText != null)
            {
                normalText.color = newColor;
            }
        }
    }

    // 公共方法：设置按钮ID（不修改显示文本）
    public void SetButtonMapID(Button button, int mapID)
    {
        if (button != null)
        {
            MapButtonID buttonIDComponent = button.GetComponent<MapButtonID>();
            if (buttonIDComponent == null)
            {
                buttonIDComponent = button.gameObject.AddComponent<MapButtonID>();
            }
            buttonIDComponent.MapID = mapID;

            // 重新关联数据并自动更新显示文本
            if (mapDataDict.ContainsKey(mapID))
            {
                buttonMapData[button] = mapDataDict[mapID];
                UpdateButtonText(button, mapDataDict[mapID]); // 自动从JSON读取名称
                SetupLockIcon(button, mapDataDict[mapID]); // 重新设置锁定图标
                Debug.Log($"按钮 {button.name} ID设置为: {mapID}, 地图名称: {mapDataDict[mapID].name}");
            }
        }
    }

    // 获取按钮的地图ID
    public int GetButtonMapID(Button button)
    {
        if (button != null)
        {
            MapButtonID buttonIDComponent = button.GetComponent<MapButtonID>();
            if (buttonIDComponent != null)
            {
                return buttonIDComponent.MapID;
            }
        }
        return -1;
    }

    // 获取按钮关联的地图数据
    public MapData GetButtonMapData(Button button)
    {
        if (buttonMapData.ContainsKey(button))
        {
            return buttonMapData[button];
        }
        return null;
    }

    // 根据地图ID获取地图数据
    public MapData GetMapDataByID(int mapID)
    {
        if (mapDataDict.ContainsKey(mapID))
        {
            return mapDataDict[mapID];
        }
        return null;
    }

    // 公共方法：更新地图解锁状态
    public void UpdateMapLockStatus(int mapID, bool isUnlocked)
    {
        if (mapDataDict.ContainsKey(mapID))
        {
            mapDataDict[mapID].isUnlocked = isUnlocked;
            
            // 保存到PlayerPrefs
            string unlockKey = $"Map_{mapID}_Unlocked";
            PlayerPrefs.SetInt(unlockKey, isUnlocked ? 1 : 0);
            PlayerPrefs.Save();

            // 找到对应的按钮并更新锁定图标
            foreach (var kvp in buttonMapData)
            {
                if (kvp.Value.id == mapID)
                {
                    SetupLockIcon(kvp.Key, kvp.Value);
                    break;
                }
            }

            Debug.Log($"地图 {mapID} 解锁状态已更新为: {isUnlocked} 并已保存");
        }
    }
    
    // 公共方法：解锁地图
    public void UnlockMap(int mapID)
    {
        UpdateMapLockStatus(mapID, true);
    }
    
    // 公共方法：锁定地图
    public void LockMap(int mapID)
    {
        UpdateMapLockStatus(mapID, false);
    }
    
    // 公共方法：重置所有地图解锁状态
    public void ResetAllMapUnlockStatus()
    {
        foreach (var mapData in mapDataList)
        {
            string unlockKey = $"Map_{mapData.id}_Unlocked";
            PlayerPrefs.DeleteKey(unlockKey);
        }
        PlayerPrefs.Save();
        
        // 重新加载数据
        LoadMapData();
        InitializeButtons();
        
        Debug.Log("所有地图解锁状态已重置");
    }
}
