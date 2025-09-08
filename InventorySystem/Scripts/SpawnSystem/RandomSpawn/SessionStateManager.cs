using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 会话状态管理器
    /// 管理货架生成状态的会话级追踪，确保同一会话中每个货架只生成一次物品
    /// 与WarehouseFixedItemManager不同，此管理器的状态仅在当前会话有效，游戏重启后会重置
    /// </summary>
    public static class SessionStateManager
    {
        /// <summary>
        /// 会话开始时间
        /// </summary>
        private static readonly DateTime SessionStartTime = DateTime.Now;
        
        /// <summary>
        /// 会话唯一标识符
        /// </summary>
        private static readonly string SessionId = GenerateSessionId();
        
        /// <summary>
        /// 已生成物品的货架状态 (货架标识符 -> 生成时间)
        /// </summary>
        private static Dictionary<string, DateTime> _generatedShelves = new Dictionary<string, DateTime>();
        
        /// <summary>
        /// 货架生成详细信息 (货架标识符 -> 生成信息)
        /// </summary>
        private static Dictionary<string, ShelfGenerationInfo> _generationDetails = new Dictionary<string, ShelfGenerationInfo>();
        
        /// <summary>
        /// 状态变更事件
        /// </summary>
        public static event System.Action<string, bool> OnShelfStateChanged;
        
        /// <summary>
        /// 会话重置事件
        /// </summary>
        public static event System.Action OnSessionReset;
        
        /// <summary>
        /// 调试日志开关
        /// </summary>
        private static bool _enableDebugLog = true;
        
        #region 公共接口
        
        /// <summary>
        /// 检查货架是否已在当前会话中生成过物品
        /// </summary>
        /// <param name="shelfId">货架标识符 (例如: "Shelf_A_1")</param>
        /// <returns>是否已生成</returns>
        public static bool IsShelfGenerated(string shelfId)
        {
            if (string.IsNullOrEmpty(shelfId))
            {
                LogWarning("SessionStateManager: 货架ID为空，返回未生成状态");
                return false;
            }
            
            bool isGenerated = _generatedShelves.ContainsKey(shelfId);
            
            if (_enableDebugLog)
            {
                LogDebug($"检查货架 '{shelfId}' 生成状态: {(isGenerated ? "已生成" : "未生成")}");
            }
            
            return isGenerated;
        }
        
        /// <summary>
        /// 标记货架为已生成状态
        /// </summary>
        /// <param name="shelfId">货架标识符</param>
        /// <param name="generationInfo">生成详细信息（可选）</param>
        /// <returns>是否成功标记</returns>
        public static bool MarkShelfGenerated(string shelfId, ShelfGenerationInfo generationInfo = null)
        {
            if (string.IsNullOrEmpty(shelfId))
            {
                LogWarning("SessionStateManager: 货架ID为空，无法标记为已生成");
                return false;
            }
            
            DateTime generationTime = DateTime.Now;
            bool wasAlreadyGenerated = _generatedShelves.ContainsKey(shelfId);
            
            // 更新状态
            _generatedShelves[shelfId] = generationTime;
            
            // 更新详细信息
            if (generationInfo != null)
            {
                generationInfo.generationTime = generationTime;
                generationInfo.sessionId = SessionId;
                _generationDetails[shelfId] = generationInfo;
            }
            else if (!_generationDetails.ContainsKey(shelfId))
            {
                // 创建默认生成信息
                _generationDetails[shelfId] = new ShelfGenerationInfo
                {
                    shelfId = shelfId,
                    generationTime = generationTime,
                    sessionId = SessionId,
                    itemCount = 0,
                    configName = "Unknown"
                };
            }
            
            LogDebug($"标记货架 '{shelfId}' 为已生成 (时间: {generationTime:HH:mm:ss})");
            
            // 触发事件
            OnShelfStateChanged?.Invoke(shelfId, true);
            
            return !wasAlreadyGenerated; // 返回是否是首次标记
        }
        
        /// <summary>
        /// 取消货架的已生成标记
        /// </summary>
        /// <param name="shelfId">货架标识符</param>
        /// <returns>是否成功取消标记</returns>
        public static bool UnmarkShelfGenerated(string shelfId)
        {
            if (string.IsNullOrEmpty(shelfId))
            {
                LogWarning("SessionStateManager: 货架ID为空，无法取消生成标记");
                return false;
            }
            
            bool wasGenerated = _generatedShelves.Remove(shelfId);
            _generationDetails.Remove(shelfId);
            
            if (wasGenerated)
            {
                LogDebug($"取消货架 '{shelfId}' 的生成标记");
                
                // 触发事件
                OnShelfStateChanged?.Invoke(shelfId, false);
            }
            
            return wasGenerated;
        }
        
        /// <summary>
        /// 获取货架的生成信息
        /// </summary>
        /// <param name="shelfId">货架标识符</param>
        /// <returns>生成信息，如果未生成则返回null</returns>
        public static ShelfGenerationInfo GetShelfGenerationInfo(string shelfId)
        {
            if (string.IsNullOrEmpty(shelfId))
                return null;
            
            _generationDetails.TryGetValue(shelfId, out ShelfGenerationInfo info);
            return info;
        }
        
        /// <summary>
        /// 获取所有已生成货架的列表
        /// </summary>
        /// <returns>已生成货架的标识符列表</returns>
        public static List<string> GetGeneratedShelves()
        {
            return new List<string>(_generatedShelves.Keys);
        }
        
        /// <summary>
        /// 获取所有已生成货架的详细信息
        /// </summary>
        /// <returns>生成信息列表</returns>
        public static List<ShelfGenerationInfo> GetAllGenerationInfo()
        {
            return new List<ShelfGenerationInfo>(_generationDetails.Values);
        }
        
        /// <summary>
        /// 重置会话状态
        /// </summary>
        public static void ResetSessionState()
        {
            int previousCount = _generatedShelves.Count;
            
            _generatedShelves.Clear();
            _generationDetails.Clear();
            
            LogDebug($"会话状态已重置，清除了 {previousCount} 个货架的生成状态");
            
            // 触发事件
            OnSessionReset?.Invoke();
        }
        
        /// <summary>
        /// 获取会话统计信息
        /// </summary>
        /// <returns>会话统计信息</returns>
        public static SessionStatistics GetSessionStatistics()
        {
            var stats = new SessionStatistics
            {
                sessionId = SessionId,
                sessionStartTime = SessionStartTime,
                sessionDuration = DateTime.Now - SessionStartTime,
                totalGeneratedShelves = _generatedShelves.Count,
                totalItemsGenerated = _generationDetails.Values.Sum(info => info.itemCount),
                averageItemsPerShelf = _generatedShelves.Count > 0 
                    ? (float)_generationDetails.Values.Sum(info => info.itemCount) / _generatedShelves.Count 
                    : 0f
            };
            
            // 按时间排序的生成历史
            stats.generationHistory = _generationDetails.Values
                .OrderBy(info => info.generationTime)
                .ToList();
            
            return stats;
        }
        
        /// <summary>
        /// 获取会话ID
        /// </summary>
        /// <returns>当前会话的唯一标识符</returns>
        public static string GetSessionId()
        {
            return SessionId;
        }
        
        /// <summary>
        /// 获取会话开始时间
        /// </summary>
        /// <returns>会话开始时间</returns>
        public static DateTime GetSessionStartTime()
        {
            return SessionStartTime;
        }
        
        /// <summary>
        /// 设置调试日志开关
        /// </summary>
        /// <param name="enabled">是否启用调试日志</param>
        public static void SetDebugLogEnabled(bool enabled)
        {
            _enableDebugLog = enabled;
            LogDebug($"调试日志已{(enabled ? "启用" : "禁用")}");
        }
        
        #endregion
        
        #region 查询和过滤
        
        /// <summary>
        /// 获取指定时间范围内生成的货架
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>符合条件的货架列表</returns>
        public static List<string> GetShelvesGeneratedInTimeRange(DateTime startTime, DateTime endTime)
        {
            return _generatedShelves
                .Where(kvp => kvp.Value >= startTime && kvp.Value <= endTime)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// 获取最近生成的N个货架
        /// </summary>
        /// <param name="count">数量</param>
        /// <returns>最近生成的货架列表</returns>
        public static List<string> GetRecentlyGeneratedShelves(int count)
        {
            return _generatedShelves
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// 检查是否有任何货架已生成
        /// </summary>
        /// <returns>是否有已生成的货架</returns>
        public static bool HasAnyGeneratedShelves()
        {
            return _generatedShelves.Count > 0;
        }
        
        /// <summary>
        /// 获取生成时间最早的货架
        /// </summary>
        /// <returns>最早生成的货架ID，如果没有则返回null</returns>
        public static string GetFirstGeneratedShelf()
        {
            if (_generatedShelves.Count == 0) return null;
            
            return _generatedShelves
                .OrderBy(kvp => kvp.Value)
                .First()
                .Key;
        }
        
        /// <summary>
        /// 获取生成时间最晚的货架
        /// </summary>
        /// <returns>最晚生成的货架ID，如果没有则返回null</returns>
        public static string GetLastGeneratedShelf()
        {
            if (_generatedShelves.Count == 0) return null;
            
            return _generatedShelves
                .OrderByDescending(kvp => kvp.Value)
                .First()
                .Key;
        }
        
        #endregion
        
        #region 批量操作
        
        /// <summary>
        /// 批量标记多个货架为已生成
        /// </summary>
        /// <param name="shelfIds">货架ID列表</param>
        /// <returns>成功标记的数量</returns>
        public static int MarkMultipleShelvesGenerated(IEnumerable<string> shelfIds)
        {
            if (shelfIds == null) return 0;
            
            int successCount = 0;
            foreach (string shelfId in shelfIds)
            {
                if (MarkShelfGenerated(shelfId))
                {
                    successCount++;
                }
            }
            
            LogDebug($"批量标记完成，成功标记 {successCount} 个货架");
            return successCount;
        }
        
        /// <summary>
        /// 批量取消多个货架的生成标记
        /// </summary>
        /// <param name="shelfIds">货架ID列表</param>
        /// <returns>成功取消标记的数量</returns>
        public static int UnmarkMultipleShelvesGenerated(IEnumerable<string> shelfIds)
        {
            if (shelfIds == null) return 0;
            
            int successCount = 0;
            foreach (string shelfId in shelfIds)
            {
                if (UnmarkShelfGenerated(shelfId))
                {
                    successCount++;
                }
            }
            
            LogDebug($"批量取消标记完成，成功取消 {successCount} 个货架的标记");
            return successCount;
        }
        
        #endregion
        
        #region 验证和调试
        
        /// <summary>
        /// 验证会话状态的一致性
        /// </summary>
        /// <returns>验证结果</returns>
        public static bool ValidateSessionState(out List<string> issues)
        {
            issues = new List<string>();
            bool isValid = true;
            
            // 检查生成状态和详细信息的一致性
            foreach (var shelfId in _generatedShelves.Keys)
            {
                if (!_generationDetails.ContainsKey(shelfId))
                {
                    issues.Add($"货架 '{shelfId}' 有生成状态但缺少详细信息");
                    isValid = false;
                }
            }
            
            foreach (var shelfId in _generationDetails.Keys)
            {
                if (!_generatedShelves.ContainsKey(shelfId))
                {
                    issues.Add($"货架 '{shelfId}' 有详细信息但缺少生成状态");
                    isValid = false;
                }
            }
            
            // 检查时间一致性
            foreach (var kvp in _generatedShelves)
            {
                if (_generationDetails.TryGetValue(kvp.Key, out var info))
                {
                    if (Math.Abs((info.generationTime - kvp.Value).TotalSeconds) > 1)
                    {
                        issues.Add($"货架 '{kvp.Key}' 的生成时间不一致");
                        isValid = false;
                    }
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 打印会话状态调试信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DebugPrintSessionState()
        {
            var stats = GetSessionStatistics();
            
            Debug.Log($"SessionStateManager Debug Info:\n" +
                     $"  Session ID: {stats.sessionId}\n" +
                     $"  Session Duration: {stats.sessionDuration.TotalMinutes:F1} minutes\n" +
                     $"  Generated Shelves: {stats.totalGeneratedShelves}\n" +
                     $"  Total Items: {stats.totalItemsGenerated}\n" +
                     $"  Average Items/Shelf: {stats.averageItemsPerShelf:F1}\n" +
                     $"  Generated Shelf IDs: [{string.Join(", ", _generatedShelves.Keys)}]");
        }
        
        /// <summary>
        /// 清理过期的生成记录（可选功能）
        /// </summary>
        /// <param name="maxAge">最大保留时间</param>
        /// <returns>清理的记录数量</returns>
        public static int CleanupExpiredRecords(TimeSpan maxAge)
        {
            DateTime cutoffTime = DateTime.Now - maxAge;
            var expiredShelves = _generatedShelves
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
            
            int cleanedCount = 0;
            foreach (string shelfId in expiredShelves)
            {
                if (UnmarkShelfGenerated(shelfId))
                {
                    cleanedCount++;
                }
            }
            
            if (cleanedCount > 0)
            {
                LogDebug($"清理了 {cleanedCount} 个过期的生成记录");
            }
            
            return cleanedCount;
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 生成会话唯一标识符
        /// </summary>
        /// <returns>会话ID</returns>
        private static string GenerateSessionId()
        {
            return $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private static void LogDebug(string message)
        {
            if (_enableDebugLog)
            {
                Debug.Log($"[SessionStateManager] {message}");
            }
        }
        
        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="message">警告消息</param>
        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[SessionStateManager] {message}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 货架生成信息
    /// </summary>
    [System.Serializable]
    public class ShelfGenerationInfo
    {
        public string shelfId;              // 货架标识符
        public DateTime generationTime;     // 生成时间
        public string sessionId;            // 会话ID
        public int itemCount;               // 生成的物品数量
        public string configName;           // 使用的配置名称
        public List<string> generatedItems; // 生成的物品列表（可选）
        
        public ShelfGenerationInfo()
        {
            generatedItems = new List<string>();
        }
        
        public override string ToString()
        {
            return $"Shelf: {shelfId}, Time: {generationTime:HH:mm:ss}, Items: {itemCount}, Config: {configName}";
        }
    }
    
    /// <summary>
    /// 会话统计信息
    /// </summary>
    [System.Serializable]
    public struct SessionStatistics
    {
        public string sessionId;                        // 会话ID
        public DateTime sessionStartTime;              // 会话开始时间
        public TimeSpan sessionDuration;               // 会话持续时间
        public int totalGeneratedShelves;              // 总生成货架数
        public int totalItemsGenerated;                // 总生成物品数
        public float averageItemsPerShelf;             // 平均每个货架的物品数
        public List<ShelfGenerationInfo> generationHistory; // 生成历史
        
        public override string ToString()
        {
            return $"Session {sessionId}: {totalGeneratedShelves} shelves, {totalItemsGenerated} items, " +
                   $"{sessionDuration.TotalMinutes:F1} minutes";
        }
    }
}
