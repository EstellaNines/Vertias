using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ItemGrid : BaseItemGrid
{
    [SerializeField][FieldLabel("网格系统宽度格数")] private int inventoryWidth = 10;
    [SerializeField][FieldLabel("网格系统高度格数")] private int inventoryHeight = 30;

    protected override void Awake()
    {
        // 确保在base.Awake()之前加载配置
        LoadFromGridConfig();
        base.Awake();
    }

    public void LoadFromGridConfig()
    {
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            inventoryWidth = gridConfig.inventoryWidth;
            inventoryHeight = gridConfig.inventoryHeight;
            width = inventoryWidth;
            height = inventoryHeight;

            // 强制更新网格数组
            InitializeGridArrays();

            isUpdatingFromConfig = false;

            // 移除showDebugInfo引用，直接使用Debug.Log
            Debug.Log($"从GridConfig加载尺寸: {inventoryWidth}x{inventoryHeight}");
        }
    }

    protected override void Start()
    {
        LoadFromGridConfig();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;

        inventoryWidth = Mathf.Clamp(inventoryWidth, 1, 50);
        inventoryHeight = Mathf.Clamp(inventoryHeight, 1, 50);

        width = inventoryWidth;
        height = inventoryHeight;

        base.OnValidate();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SaveToGridConfigDelayed();
        }
#endif
    }

#if UNITY_EDITOR
    private void SaveToGridConfigDelayed()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            SaveToGridConfig();
        };
    }
#endif

    protected override void Init(int width, int height)
    {
        if (rectTransform == null) return;

        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;

        Vector2 size = new Vector2(
            width * cellSize,
            height * cellSize
        );
        rectTransform.sizeDelta = size;
    }

    // 删除重复的LoadFromGridConfig方法，只保留上面的一个

    public void SaveToGridConfig()
    {
#if UNITY_EDITOR
        if (gridConfig != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;

            bool hasChanged = false;
            if (gridConfig.inventoryWidth != inventoryWidth)
            {
                gridConfig.inventoryWidth = inventoryWidth;
                hasChanged = true;
            }
            if (gridConfig.inventoryHeight != inventoryHeight)
            {
                gridConfig.inventoryHeight = inventoryHeight;
                hasChanged = true;
            }

            if (hasChanged)
            {
                EditorUtility.SetDirty(gridConfig);
                AssetDatabase.SaveAssets();
            }

            isUpdatingFromConfig = false;
        }
#endif
    }

    public void SyncToGridConfig()
    {
        SaveToGridConfig();
    }
}
