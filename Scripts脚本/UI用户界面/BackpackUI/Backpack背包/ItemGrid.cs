using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemGrid : MonoBehaviour
{
    // 定义每个格子的宽度和高度
    const float tileSizeWidth = 32 / 2;
    const float tileSizeHeight = 32 / 2;

    // 计算在格子中的位置
    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    RectTransform rectTransform;
    Canvas canvas;
    
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();  
        canvas = FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 首先检查鼠标是否在image组件内部
            if (IsMouseOverImage(Input.mousePosition))
            {
                // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
                Vector2Int gridPos = GetTileGridPosition(Input.mousePosition);
                Debug.Log($"格子坐标: ({gridPos.x}, {gridPos.y})");
            }
        }
    }

    // 检查鼠标是否在image组件内部
    public bool IsMouseOverImage(Vector2 mousePosition)
    {
        Vector2 localPoint;
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            mousePosition, 
            canvas.worldCamera, 
            out localPoint
        );
        
        if (!isInside) return false;
        
        // 检查本地坐标是否在RectTransform的边界内
        Rect rect = rectTransform.rect;
        return localPoint.x >= rect.xMin && localPoint.x <= rect.xMax &&
               localPoint.y >= rect.yMin && localPoint.y <= rect.yMax;
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        // 使用RectTransformUtility进行更精确的坐标转换
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            mousePosition, 
            canvas.worldCamera, 
            out localPoint
        );
        
        // 调整坐标系，使左上角为原点(0,0)
        // RectTransform的本地坐标系中心在中央，需要转换为左上角为原点
        float adjustedX = localPoint.x + rectTransform.rect.width * 0.5f;
        float adjustedY = rectTransform.rect.height * 0.5f - localPoint.y;
        
        // 计算格子坐标，使用Mathf.FloorToInt确保整数格子坐标
        tileGridPosition.x = Mathf.FloorToInt(adjustedX / tileSizeWidth);
        tileGridPosition.y = Mathf.FloorToInt(adjustedY / tileSizeHeight);
        
        // 确保坐标不为负数（防止点击到网格外部时出现负坐标）
        tileGridPosition.x = Mathf.Max(0, tileGridPosition.x);
        tileGridPosition.y = Mathf.Max(0, tileGridPosition.y);
        
        // 限制坐标在网格范围内（可选，根据你的网格大小调整）
        int maxGridX = Mathf.FloorToInt(rectTransform.rect.width / tileSizeWidth) - 1;
        int maxGridY = Mathf.FloorToInt(rectTransform.rect.height / tileSizeHeight) - 1;
        
        tileGridPosition.x = Mathf.Min(tileGridPosition.x, maxGridX);
        tileGridPosition.y = Mathf.Min(tileGridPosition.y, maxGridY);

        return tileGridPosition;
    }
    
    // 可选：根据格子坐标获取该格子的中心世界坐标
    public Vector2 GetGridCenterWorldPosition(Vector2Int gridPosition)
    {
        // 计算格子中心的本地坐标
        float centerX = (gridPosition.x + 0.5f) * tileSizeWidth - rectTransform.rect.width * 0.5f;
        float centerY = rectTransform.rect.height * 0.5f - (gridPosition.y + 0.5f) * tileSizeHeight;
        
        Vector2 localCenter = new Vector2(centerX, centerY);
        
        // 转换为世界坐标
        Vector3 worldPosition = rectTransform.TransformPoint(localCenter);
        return worldPosition;
    }
    
    // 可选：检查格子坐标是否在有效范围内
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        int maxGridX = Mathf.FloorToInt(rectTransform.rect.width / tileSizeWidth);
        int maxGridY = Mathf.FloorToInt(rectTransform.rect.height / tileSizeHeight);
        
        return gridPosition.x >= 0 && gridPosition.x < maxGridX && 
               gridPosition.y >= 0 && gridPosition.y < maxGridY;
    }
}
