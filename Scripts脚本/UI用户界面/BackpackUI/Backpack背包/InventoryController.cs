using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField][FieldLabel("物品列表")] List<ItemData> items;
    [SerializeField][FieldLabel("物品预制体")] GameObject itemPrefab;
    public ItemGrid selectedItemGrid;//操作的背包
    Item selectedItem;//选中物品
    Item overlapItem;//重叠物品
    Canvas canvas;
    InventoryHighlight inventoryHighlight;
    Item itemToHighlight;//高亮显示物品
    Vector2Int oldPosition;
    
    // 添加索引变量用于按顺序添加物品
    private int currentItemIndex = 0;

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        inventoryHighlight = GetComponent<InventoryHighlight>();
    }
    
    private void Update()
    {
        //TODO: 方便测试，动态按顺序添加物品
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CreateSequentialItem();
        }
        
        //TODO: 方便测试，动态随机添加物品（保留原方法）
        if (Input.GetKeyDown(KeyCode.R))
        {
            CreateRandomItem();
        }

        //物品跟随鼠标
        if (selectedItem) selectedItem.transform.position = Input.mousePosition;

        if (selectedItemGrid == null)
        {
            inventoryHighlight.Show(false);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(selectedItemGrid.GetTileGridPosition(Input.mousePosition));

            LeftMouseButtonPress();
        }

        //高亮显示
        HandleHighlight();
    }

    //按顺序添加物品
    private void CreateSequentialItem()
    {
        if (selectedItem) return;
        if (items.Count == 0) return;
        
        Item item = Instantiate(itemPrefab).GetComponent<Item>();
        selectedItem = item;
        selectedItem.transform.SetParent(canvas.transform, false);
        
        // 按顺序选择物品
        item.Set(items[currentItemIndex]);
        
        // 更新索引，循环到下一个物品
        currentItemIndex = (currentItemIndex + 1) % items.Count;
    }

    //随机添加物品（保留原方法）
    private void CreateRandomItem()
    {
        if (selectedItem) return;
        if (items.Count == 0) return;
        
        Item item = Instantiate(itemPrefab).GetComponent<Item>();
        selectedItem = item;
        selectedItem.transform.SetParent(canvas.transform, false);
        int index = UnityEngine.Random.Range(0, items.Count);
        item.Set(items[index]);
    }

    //点击操作，选中物品
    private void LeftMouseButtonPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();
        if (selectedItem == null)
        {
            //选中物品
            selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        }
        else
        {
            // 移动物品
            PlaceItem(tileGridPosition);
        }
    }

    //鼠标坐标转化为格子坐标
    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.itemData.width - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.itemData.height - 1) * ItemGrid.tileSizeHeight / 2;
        }
        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(position);
        return tileGridPosition;
    }

    //移动物品
    void PlaceItem(Vector2Int tileGridPosition)
    {
        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if (complete)
        {
            selectedItem = null;

            //如果存在重叠物品
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
            }
        }
    }

    //高亮显示
    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();

        //节约没必要的计算
        if(oldPosition == positionOnGrid) return;
        oldPosition = positionOnGrid;

        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetParent(selectedItemGrid);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
            }else{
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            inventoryHighlight.Show(selectedItemGrid.BoundryCheck(
                    positionOnGrid.x,
                    positionOnGrid.y,
                    selectedItem.itemData.width,
                    selectedItem.itemData.height)
            );//防止显示跨界
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetParent(selectedItemGrid);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }
}