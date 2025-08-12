using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemGrid))]
public class ItemGridEditor : Editor
{
    private ItemGrid itemGrid;

    private void OnEnable()
    {
        itemGrid = (ItemGrid)target;
    }

    public override void OnInspectorGUI()
    {
        // 绘制默认的Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // 显示网格信息
        EditorGUILayout.LabelField("网格信息", EditorStyles.boldLabel);
        Vector2Int gridSize = itemGrid.GetGridSize();
        float cellSize = itemGrid.GetCellSize();

        EditorGUILayout.LabelField($"网格尺寸: {gridSize.x} x {gridSize.y}");
        EditorGUILayout.LabelField($"总格子数: {gridSize.x * gridSize.y}");
        EditorGUILayout.LabelField($"单元格大小: {cellSize}");
        EditorGUILayout.LabelField($"UI尺寸: {gridSize.x * cellSize} x {gridSize.y * cellSize}");

        // 显示GridConfig信息
        GridConfig config = itemGrid.GetGridConfig();
        if (config != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GridConfig 信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"配置文件: {config.name}");
            EditorGUILayout.LabelField($"单元格大小: {config.cellSize}");
            EditorGUILayout.LabelField($"背包宽度: {config.inventoryWidth}");
            EditorGUILayout.LabelField($"背包高度: {config.inventoryHeight}");
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("未分配 GridConfig，请在 Inspector 中分配一个 GridConfig 资源。", MessageType.Warning);
        }
    }
}
