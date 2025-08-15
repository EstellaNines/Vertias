// TacticalRigItemGrid.cs
// 战术挂具专用网格，继承 BaseItemGrid，支持运行时切换数据
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TactiaclRigItemGrid : BaseItemGrid
{
    [Header("战术挂具网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 1;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 1;

    // 当前战术挂具数据（运行时动态设置）
    private InventorySystemItemDataSO currentTacticalRigData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromTacticalRigData();
        base.Awake();
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

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态数据 ---------------- */
    private void LoadFromTacticalRigData()
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

    /// <summary>运行时更换战术挂具</summary>
    public void SetTacticalRigData(InventorySystemItemDataSO data)
    {
        currentTacticalRigData = data;
        LoadFromTacticalRigData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetTacticalRigData() => currentTacticalRigData;

    /// <summary>挂具占用率</summary>
    public float GetTacticalRigOccupancyRate() => GetOccupancyRate();

    /// <summary>挂具剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;
}