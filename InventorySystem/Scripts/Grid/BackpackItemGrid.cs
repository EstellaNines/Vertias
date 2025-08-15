// BackpackItemGrid.cs
// 背包专用网格，直接继承 BaseItemGrid 并支持运行时切换背包数据
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BackpackItemGrid : BaseItemGrid
{
    [Header("背包网格参数")]
    [SerializeField, Tooltip("默认宽度")] private int defaultWidth = 6;
    [SerializeField, Tooltip("默认高度")] private int defaultHeight = 8;

    // 当前背包数据（运行时动态设置）
    private InventorySystemItemDataSO currentBackpackData;

    /* ---------------- 生命周期 ---------------- */
    protected override void Awake()
    {
        LoadFromBackpackData();
        base.Awake();
    }

    protected override void Start()
    {
        LoadFromBackpackData();
        base.Start();
    }

    protected override void OnValidate()
    {
        if (isUpdatingFromConfig) return;
        LoadFromBackpackData();
        width = Mathf.Clamp(width, 1, 50);
        height = Mathf.Clamp(height, 1, 50);
        base.OnValidate();
    }

    protected override void Init(int w, int h)
    {
        if (rectTransform == null) return;
        float cellSize = gridConfig != null ? gridConfig.cellSize : 64f;
        rectTransform.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        if (Application.isPlaying) InitializeGridArrays();
    }

    /* ---------------- 动态背包 ---------------- */
    private void LoadFromBackpackData()
    {
        if (currentBackpackData != null && !isUpdatingFromConfig)
        {
            isUpdatingFromConfig = true;
            width = currentBackpackData.CellH;
            height = currentBackpackData.CellV;
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

    /// <summary>运行时更换背包（换装更大的背包）</summary>
    public void SetBackpackData(InventorySystemItemDataSO data)
    {
        currentBackpackData = data;
        LoadFromBackpackData();
        if (Application.isPlaying)
        {
            InitializeGridArrays();
            placedItems.Clear();
        }
        Init(width, height);
    }

    public InventorySystemItemDataSO GetCurrentBackpackData() => currentBackpackData;

    /// <summary>背包占用率</summary>
    public float GetBackpackOccupancyRate() => GetOccupancyRate();

    /// <summary>背包剩余格子数</summary>
    public int GetRemainingSpace() => width * height - occupiedCells.Count;
}