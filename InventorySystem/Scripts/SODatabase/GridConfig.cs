using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "Inventory System/Grid Config")]
public class GridConfig : ScriptableObject
{
    [Header("网格基础配置")]
    public float cellSize = 80f;

    [Header("背包网格配置")]
    public int inventoryWidth = 10;
    public int inventoryHeight = 12;

    [Header("高级配置")]
    public float itemSpacing = 5f;
    public int maxRandomAttempts = 1000;

    // 验证配置的合理性
    private void OnValidate()
    {
        cellSize = Mathf.Max(1f, cellSize);
        inventoryWidth = Mathf.Max(1, inventoryWidth);
        inventoryHeight = Mathf.Max(1, inventoryHeight);
        itemSpacing = Mathf.Max(0f, itemSpacing);
        maxRandomAttempts = Mathf.Max(1, maxRandomAttempts);
    }
}