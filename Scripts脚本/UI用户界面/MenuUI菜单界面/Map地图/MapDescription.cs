using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapDescription : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] [FieldLabel("地图名称文本")]private TextMeshProUGUI mapNameText;        // 地图名称文本
    [SerializeField] [FieldLabel("地图描述文本")]private TextMeshProUGUI mapDescriptionText; // 地图描述文本
    [SerializeField] [FieldLabel("地图缩略图")]private Image mapThumbnailImage;            // 地图缩略图

    [Header("地图属性UI组件")]
    [SerializeField] [FieldLabel("难度图标")]private Image difficultyImage;             // 难度图标

    [SerializeField] [FieldLabel("难度文本")]private TextMeshProUGUI difficultyText;    // 难度文本
    [SerializeField] [FieldLabel("资源丰富度图标")]private Image lootLevelImage;              // 资源丰富度图标
    [SerializeField] [FieldLabel("资源丰富度文本")]private TextMeshProUGUI lootLevelText;     // 资源丰富度文本
    [SerializeField] [FieldLabel("敌人数量图标")]private Image enemyCountImage;             // 敌人数量图标
    [SerializeField] [FieldLabel("敌人数量文本")]private TextMeshProUGUI enemyCountText;    // 敌人数量文本

    [Header("地图数据")]
    [SerializeField] [FieldLabel("地图数据JSON文件")]private TextAsset mapDataJson;             // JSON数据文件

    [Header("调试信息")]
    [SerializeField] [FieldLabel("是否显示调试信息")]private bool showDebugInfo = true;

    private Dictionary<int, MapData> mapDataDict = new Dictionary<int, MapData>();
    private MapButtonManager buttonManager;

    void Start()
    {
        // 获取MapButtonManager引用
        buttonManager = FindObjectOfType<MapButtonManager>();
        if (buttonManager == null)
        {
            Debug.LogError("未找到MapButtonManager组件！");
            return;
        }

        // 加载地图数据
        LoadMapData();

        // 订阅按钮点击事件
        SubscribeToButtonEvents();

        // 初始化时隐藏所有图标
        HideAllIcons();
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

                    if (showDebugInfo)
                    {
                        Debug.Log($"MapDescription: 成功加载 {mapDataDict.Count} 个地图数据");
                    }
                }
                else
                {
                    Debug.LogError("MapDescription: 地图数据为空或格式不正确");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapDescription: 加载地图数据失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("MapDescription: 未设置地图数据JSON文件");
        }
    }

    // 订阅按钮点击事件
    private void SubscribeToButtonEvents()
    {
        if (buttonManager != null)
        {
            // 获取所有地图按钮并添加点击监听
            Button[] allButtons = FindObjectsOfType<Button>();
            foreach (Button button in allButtons)
            {
                MapButtonID buttonID = button.GetComponent<MapButtonID>();
                if (buttonID != null)
                {
                    // 为每个有MapButtonID的按钮添加点击事件
                    button.onClick.AddListener(() => OnMapButtonClicked(button));

                    if (showDebugInfo)
                    {
                        Debug.Log($"MapDescription: 已为按钮 {button.name} 添加点击监听");
                    }
                }
            }
        }
    }

    // 按钮点击事件处理
    private void OnMapButtonClicked(Button clickedButton)
    {
        MapButtonID buttonID = clickedButton.GetComponent<MapButtonID>();
        if (buttonID != null)
        {
            int mapID = buttonID.MapID;
            DisplayMapInfo(mapID);

            if (showDebugInfo)
            {
                Debug.Log($"MapDescription: 按钮 {clickedButton.name} 被点击，地图ID: {mapID}");
            }
        }
    }

    // 显示地图信息
    public void DisplayMapInfo(int mapID)
    {
        if (mapDataDict.ContainsKey(mapID))
        {
            MapData mapData = mapDataDict[mapID];

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

            // 更新地图缩略图
            if (mapThumbnailImage != null)
            {
                LoadMapThumbnail(mapData.thumbnail);
            }

            // 显示并更新难度信息
            if (difficultyImage != null)
            {
                difficultyImage.gameObject.SetActive(true);
            }
            if (difficultyText != null)
            {
                difficultyText.text = mapData.difficulty;
            }

            // 显示并更新资源丰富度信息
            if (lootLevelImage != null)
            {
                lootLevelImage.gameObject.SetActive(true);
            }
            if (lootLevelText != null)
            {
                lootLevelText.text = mapData.lootLevel;
            }

            // 显示并更新敌人数量信息
            if (enemyCountImage != null)
            {
                enemyCountImage.gameObject.SetActive(true);
            }
            if (enemyCountText != null)
            {
                enemyCountText.text = mapData.enemyCount;
            }

            if (showDebugInfo)
            {
                Debug.Log($"MapDescription: 已显示地图信息 - ID: {mapData.id}, 名称: {mapData.name}, 难度: {mapData.difficulty}, 资源: {mapData.lootLevel}, 敌人: {mapData.enemyCount}");
            }
        }
        else
        {
            Debug.LogWarning($"MapDescription: 未找到ID为 {mapID} 的地图数据");

            // 显示默认信息
            if (mapNameText != null)
            {
                mapNameText.text = "未知地图";
            }

            if (mapDescriptionText != null)
            {
                mapDescriptionText.text = "暂无描述信息";
            }

            // 显示图标并设置默认文本
            if (difficultyImage != null)
            {
                difficultyImage.gameObject.SetActive(true);
            }
            if (difficultyText != null)
            {
                difficultyText.text = "未知";
            }

            if (lootLevelImage != null)
            {
                lootLevelImage.gameObject.SetActive(true);
            }
            if (lootLevelText != null)
            {
                lootLevelText.text = "未知";
            }

            if (enemyCountImage != null)
            {
                enemyCountImage.gameObject.SetActive(true);
            }
            if (enemyCountText != null)
            {
                enemyCountText.text = "未知";
            }
        }
    }

    // 加载地图缩略图
    private void LoadMapThumbnail(string thumbnailPath)
    {
        if (string.IsNullOrEmpty(thumbnailPath))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("MapDescription: 缩略图路径为空");
            }
            return;
        }

        // 从Resources/MapImages文件夹加载图片
        // 移除路径中的"MapImages/"前缀和".png"后缀（如果存在）
        string imageName = thumbnailPath;
        if (imageName.StartsWith("MapImages/"))
        {
            imageName = imageName.Substring("MapImages/".Length);
        }
        if (imageName.EndsWith(".png"))
        {
            imageName = imageName.Substring(0, imageName.Length - 4);
        }

        // 构建完整的Resources路径
        string resourcePath = "MapImages/" + imageName;
        Sprite thumbnailSprite = Resources.Load<Sprite>(resourcePath);

        if (thumbnailSprite != null)
        {
            mapThumbnailImage.sprite = thumbnailSprite;

            if (showDebugInfo)
            {
                Debug.Log($"MapDescription: 成功加载缩略图: {resourcePath}");
            }
        }
        else
        {
            Debug.LogWarning($"MapDescription: 无法加载缩略图: {resourcePath}");
        }
    }

    // 公共方法：手动显示指定地图信息
    public void ShowMapInfo(int mapID)
    {
        DisplayMapInfo(mapID);
    }

    // 公共方法：清空显示内容
    public void ClearDisplay()
    {
        if (mapNameText != null)
        {
            mapNameText.text = "";
        }

        if (mapDescriptionText != null)
        {
            mapDescriptionText.text = "";
        }

        if (mapThumbnailImage != null)
        {
            mapThumbnailImage.sprite = null;
        }

        if (difficultyText != null)
        {
            difficultyText.text = "";
        }

        if (lootLevelText != null)
        {
            lootLevelText.text = "";
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = "";
        }

        // 隐藏所有图标
        HideAllIcons();

        if (showDebugInfo)
        {
            Debug.Log("MapDescription: 已清空显示内容并隐藏图标");
        }
    }

    // 隐藏所有图标
    private void HideAllIcons()
    {
        if (difficultyImage != null)
        {
            difficultyImage.gameObject.SetActive(false);
        }

        if (lootLevelImage != null)
        {
            lootLevelImage.gameObject.SetActive(false);
        }

        if (enemyCountImage != null)
        {
            enemyCountImage.gameObject.SetActive(false);
        }
    }

    // 获取地图数据（供外部调用）
    public MapData GetMapData(int mapID)
    {
        if (mapDataDict.ContainsKey(mapID))
        {
            return mapDataDict[mapID];
        }
        return null;
    }
}
