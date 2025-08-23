using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem.Grid;

namespace GridSystem.Editor
{
    /// <summary>
    /// 网格系统统计信息类 - 用于记录和管理网格系统的各种统计数据
    /// </summary>
    [System.Serializable]
    public class GridSystemStatistics
    {
        [Header("基础统计")]
        public int TotalGrids;              // 总网格数量
        public int ActiveGrids;             // 活跃网格数量
        public int TotalItems;              // 总物品数量
        public float AverageOccupancy;      // 平均占用率
        public double LastUpdateTime;       // 最后更新时间（Unix时间戳）
        
        [Header("性能统计")]
        public float AverageRefreshTime;    // 平均刷新时间（毫秒）
        public int RefreshCount;            // 刷新次数
        public int TotalRefreshCount;       // 总刷新次数
        public float TotalProcessingTime;   // 总处理时间
        public Dictionary<string, int> GridTypeUpdateCounts; // 网格类型更新次数统计
        
        [Header("事件统计")]
        public int TotalEvents;             // 总事件数量
        public int ProcessedEvents;         // 已处理事件数量
        public int PendingEvents;           // 待处理事件数量
        
        /// <summary>
        /// 构造函数 - 初始化统计数据
        /// </summary>
        public GridSystemStatistics()
        {
            TotalGrids = 0;
            ActiveGrids = 0;
            TotalItems = 0;
            AverageOccupancy = 0f;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AverageRefreshTime = 0f;
            RefreshCount = 0;
            TotalRefreshCount = 0;
            TotalProcessingTime = 0f;
            GridTypeUpdateCounts = new Dictionary<string, int>();
            TotalEvents = 0;
            ProcessedEvents = 0;
            PendingEvents = 0;
        }
        
        /// <summary>
        /// 更新基础统计信息
        /// </summary>
        public void UpdateBasicStats(int totalGrids, int activeGrids, int totalItems, float averageOccupancy)
        {
            TotalGrids = totalGrids;
            ActiveGrids = activeGrids;
            TotalItems = totalItems;
            AverageOccupancy = averageOccupancy;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// 更新性能统计信息
        /// </summary>
        public void UpdatePerformanceStats(float refreshTime)
        {
            RefreshCount++;
            TotalProcessingTime += refreshTime;
            AverageRefreshTime = TotalProcessingTime / RefreshCount;
        }
        
        /// <summary>
        /// 更新事件统计信息
        /// </summary>
        public void UpdateEventStats(int totalEvents, int processedEvents, int pendingEvents)
        {
            TotalEvents = totalEvents;
            ProcessedEvents = processedEvents;
            PendingEvents = pendingEvents;
        }
        
        /// <summary>
        /// 重置所有统计信息
        /// </summary>
        public void Reset()
        {
            TotalGrids = 0;
            ActiveGrids = 0;
            TotalItems = 0;
            AverageOccupancy = 0f;
            AverageRefreshTime = 0f;
            RefreshCount = 0;
            TotalRefreshCount = 0;
            TotalProcessingTime = 0f;
            GridTypeUpdateCounts?.Clear();
            TotalEvents = 0;
            ProcessedEvents = 0;
            PendingEvents = 0;
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// 获取统计信息的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"网格统计: 总数={TotalGrids}, 活跃={ActiveGrids}, 物品={TotalItems}, 占用率={AverageOccupancy:F1}%";
        }
    }
    
    /// <summary>
    /// 系统统计信息类 - SystemStatistics的别名，用于向后兼容
    /// </summary>
    [System.Serializable]
    public class SystemStatistics : GridSystemStatistics
    {
        [Header("扩展统计")]
        public int RegisteredGridsCount;    // 已注册网格数量
        public DateTime LastRegistrationTime; // 最后注册时间
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemStatistics() : base()
        {
            RegisteredGridsCount = 0;
            LastRegistrationTime = DateTime.Now;
        }
        
        /// <summary>
        /// 更新注册统计信息
        /// </summary>
        public void UpdateRegistrationStats(int registeredCount)
        {
            RegisteredGridsCount = registeredCount;
            LastRegistrationTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 刷新统计信息类 - 用于记录刷新系统的统计数据
    /// </summary>
    [System.Serializable]
    public class RefreshStatistics
    {
        [Header("刷新统计")]
        public int TotalRefreshCount;       // 总刷新次数
        public float AverageRefreshTime;    // 平均刷新时间（毫秒）
        public bool IsAutoRefreshEnabled;   // 是否启用自动刷新
        public float RefreshInterval;       // 刷新间隔（秒）
        public bool IsCurrentlyRefreshing;  // 当前是否正在刷新
        public DateTime LastRefreshTime;    // 最后刷新时间
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public RefreshStatistics()
        {
            TotalRefreshCount = 0;
            AverageRefreshTime = 0f;
            IsAutoRefreshEnabled = false;
            RefreshInterval = 1f;
            IsCurrentlyRefreshing = false;
            LastRefreshTime = DateTime.MinValue;
        }
        
        /// <summary>
        /// 记录一次刷新操作
        /// </summary>
        public void RecordRefresh(float refreshTime)
        {
            TotalRefreshCount++;
            AverageRefreshTime = ((AverageRefreshTime * (TotalRefreshCount - 1)) + refreshTime) / TotalRefreshCount;
            LastRefreshTime = DateTime.Now;
        }
        
        /// <summary>
        /// 重置刷新统计
        /// </summary>
        public void Reset()
        {
            TotalRefreshCount = 0;
            AverageRefreshTime = 0f;
            LastRefreshTime = DateTime.MinValue;
        }
    }
}