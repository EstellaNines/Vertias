using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TactiaclRigItemGrid : BaseItemGrid
{
    [Header("战术挂具网格系统参数设置")]
    [SerializeField][FieldLabel("默认网格宽度")] private int defaultWidth = 1;
    [SerializeField][FieldLabel("默认网格高度")] private int defaultHeight = 1;
    
    // 当前装备的战术挂具数据对象（动态设置）
    private InventorySystemItemDataSO currentTacticalRigData;

    protected override void Awake()
    {
        base.Awake();
        LoadFromTacticalRigData();
    }

    protected override void Start()
    {
        LoadFromTacticalRigData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;

        LoadFromTacticalRigData();
        
        width = Mathf.Clamp(width, 1, 20);
        height = Mathf.Clamp(height, 1, 20);
        
        base.OnValidate();
    }

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

    // 从战术挂具数据加载配置
    public void LoadFromTacticalRigData()
    {
        if (currentTacticalRigData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentTacticalRigData.CellH;
            height = currentTacticalRigData.CellV;
            isUpdatingFromConfig = false;
        }
        else if (!isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = defaultWidth;
            height = defaultHeight;
            isUpdatingFromConfig = false;
        }
    }

    // 设置战术挂具数据并更新网格
    public void SetTacticalRigData(InventorySystemItemDataSO data)
    {
        currentTacticalRigData = data;
        LoadFromTacticalRigData();

        if (Application.isPlaying)
        {
            gridOccupancy = new bool[width, height];
            placedItems.Clear();
        }

        Init(width, height);
    }

    // 获取战术挂具数据
    public InventorySystemItemDataSO GetTacticalRigData()
    {
        return currentTacticalRigData;
    }

    public override void PlaceItem(GameObject itemObject, Vector2Int position, Vector2Int size)
    {
        base.PlaceItem(itemObject, position, size);
        Debug.Log($"物品放置在战术挂具网格位置: ({position.x}, {position.y}), 物品尺寸: {size.x}x{size.y}");
    }
}
