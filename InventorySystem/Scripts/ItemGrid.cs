using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    // ==================== 检查器数据显示 ====================
    [FieldLabel("网格宽度")] public int gridSizeWidth = 10;
    [FieldLabel("网格高度")] public int gridSizeHeight = 10;

    // ==================== 内部数据 ====================
    // 定义每个格子的宽度和高度
    public static float tileSizeWidth = 64f;
    public static float tileSizeHeight = 64f;


    // 物品存储数组
    Item[,] itemSlot;

    // 计算在格子中的位置

    RectTransform rectTransform;
    Canvas canvas;

    private void Start()
    {
        // 初始化物品存储数组
        itemSlot = new Item[gridSizeWidth, gridSizeHeight];

        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        Init(gridSizeWidth, gridSizeHeight);

        // 测试代码：添加一些测试物品
        // TestAddItems();
    }

    // 初始化网格大小
    void Init(int width, int height)
    {
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        // 计算相对于RectTransform的偏移量（从左上角开始）
        Vector2 positionOnTheGrid;
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        // 调整到左上角起始点（考虑RectTransform的锚点）
        float gridWidth = gridSizeWidth * tileSizeWidth * canvas.scaleFactor;
        float gridHeight = gridSizeHeight * tileSizeHeight * canvas.scaleFactor;

        // 如果RectTransform的锚点是中心，需要调整偏移
        positionOnTheGrid.x += gridWidth / 2;
        positionOnTheGrid.y += gridHeight / 2;

        // 转换为网格坐标，考虑Canvas缩放
        Vector2Int tileGridPosition = new Vector2Int();  // 初始化为默认值
        tileGridPosition.x = Mathf.FloorToInt(positionOnTheGrid.x / (tileSizeWidth * canvas.scaleFactor));
        tileGridPosition.y = Mathf.FloorToInt(positionOnTheGrid.y / (tileSizeHeight * canvas.scaleFactor));

        // 确保坐标在有效范围内
        tileGridPosition.x = Mathf.Clamp(tileGridPosition.x, 0, gridSizeWidth - 1);
        tileGridPosition.y = Mathf.Clamp(tileGridPosition.y, 0, gridSizeHeight - 1);

        return tileGridPosition;
    }

    // 在指定位置放置物品
    public bool PlaceItem(Item item, int posX, int posY)
    {
        // 判断物品是否超出边界
        if (BoundryCheck(posX, posY, item.GetWidth(), item.GetHeight()) == false)
        {
            Debug.LogWarning($"ItemGrid: 物品 {item.name} 在位置 ({posX}, {posY}) 超出边界");
            return false;
        }

        // 检查目标位置是否已被占用（检查物品占用的所有格子）
        for (int x = posX; x < posX + item.GetWidth(); x++)
        {
            for (int y = posY; y < posY + item.GetHeight(); y++)
            {
                if (itemSlot[x, y] != null)
                {
                    Debug.LogWarning($"ItemGrid: 位置 ({x}, {y}) 已被占用");
                    return false;
                }
            }
        }

        // 将物品存储到所有占用的格子中
        for (int x = posX; x < posX + item.GetWidth(); x++)
        {
            for (int y = posY; y < posY + item.GetHeight(); y++)
            {
                itemSlot[x, y] = item;
            }
        }

        // 设置物品的父对象为当前网格
        item.transform.SetParent(transform, false);

        // 计算物品在网格中的本地位置
        Vector2 position = CalculatePositionOnGrid(item, posX, posY);

        // 设置物品的本地位置
        item.transform.localPosition = position;

        // 设置物品的网格状态
        item.SetGridState(this, new Vector2Int(posX, posY));
        
        return true;
    }

    // 判断物品是否超出边界（设为public以供DraggableItem调用）
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) return false;
        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) return false;
        return true;
    }

    // 判断格子坐标是否超出（设为public以供边界检查调用）
    public bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0) return false;
        if (posX >= gridSizeWidth || posY >= gridSizeHeight) return false;
        return true;
    }

    // 计算物品在网格中的位置
    public Vector2 CalculatePositionOnGrid(Item item, int posX, int posY)
    {
        Vector2 position = new Vector2();

        // 计算网格的总尺寸
        float gridWidth = gridSizeWidth * tileSizeWidth;
        float gridHeight = gridSizeHeight * tileSizeHeight;

        // 基于中心对齐计算位置
        // 物品的中心轴心(0.5,0.5)对准格子的中心
        float itemWidth = item.GetWidth() * tileSizeWidth;
        float itemHeight = item.GetHeight() * tileSizeHeight;
        
        position.x = (posX + item.GetWidth() * 0.5f) * tileSizeWidth - gridWidth / 2;
        position.y = gridHeight / 2 - (posY + item.GetHeight() * 0.5f) * tileSizeHeight;

        return position;
    }



    // 从指定位置拾取物品
    public Item PickUpItem(int x, int y)
    {
        // 检查坐标是否有效
        if (x < 0 || x >= gridSizeWidth || y < 0 || y >= gridSizeHeight)
        {
            Debug.LogWarning($"ItemGrid: 尝试从无效位置 ({x}, {y}) 拾取物品");
            return null;
        }

        // 获取该位置的物品
        Item toReturn = itemSlot[x, y];

        // 如果有物品，则从所有占用的格子中移除
        if (toReturn != null)
        {
            // 清理物品占用的所有格子
            Vector2Int itemPosition = toReturn.OnGridPosition;
            int itemWidth = toReturn.GetWidth();
            int itemHeight = toReturn.GetHeight();

            for (int clearX = itemPosition.x; clearX < itemPosition.x + itemWidth; clearX++)
            {
                for (int clearY = itemPosition.y; clearY < itemPosition.y + itemHeight; clearY++)
                {
                    if (clearX >= 0 && clearX < gridSizeWidth && clearY >= 0 && clearY < gridSizeHeight)
                    {
                        itemSlot[clearX, clearY] = null;
                    }
                }
            }

            
        }


        return toReturn;
    }

    // 检查指定位置是否有物品
    public bool HasItemAt(int x, int y)
    {
        if (x < 0 || x >= gridSizeWidth || y < 0 || y >= gridSizeHeight)
        {
            return false;
        }

        return itemSlot[x, y] != null;
    }

    // 获取指定位置的物品
    public Item GetItemAt(int x, int y)
    {
        if (x < 0 || x >= gridSizeWidth || y < 0 || y >= gridSizeHeight)
        {
            return null;
        }

        return itemSlot[x, y];
    }

    // 按格子坐标获取物品（添加此方法以匹配教程接口）
    public Item GetItem(int x, int y)
    {
        return GetItemAt(x, y);
    }

    // 检查指定区域是否有物品冲突（排除指定物品）
    public bool HasItemConflict(int posX, int posY, int width, int height, Item excludeItem = null)
    {
        // 首先进行边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            Debug.LogWarning($"ItemGrid: 物品位置 ({posX}, {posY}) 超出边界，无法放置");
            return true; // 超出边界视为冲突
        }

        // 检查物品占用的所有格子
        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                // 检查坐标是否有效
                if (!PositionCheck(x, y))
                {
                    Debug.LogWarning($"ItemGrid: 检查位置 ({x}, {y}) 超出边界");
                    continue;
                }

                // 检查该位置是否有物品，且不是要排除的物品
                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != excludeItem)
                {
                    
                    return true; // 发现冲突
                }
            }
        }
        
        return false; // 没有冲突
    }

    // 根据鼠标位置检测物品（优化版）
    public Item GetItemAtMousePosition(int mouseX, int mouseY)
    {
        // 检查坐标是否有效
        if (mouseX < 0 || mouseX >= gridSizeWidth || mouseY < 0 || mouseY >= gridSizeHeight)
        {
            return null;
        }

        // 直接返回该位置的物品（现在所有占用格子都存储了物品引用）
        Item item = itemSlot[mouseX, mouseY];

        

        return item;
    }

    // 检查拖拽时的物品重叠冲突（专门用于拖拽场景的重叠检测）
    public bool HasOverlapConflict(int posX, int posY, int width, int height, Item draggingItem)
    {
        // 首先进行边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            return true; // 超出边界视为冲突
        }

        // 检查物品占用的所有格子是否与其他物品重叠
        for (int x = posX; x < posX + width; x++)
        {
            for (int y = posY; y < posY + height; y++)
            {
                // 检查该位置是否有物品，且不是正在拖拽的物品
                Item itemAt = itemSlot[x, y];
                if (itemAt != null && itemAt != draggingItem)
                {
                    return true; // 发现重叠冲突
                }
            }
        }

        return false; // 没有重叠冲突
    }

    // 检查指定位置是否可以放置物品（用于拖拽时的实时检测）
    public bool CanPlaceItemAtPosition(int posX, int posY, int width, int height, Item draggingItem)
    {
        // 边界检查
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        // 重叠检查
        return !HasOverlapConflict(posX, posY, width, height, draggingItem);
    }
}
