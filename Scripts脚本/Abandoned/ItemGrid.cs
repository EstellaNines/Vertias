using UnityEngine;
using System.Collections.Generic;

public class ItemGrid : MonoBehaviour
{
    [SerializeField]
    [FieldLabel("背包格子宽度")] int gridSizeWidth = 10; // 网格总宽度（格子数）

    [SerializeField][FieldLabel("背包格子高度")] int gridSizeHeight = 10; // 网格总高度（格子数）

    [SerializeField]
    [FieldLabel("物品引用")] private GameObject itemPrefab; // 物品预制体引用，用于动态生成物品

    BaseItem[,] itemSlot; // 二维数组，记录每个格子里放的物品
    BaseItem overlapItem;//重叠物品

    // 单个格子的像素宽高（256 像素图被 4 等分）
    public static float tileSizeWidth = 256f / 4f; // 格子宽度
    public static float tileSizeHeight = 256f / 4f;  // 格子高度

    Vector2 positionOnTheGrid = new Vector2(); // 临时变量：鼠标相对于网格的偏移坐标
    Vector2Int tileGridPosition = new Vector2Int(); // 临时变量：最终计算出的格子坐标

    RectTransform rectTransform; // 当前 UI 的 RectTransform 组件引用
    Canvas canvas; // 场景中的 Canvas，用于计算缩放因子

    private void Start()
    {
        itemSlot = new BaseItem[gridSizeWidth, gridSizeHeight]; // 按设定宽高创建二维数组

        rectTransform = GetComponent<RectTransform>(); // 获取自身的 RectTransform 组件
        canvas = FindObjectOfType<Canvas>(); // 查找场景中的 Canvas

        Init(gridSizeWidth, gridSizeHeight); // 根据格子数初始化网格大小

    }

    void Init(int width, int height)// 初始化网格尺寸
    {
        Vector2 size = new Vector2(
            width * tileSizeWidth, // 计算总宽度
            height * tileSizeHeight // 计算总高度
        );
        rectTransform.sizeDelta = size; // 设置 RectTransform 的大小
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 如果按下鼠标左键
        {
            // 打印当前鼠标在网格中的格子坐标
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }
    }

    // 根据屏幕鼠标位置，计算对应的格子坐标
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        // 计算鼠标相对于网格左下角的偏移量
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        // 除以单个格子宽高，再除以 Canvas 的缩放因子，得到整数格子坐标
        tileGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidth / canvas.scaleFactor);
        tileGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight / canvas.scaleFactor);

        return tileGridPosition;                      // 返回计算结果
    }

    //按格子坐标添加物品
    public bool PlaceItem(BaseItem item, int posX, int posY, ref BaseItem overlapItem)
    {
        //判断物品是否超出边界
        if (BoundryCheck(posX, posY, item.itemData.width, item.itemData.height) == false) return false;

        //检查指定位置和范围内是否存在重叠物品，有多个重叠物品退出
        if (OverlapCheck(posX, posY, item.itemData.width, item.itemData.height, ref overlapItem) == false) return false;

        if (overlapItem) CleanGridReference(overlapItem);

        item.transform.SetParent(transform, false);

        // 按物品尺寸占用对应大小的格子
        for (int ix = 0; ix < item.itemData.width; ix++)
        {
            for (int iy = 0; iy < item.itemData.height; iy++)
            {
                itemSlot[posX + ix, posY + iy] = item;
            }
        }
        item.onGridPositionX = posX;
        item.onGridPositionY = posY;

        Vector2 positon = new Vector2();
        positon.x = posX * tileSizeWidth + tileSizeWidth * item.itemData.width / 2;
        positon.y = -(posY * tileSizeHeight + tileSizeHeight * item.itemData.height / 2);
        item.transform.localPosition = positon;

        return true; // 成功放置
    }

    //按格子坐标获取物品
    public BaseItem PickUpItem(int x, int y)
    {
        BaseItem toReturn = itemSlot[x, y];

        if (toReturn == null) return null;

        CleanGridReference(toReturn);

        return toReturn;
    }

    //按物品尺寸取消对应大小的格子的占用
    void CleanGridReference(BaseItem item)
    {
        for (int ix = 0; ix < item.itemData.width; ix++)
        {
            for (int iy = 0; iy < item.itemData.height; iy++)
            {
                itemSlot[item.onGridPositionX + ix, item.onGridPositionY + iy] = null;
            }
        }
    }

    //判断物品是否超出边界
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) return false;

        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) return false;
        return true;
    }

    //判断格子坐标是否超出
    bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0) return false;

        if (posX >= gridSizeWidth || posY >= gridSizeHeight) return false;

        return true;
    }

    //检查指定位置和范围内是否存在重叠物品，并overlapItem返回重叠物品，，有多个重叠物品返回false
    private bool OverlapCheck(int posX, int posY, int width, int height, ref BaseItem overlapItem)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 如果当前位置有物品
                if (itemSlot[posX + x, posY + y] != null)
                {
                    // 如果 overlapItem 还未被赋值（第一次找到重叠物品）
                    if (overlapItem == null)
                    {
                        overlapItem = itemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        // 如果发现范围有多个重叠物品
                        if (overlapItem != itemSlot[posX + x, posY + y])
                        {
                            overlapItem = null;
                            return false;
                        }
                    }
                }
            }
        }
        // 如果所有被检查的位置都有相同的重叠物品，则返回 true
        return true;
    }

    //按格子坐标转化为UI坐标位置
    public Vector2 CalculatePositionOnGrid(BaseItem item, int posX, int posY)
    {
        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * item.itemData.width / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * item.itemData.height / 2);
        return position;
    }

    //按格子坐标获取物品
    internal BaseItem GetItem(int x, int y)
    {
        return itemSlot[x, y];
    }

    // 尝试在网格中随机放置物品，成功返回true，失败返回false

    // 清空网格状态
    public void ClearGrid()
    {
        if (itemSlot != null)
        {
            for (int x = 0; x < gridSizeWidth; x++)
            {
                for (int y = 0; y < gridSizeHeight; y++)
                {
                    itemSlot[x, y] = null;
                }
            }
        }
    }

    // 改进的随机放置算法
    public bool TryPlaceItemRandomly(BaseItem item, int maxAttempts = 100)
    {
        int width = item.itemData.width;
        int height = item.itemData.height;

        // 如果物品尺寸超过网格，直接返回失败
        if (width > gridSizeWidth || height > gridSizeHeight)
        {
            Debug.LogWarning($"物品尺寸({width}x{height})超过网格大小({gridSizeWidth}x{gridSizeHeight})");
            return false;
        }

        // 创建可用位置列表
        List<Vector2Int> availablePositions = new List<Vector2Int>();

        for (int x = 0; x <= gridSizeWidth - width; x++)
        {
            for (int y = 0; y <= gridSizeHeight - height; y++)
            {
                if (CanPlaceAt(x, y, width, height))
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // 如果没有可用位置，返回失败
        if (availablePositions.Count == 0)
        {
            return false;
        }

        // 随机选择一个可用位置
        Vector2Int selectedPos = availablePositions[Random.Range(0, availablePositions.Count)];

        BaseItem overlapItem = null;
        return PlaceItem(item, selectedPos.x, selectedPos.y, ref overlapItem);
    }

    // 检查指定位置是否可以放置物品
    private bool CanPlaceAt(int posX, int posY, int width, int height)
    {
        // 边界检查
        if (!BoundryCheck(posX, posY, width, height))
            return false;

        // 检查是否有重叠
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (itemSlot[posX + x, posY + y] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 获取网格尺寸（供外部访问）
    public int GridSizeWidth { get { return gridSizeWidth; } }
    public int GridSizeHeight { get { return gridSizeHeight; } }

    // 按顺序放置物品（从左到右，从上到下）
    public bool TryPlaceItemSequentially(BaseItem item)
    {
        int width = item.itemData.width;
        int height = item.itemData.height;

        // 如果物品尺寸超过网格，直接返回失败
        if (width > gridSizeWidth || height > gridSizeHeight)
        {
            Debug.LogWarning($"物品尺寸({width}x{height})超过网格大小({gridSizeWidth}x{gridSizeHeight})");
            return false;
        }

        // 从左到右，从上到下遍历网格
        for (int y = 0; y <= gridSizeHeight - height; y++)
        {
            for (int x = 0; x <= gridSizeWidth - width; x++)
            {
                if (CanPlaceAt(x, y, width, height))
                {
                    BaseItem overlapItem = null;
                    return PlaceItem(item, x, y, ref overlapItem);
                }
            }
        }

        // 没有找到合适的位置
        return false;
    }
}