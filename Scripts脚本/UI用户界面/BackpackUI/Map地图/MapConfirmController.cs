using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapConfirmController : MonoBehaviour
{
    [Header("确认按钮精灵设置")]
    [SerializeField] private Sprite unlockedSprite;     // 地图解锁时显示的精灵
    [SerializeField] private Sprite lockedSprite;       // 地图未解锁时显示的精灵
    [SerializeField] private Sprite hoverClickSprite;   // 悬停或点击时显示的精灵
    
    [Header("地图数据")]
    [SerializeField] private TextAsset mapDataJson;
    
    [Header("按钮组件")]
    [SerializeField] private Button confirmButton;
    
    private Dictionary<int, MapData> mapDataDict = new Dictionary<int, MapData>();
    private int currentMapID = -1;
    private Image buttonImage;
    private Sprite originalSprite;
    private bool isMapUnlocked = false;
    
    void Start()
    {
        LoadMapData();
        InitializeButton();
    }
    
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
    
    // 接收地图ID的消息函数
    public void ReceiveMapID(int mapID)
    {
        currentMapID = mapID;
        Debug.Log($"MapConfirmController: 接收到地图ID: {mapID}");
        
        // 检查地图是否解锁并更新按钮精灵
        UpdateButtonSprite();
        
        // 获取地图信息
        if (mapDataDict.ContainsKey(mapID))
        {
            MapData mapData = mapDataDict[mapID];
            isMapUnlocked = mapData.isUnlocked;
            Debug.Log($"MapConfirmController: 地图信息 - 名称: {mapData.name}, 解锁状态: {mapData.isUnlocked}, 场景名称: {mapData.sceneName}");
        }
        else
        {
            Debug.LogWarning($"MapConfirmController: 未找到ID为 {mapID} 的地图数据");
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
        
        if (currentMapID >= 0 && mapDataDict.ContainsKey(currentMapID))
        {
            MapData mapData = mapDataDict[currentMapID];
            Debug.Log($"MapConfirmController: 确认按钮被点击 - 地图ID: {currentMapID}, 解锁状态: {mapData.isUnlocked}");
            
            if (mapData.isUnlocked)
            {
                Debug.Log($"MapConfirmController: 可以进入地图 - 场景名称: {mapData.sceneName}");
                // 这里可以添加场景切换逻辑
            }
            else
            {
                Debug.Log("MapConfirmController: 地图未解锁，无法进入");
            }
        }
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
}
