using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//控制高亮背景显示
public class InventoryHighlight : MonoBehaviour
{
    [SerializeField] RectTransform highlighter;

    // 设置高亮框大小
    public void SetSize(Item targetItem)
    {
        Vector2 size = new Vector2();
        size.x = targetItem.itemData.width * ItemGrid.tileSizeWidth;
        size.y = targetItem.itemData.height * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;
    }

    // 设置高亮框位置
    public void SetPosition(ItemGrid targetGrid, Item targetItem)
    {
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, targetItem.onGridPositionX, targetItem.onGridPositionY);
        highlighter.localPosition = pos;
    }

    //显示隐藏
    public void Show(bool b)
    {
        highlighter.gameObject.SetActive(b);
    }

    //设置高亮背景父级
    public void SetParent(ItemGrid targetGrid)
    {
        highlighter.SetParent(targetGrid.GetComponent<RectTransform>());
    }

    //设置高亮框位置
    public void SetPosition(ItemGrid targetGrid, Item targetItem, int posX, int posY)
    {
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;
    }

}
