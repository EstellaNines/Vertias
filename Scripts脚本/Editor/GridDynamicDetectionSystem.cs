using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Grid;

namespace GridSystem.Editor
{
    /// <summary>
    /// 网格动态检测系统 - 专门处理动态创建的背包/挂具网格检测
    /// 解决监控系统无法及时发现动态创建网格的问题
    /// </summary>
    public class GridDynamicDetectionSystem
    {
        private static GridDynamicDetectionSystem instance;
        public static GridDynamicDetectionSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GridDynamicDetectionSystem();
                }
                return instance;
            }
        }

        // 事件：当检测到新的动态网格时触发
        public System.Action<BaseItemGrid> OnDynamicGridDetected;
        // 事件：当动态网格被移除时触发
        public System.Action<BaseItemGrid> OnDynamicGridRemoved;

        private HashSet<BaseItemGrid> trackedDynamicGrids = new HashSet<BaseItemGrid>();
        private Dictionary<string, BaseItemGrid> gridsByInstanceId = new Dictionary<string, BaseItemGrid>();
        private bool isMonitoring = false;
        private double lastScanTime = 0;
        private const double SCAN_INTERVAL = 0.5; // 每0.5秒扫描一次

        /// <summary>
        /// 开始监控动态网格
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring) return;

            isMonitoring = true;
            EditorApplication.update += OnEditorUpdate;

            // 立即执行一次扫描
            ScanForDynamicGrids();

            Debug.Log("[GridDynamicDetectionSystem] 开始监控动态网格创建");
        }

        /// <summary>
        /// 停止监控动态网格
        /// </summary>
        public void StopMonitoring()
        {
            if (!isMonitoring) return;

            isMonitoring = false;
            EditorApplication.update -= OnEditorUpdate;

            trackedDynamicGrids.Clear();
            gridsByInstanceId.Clear();

            Debug.Log("[GridDynamicDetectionSystem] 停止监控动态网格");
        }

        /// <summary>
        /// 编辑器更新回调
        /// </summary>
        private void OnEditorUpdate()
        {
            if (!isMonitoring) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastScanTime >= SCAN_INTERVAL)
            {
                ScanForDynamicGrids();
                lastScanTime = currentTime;
            }
        }

        /// <summary>
        /// 扫描动态创建的网格
        /// </summary>
        private void ScanForDynamicGrids()
        {
            try
            {
                // 查找所有当前存在的网格
                BaseItemGrid[] allCurrentGrids = UnityEngine.Object.FindObjectsOfType<BaseItemGrid>();

                // 检测新增的动态网格
                DetectNewDynamicGrids(allCurrentGrids);

                // 检测移除的动态网格
                DetectRemovedDynamicGrids(allCurrentGrids);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GridDynamicDetectionSystem] 扫描动态网格时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检测新增的动态网格
        /// </summary>
        private void DetectNewDynamicGrids(BaseItemGrid[] currentGrids)
        {
            foreach (var grid in currentGrids)
            {
                if (grid == null) continue;

                // 检查是否是动态创建的网格（背包或挂具）
                if (IsDynamicGrid(grid) && !trackedDynamicGrids.Contains(grid))
                {
                    // 新发现的动态网格
                    trackedDynamicGrids.Add(grid);

                    // 生成唯一标识符
                    string instanceId = GenerateGridInstanceId(grid);
                    gridsByInstanceId[instanceId] = grid;

                    Debug.Log($"[GridDynamicDetectionSystem] 检测到新的动态网格: {grid.name} (类型: {grid.GetType().Name})");

                    // 触发事件
                    OnDynamicGridDetected?.Invoke(grid);
                }
            }
        }

        /// <summary>
        /// 检测移除的动态网格
        /// </summary>
        private void DetectRemovedDynamicGrids(BaseItemGrid[] currentGrids)
        {
            var currentGridsSet = new HashSet<BaseItemGrid>(currentGrids);
            var gridsToRemove = new List<BaseItemGrid>();

            foreach (var trackedGrid in trackedDynamicGrids)
            {
                if (trackedGrid == null || !currentGridsSet.Contains(trackedGrid))
                {
                    gridsToRemove.Add(trackedGrid);
                }
            }

            foreach (var gridToRemove in gridsToRemove)
            {
                trackedDynamicGrids.Remove(gridToRemove);

                // 从ID映射中移除
                var keysToRemove = gridsByInstanceId.Where(kvp => kvp.Value == gridToRemove).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    gridsByInstanceId.Remove(key);
                }

                Debug.Log($"[GridDynamicDetectionSystem] 检测到动态网格被移除: {(gridToRemove != null ? gridToRemove.name : "null")}");

                // 触发事件
                OnDynamicGridRemoved?.Invoke(gridToRemove);
            }
        }

        /// <summary>
        /// 判断是否是动态创建的网格
        /// </summary>
        private bool IsDynamicGrid(BaseItemGrid grid)
        {
            if (grid == null) return false;

            // 检查是否是背包网格
            if (grid is BackpackItemGrid)
            {
                return true;
            }

            // 检查是否是战术挂具网格
            if (grid is TactiaclRigItemGrid)
            {
                return true;
            }

            // 检查是否是仓库网格（ItemGrid）
            if (grid is ItemGrid)
            {
                return true;
            }

            // 检查父对象是否包含装备槽组件
            Transform parent = grid.transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<EquipSlot>() != null)
                {
                    return true;
                }
                parent = parent.parent;
            }

            return false;
        }

        /// <summary>
        /// 生成网格实例ID
        /// </summary>
        private string GenerateGridInstanceId(BaseItemGrid grid)
        {
            if (grid == null) return "null";

            return $"{grid.GetType().Name}_{grid.GetInstanceID()}_{grid.name}";
        }

        /// <summary>
        /// 获取当前跟踪的动态网格列表
        /// </summary>
        public List<BaseItemGrid> GetTrackedDynamicGrids()
        {
            return new List<BaseItemGrid>(trackedDynamicGrids.Where(g => g != null));
        }

        /// <summary>
        /// 获取动态网格统计信息
        /// </summary>
        public DynamicGridStatistics GetStatistics()
        {
            var validGrids = trackedDynamicGrids.Where(g => g != null).ToList();

            return new DynamicGridStatistics
            {
                TotalDynamicGrids = validGrids.Count,
                BackpackGrids = validGrids.OfType<BackpackItemGrid>().Count(),
                TacticalRigGrids = validGrids.OfType<TactiaclRigItemGrid>().Count(),
                WarehouseGrids = validGrids.OfType<ItemGrid>().Count(),
                LastScanTime = lastScanTime,
                IsMonitoring = isMonitoring
            };
        }

        /// <summary>
        /// 强制刷新动态网格检测
        /// </summary>
        public void ForceRefresh()
        {
            if (isMonitoring)
            {
                ScanForDynamicGrids();
                Debug.Log("[GridDynamicDetectionSystem] 强制刷新动态网格检测完成");
            }
        }
    }

    /// <summary>
    /// 动态网格统计信息
    /// </summary>
    public struct DynamicGridStatistics
    {
        public int TotalDynamicGrids;
        public int BackpackGrids;
        public int TacticalRigGrids;
        public int WarehouseGrids;
        public double LastScanTime;
        public bool IsMonitoring;
    }
}