// using UnityEngine;
// using System.Collections.Generic;

// /// <summary>
// /// 网格可见性控制器 - 控制网格的隐藏与激活
// /// 提供运行时动态控制网格显示状态的功能
// /// </summary>
// public class GridVisibilityController : MonoBehaviour
// {
//     [Header("网格控制设置")]
//     [SerializeField] private List<ItemGrid> controlledGrids = new List<ItemGrid>();
//     [SerializeField] private bool autoFindGrids = true;
//     [SerializeField] private bool enableDebugLog = true;
    
//     [Header("快捷键设置")]
//     [SerializeField] private KeyCode toggleAllKey = KeyCode.G;
//     [SerializeField] private KeyCode hideAllKey = KeyCode.H;
//     [SerializeField] private KeyCode showAllKey = KeyCode.J;
    
//     // 网格状态记录
//     private Dictionary<ItemGrid, bool> gridStates = new Dictionary<ItemGrid, bool>();
    
//     private void Start()
//     {
//         InitializeController();
//     }
    
//     private void Update()
//     {
//         HandleInput();
//     }
    
//     /// <summary>
//     /// 初始化控制器
//     /// </summary>
//     private void InitializeController()
//     {
//         if (autoFindGrids)
//         {
//             FindAllGrids();
//         }
        
//         // 记录初始状态
//         foreach (ItemGrid grid in controlledGrids)
//         {
//             if (grid != null)
//             {
//                 gridStates[grid] = grid.gameObject.activeInHierarchy;
//             }
//         }
        
//         if (enableDebugLog)
//         {
//             Debug.Log($"GridVisibilityController: 已初始化，控制 {controlledGrids.Count} 个网格");
//         }
//     }
    
//     /// <summary>
//     /// 自动查找所有网格
//     /// </summary>
//     private void FindAllGrids()
//     {
//         ItemGrid[] allGrids = FindObjectsOfType<ItemGrid>();
//         controlledGrids.Clear();
//         controlledGrids.AddRange(allGrids);
        
//         if (enableDebugLog)
//         {
//             Debug.Log($"GridVisibilityController: 自动找到 {allGrids.Length} 个网格");
//         }
//     }
    
//     /// <summary>
//     /// 处理输入
//     /// </summary>
//     private void HandleInput()
//     {
//         if (Input.GetKeyDown(toggleAllKey))
//         {
//             ToggleAllGrids();
//         }
        
//         if (Input.GetKeyDown(hideAllKey))
//         {
//             HideAllGrids();
//         }
        
//         if (Input.GetKeyDown(showAllKey))
//         {
//             ShowAllGrids();
//         }
//     }
    
//     /// <summary>
//     /// 切换所有网格的可见性
//     /// </summary>
//     public void ToggleAllGrids()
//     {
//         foreach (ItemGrid grid in controlledGrids)
//         {
//             if (grid != null)
//             {
//                 ToggleGrid(grid);
//             }
//         }
        
//         if (enableDebugLog)
//         {
//             Debug.Log("GridVisibilityController: 切换所有网格可见性");
//         }
//     }
    
//     /// <summary>
//     /// 隐藏所有网格
//     /// </summary>
//     public void HideAllGrids()
//     {
//         foreach (ItemGrid grid in controlledGrids)
//         {
//             if (grid != null)
//             {
//                 SetGridVisibility(grid, false);
//             }
//         }
        
//         if (enableDebugLog)
//         {
//             Debug.Log("GridVisibilityController: 隐藏所有网格");
//         }
//     }
    
//     /// <summary>
//     /// 显示所有网格
//     /// </summary>
//     public void ShowAllGrids()
//     {
//         foreach (ItemGrid grid in controlledGrids)
//         {
//             if (grid != null)
//             {
//                 SetGridVisibility(grid, true);
//             }
//         }
        
//         if (enableDebugLog)
//         {
//             Debug.Log("GridVisibilityController: 显示所有网格");
//         }
//     }
    
//     /// <summary>
//     /// 切换单个网格的可见性
//     /// </summary>
//     /// <param name="grid">目标网格</param>
//     public void ToggleGrid(ItemGrid grid)
//     {
//         if (grid != null)
//         {
//             bool currentState = grid.gameObject.activeInHierarchy;
//             SetGridVisibility(grid, !currentState);
//         }
//     }
    
//     /// <summary>
//     /// 设置网格可见性
//     /// </summary>
//     /// <param name="grid">目标网格</param>
//     /// <param name="visible">是否可见</param>
//     public void SetGridVisibility(ItemGrid grid, bool visible)
//     {
//         if (grid != null)
//         {
//             grid.gameObject.SetActive(visible);
//             gridStates[grid] = visible;
            
//             if (enableDebugLog)
//             {
//                 Debug.Log($"GridVisibilityController: 设置网格 {grid.name} 可见性为 {visible}");
//             }
//         }
//     }
    
//     /// <summary>
//     /// 获取网格当前状态
//     /// </summary>
//     /// <param name="grid">目标网格</param>
//     /// <returns>是否可见</returns>
//     public bool GetGridVisibility(ItemGrid grid)
//     {
//         if (grid != null && gridStates.ContainsKey(grid))
//         {
//             return gridStates[grid];
//         }
//         return false;
//     }
    
//     /// <summary>
//     /// 添加网格到控制列表
//     /// </summary>
//     /// <param name="grid">要添加的网格</param>
//     public void AddGrid(ItemGrid grid)
//     {
//         if (grid != null && !controlledGrids.Contains(grid))
//         {
//             controlledGrids.Add(grid);
//             gridStates[grid] = grid.gameObject.activeInHierarchy;
            
//             if (enableDebugLog)
//             {
//                 Debug.Log($"GridVisibilityController: 添加网格 {grid.name} 到控制列表");
//             }
//         }
//     }
    
//     /// <summary>
//     /// 从控制列表移除网格
//     /// </summary>
//     /// <param name="grid">要移除的网格</param>
//     public void RemoveGrid(ItemGrid grid)
//     {
//         if (grid != null && controlledGrids.Contains(grid))
//         {
//             controlledGrids.Remove(grid);
//             gridStates.Remove(grid);
            
//             if (enableDebugLog)
//             {
//                 Debug.Log($"GridVisibilityController: 从控制列表移除网格 {grid.name}");
//             }
//         }
//     }
    
//     /// <summary>
//     /// 获取控制的网格数量
//     /// </summary>
//     /// <returns>网格数量</returns>
//     public int GetControlledGridCount()
//     {
//         return controlledGrids.Count;
//     }
    
//     /// <summary>
//     /// 获取可见网格数量
//     /// </summary>
//     /// <returns>可见网格数量</returns>
//     public int GetVisibleGridCount()
//     {
//         int visibleCount = 0;
//         foreach (var kvp in gridStates)
//         {
//             if (kvp.Value)
//             {
//                 visibleCount++;
//             }
//         }
//         return visibleCount;
//     }
// }