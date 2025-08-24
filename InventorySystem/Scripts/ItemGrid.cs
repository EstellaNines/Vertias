using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    // ==================== 检查器数据显示 ====================
    [SerializeField] int gridSizeWidth = 10;
    [SerializeField] int gridSizeHeight = 10;

    // ==================== 内部数据 ====================
    // 定义每个格子的宽度和高度
    const float tileSizeWidth = 64;
    const float tileSizeHeight = 64;

    // 计算在格子中的位置
    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    RectTransform rectTransform;
    Canvas canvas;
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        Init(gridSizeWidth, gridSizeHeight);
    }

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
        // 计算鼠标位置相对于 RectTransform 的偏移量
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        // 将偏移量转换为网格位置
        // 这里 tileSizeWidth 和 tileSizeHeight 是单个瓦片的宽度和高度
        // canvas.scaleFactor 是 Canvas 的缩放因子（通常用于 UI 适配不同分辨率）
        tileGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidth / canvas.scaleFactor);
        tileGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight / canvas.scaleFactor);

        // 返回计算出的网格位置
        return tileGridPosition;
    }
}
