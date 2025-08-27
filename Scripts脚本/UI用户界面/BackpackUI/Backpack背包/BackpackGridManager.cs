using UnityEngine;
using System.Collections.Generic;

public class BackpackGridManager : MonoBehaviour
{

    private void Start()
    {
        Debug.Log("BackpackGridManager: 网格系统已禁用");
    }

    // 保留空方法以避免其他脚本调用时出错
    public void InitializeBackpackGrid()
    {
        Debug.LogWarning("BackpackGridManager: 网格系统已被禁用");
    }

    public bool IsBackpackGridAvailable()
    {
        return false;
    }

    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        return Vector2Int.zero;
    }
}