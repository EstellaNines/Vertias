using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 动态背包网格管理器，负责创建和管理背包装备后生成的网格
/// </summary>
public class DynamicBackpackGridManager : MonoBehaviour
{
    [Header("网格精灵设置")]
    [SerializeField] private Sprite gridSprite; // Grid格子64x64精灵
    [SerializeField] private GameObject itemGridPrefab; // ItemGrid预制体（如果需要）

    [Header("网格样式设置")]
    [SerializeField] private Color gridBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    // 存储所有动态创建的网格
    private List<GameObject> dynamicGrids = new List<GameObject>();

    /// <summary>
    /// 创建背包网格
    /// </summary>
    /// <param name="parent">父对象（BackpackGridContainer）</param>
    /// <param name="width">网格宽度</param>
    /// <param name="height">网格高度</param>
    /// <param name="gridConfig">网格配置</param>
    /// <returns>创建的网格GameObject</returns>
    public GameObject CreateBackpackGrid(Transform parent, int width, int height, GridConfig gridConfig)
    {
        if (parent == null || gridConfig == null)
        {
            Debug.LogError("创建背包网格失败：缺少必要参数");
            return null;
        }

        // 获取容器的RectTransform
        RectTransform containerRect = parent.GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("父对象必须有RectTransform组件");
            return null;
        }

        // 计算网格所需大小
        float cellSize = gridConfig.cellSize;
        Vector2 requiredGridSize = new Vector2(width * cellSize, height * cellSize);
        
        // 检查容器大小是否足够，不够则扩大
        Vector2 containerSize = containerRect.sizeDelta;
        Vector2 newContainerSize = new Vector2(
            Mathf.Max(containerSize.x, requiredGridSize.x),
            Mathf.Max(containerSize.y, requiredGridSize.y)
        );
        
        // 如果需要扩大容器
        if (newContainerSize != containerSize)
        {
            containerRect.sizeDelta = newContainerSize;
            Debug.Log($"扩大背包容器尺寸从 {containerSize} 到 {newContainerSize}");
        }

        // 创建网格根对象
        GameObject gridRoot = new GameObject($"BackpackGrid_{width}x{height}");
        gridRoot.transform.SetParent(parent, false);

        // 添加RectTransform组件
        RectTransform rectTransform = gridRoot.AddComponent<RectTransform>();

        // 设置锚点和轴心为左上角
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        
        // 设置位置为左上角
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 设置网格大小
        rectTransform.sizeDelta = requiredGridSize;

        // 添加Image组件用于显示平铺的网格背景
        Image gridImage = gridRoot.AddComponent<Image>();
        
        // 设置精灵和平铺模式
        if (gridSprite != null)
        {
            gridImage.sprite = gridSprite;
            gridImage.type = Image.Type.Tiled;
        }
        else
        {
            // 如果没有精灵，使用纯色背景
            gridImage.color = gridBackgroundColor;
            Debug.LogWarning("未设置网格精灵，使用纯色背景");
        }

        // 添加ItemGrid组件
        ItemGrid itemGrid = gridRoot.AddComponent<ItemGrid>();

        // 创建动态网格配置
        GridConfig dynamicConfig = CreateDynamicGridConfig(width, height, cellSize);
        itemGrid.SetGridConfig(dynamicConfig);

        // 添加GridInteract组件
        GridInteract gridInteract = gridRoot.AddComponent<GridInteract>();

        // 添加到动态网格列表
        dynamicGrids.Add(gridRoot);

        // 通知InventoryController
        InventoryController inventoryController = FindObjectOfType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryController.SetSelectedGrid(itemGrid);
        }

        Debug.Log($"成功创建动态背包网格：{width}x{height}，网格大小：{requiredGridSize}，容器大小：{newContainerSize}");
        return gridRoot;
    }

    /// <summary>
    /// 创建动态网格配置
    /// </summary>
    private GridConfig CreateDynamicGridConfig(int width, int height, float cellSize)
    {
        GridConfig config = ScriptableObject.CreateInstance<GridConfig>();
        config.inventoryWidth = width;
        config.inventoryHeight = height;
        config.cellSize = cellSize;
        return config;
    }

    /// <summary>
    /// 销毁背包网格
    /// </summary>
    /// <param name="gridObject">要销毁的网格对象</param>
    public void DestroyBackpackGrid(GameObject gridObject)
    {
        if (gridObject == null) return;

        // 从列表中移除
        dynamicGrids.Remove(gridObject);

        // 清理网格中的物品（如果需要的话）
        ItemGrid itemGrid = gridObject.GetComponent<ItemGrid>();
        if (itemGrid != null)
        {
            // 这里可以添加清理网格中物品的逻辑
            // 比如将物品返回到主背包或销毁
        }

        // 销毁对象
        DestroyImmediate(gridObject);

        Debug.Log("销毁了动态背包网格");
    }

    /// <summary>
    /// 清理所有动态网格
    /// </summary>
    public void ClearAllDynamicGrids()
    {
        for (int i = dynamicGrids.Count - 1; i >= 0; i--)
        {
            if (dynamicGrids[i] != null)
            {
                DestroyBackpackGrid(dynamicGrids[i]);
            }
        }
        dynamicGrids.Clear();
    }

    /// <summary>
    /// 获取所有动态网格
    /// </summary>
    public List<GameObject> GetAllDynamicGrids()
    {
        return new List<GameObject>(dynamicGrids);
    }

    /// <summary>
    /// 设置网格精灵
    /// </summary>
    public void SetGridSprite(Sprite sprite)
    {
        gridSprite = sprite;
    }

    /// <summary>
    /// 获取网格精灵
    /// </summary>
    public Sprite GetGridSprite()
    {
        return gridSprite;
    }

    private void OnDestroy()
    {
        // 清理所有动态创建的ScriptableObject
        ClearAllDynamicGrids();
    }
}
