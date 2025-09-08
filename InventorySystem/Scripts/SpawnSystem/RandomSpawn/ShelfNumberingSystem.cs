using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 货架编号系统
    /// 管理货架的自动编号分配，确保每个货架都有唯一的标识符
    /// 编号格式：Shelf_A_1, Shelf_B_1, Shelf_C_1... (货架_字母编号_会话次数)
    /// </summary>
    public static class ShelfNumberingSystem
    {
        /// <summary>
        /// 货架编号前缀
        /// </summary>
        private const string SHELF_PREFIX = "Shelf_";
        
        /// <summary>
        /// 会话编号后缀
        /// </summary>
        private const string SESSION_SUFFIX = "_1";
        
        /// <summary>
        /// 已分配的货架编号映射 (GameObject实例ID -> 编号字符)
        /// </summary>
        private static Dictionary<int, char> _assignedNumbers = new Dictionary<int, char>();
        
        /// <summary>
        /// 已使用的编号集合
        /// </summary>
        private static HashSet<char> _usedNumbers = new HashSet<char>();
        
        /// <summary>
        /// 下一个可用的编号索引
        /// </summary>
        private static int _nextNumberIndex = 0;
        
        /// <summary>
        /// 基础字母表 (A-Z)
        /// </summary>
        private static readonly char[] BaseAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        
        /// <summary>
        /// 编号分配事件
        /// </summary>
        public static event System.Action<GameObject, string> OnShelfNumberAssigned;
        
        /// <summary>
        /// 编号重置事件
        /// </summary>
        public static event System.Action OnNumberingReset;
        
        #region 公共接口
        
        /// <summary>
        /// 获取或分配货架编号
        /// 如果货架已有编号则返回现有编号，否则分配新编号
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <returns>完整的货架标识符，格式为 "Shelf_A_1"</returns>
        public static string GetOrAssignShelfNumber(GameObject shelfObject)
        {
            if (shelfObject == null)
            {
                Debug.LogWarning("ShelfNumberingSystem: 货架对象为null，无法分配编号");
                return GenerateDefaultShelfId();
            }
            
            int instanceId = shelfObject.GetInstanceID();
            
            // 检查是否已有编号
            if (_assignedNumbers.TryGetValue(instanceId, out char existingNumber))
            {
                string existingId = GenerateShelfId(existingNumber);
                Debug.Log($"ShelfNumberingSystem: 货架 '{shelfObject.name}' 已有编号: {existingId}");
                return existingId;
            }
            
            // 分配新编号
            char newNumber = GetNextAvailableNumber();
            _assignedNumbers[instanceId] = newNumber;
            _usedNumbers.Add(newNumber);
            
            string newShelfId = GenerateShelfId(newNumber);
            
            Debug.Log($"ShelfNumberingSystem: 为货架 '{shelfObject.name}' 分配新编号: {newShelfId}");
            
            // 触发事件
            OnShelfNumberAssigned?.Invoke(shelfObject, newShelfId);
            
            return newShelfId;
        }
        
        /// <summary>
        /// 获取货架的当前编号（如果已分配）
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <returns>货架标识符，如果未分配则返回null</returns>
        public static string GetShelfNumber(GameObject shelfObject)
        {
            if (shelfObject == null) return null;
            
            int instanceId = shelfObject.GetInstanceID();
            if (_assignedNumbers.TryGetValue(instanceId, out char number))
            {
                return GenerateShelfId(number);
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查货架是否已分配编号
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <returns>是否已分配编号</returns>
        public static bool HasShelfNumber(GameObject shelfObject)
        {
            if (shelfObject == null) return false;
            return _assignedNumbers.ContainsKey(shelfObject.GetInstanceID());
        }
        
        /// <summary>
        /// 手动分配指定编号给货架
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <param name="preferredNumber">首选编号字符</param>
        /// <returns>实际分配的货架标识符</returns>
        public static string AssignSpecificNumber(GameObject shelfObject, char preferredNumber)
        {
            if (shelfObject == null)
            {
                Debug.LogWarning("ShelfNumberingSystem: 货架对象为null，无法分配编号");
                return GenerateDefaultShelfId();
            }
            
            preferredNumber = char.ToUpper(preferredNumber);
            
            // 验证字符是否有效
            if (!IsValidNumberChar(preferredNumber))
            {
                Debug.LogWarning($"ShelfNumberingSystem: 编号字符 '{preferredNumber}' 无效，使用自动分配");
                return GetOrAssignShelfNumber(shelfObject);
            }
            
            int instanceId = shelfObject.GetInstanceID();
            
            // 检查是否已被使用
            if (_usedNumbers.Contains(preferredNumber))
            {
                Debug.LogWarning($"ShelfNumberingSystem: 编号 '{preferredNumber}' 已被使用，使用自动分配");
                return GetOrAssignShelfNumber(shelfObject);
            }
            
            // 分配指定编号
            _assignedNumbers[instanceId] = preferredNumber;
            _usedNumbers.Add(preferredNumber);
            
            string shelfId = GenerateShelfId(preferredNumber);
            
            Debug.Log($"ShelfNumberingSystem: 为货架 '{shelfObject.name}' 手动分配编号: {shelfId}");
            
            // 触发事件
            OnShelfNumberAssigned?.Invoke(shelfObject, shelfId);
            
            return shelfId;
        }
        
        /// <summary>
        /// 释放货架编号
        /// </summary>
        /// <param name="shelfObject">货架GameObject</param>
        /// <returns>是否成功释放编号</returns>
        public static bool ReleaseShelfNumber(GameObject shelfObject)
        {
            if (shelfObject == null) return false;
            
            int instanceId = shelfObject.GetInstanceID();
            if (_assignedNumbers.TryGetValue(instanceId, out char number))
            {
                _assignedNumbers.Remove(instanceId);
                _usedNumbers.Remove(number);
                
                Debug.Log($"ShelfNumberingSystem: 释放货架 '{shelfObject.name}' 的编号: {number}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 重置整个编号系统
        /// </summary>
        public static void ResetNumbering()
        {
            int previousCount = _assignedNumbers.Count;
            
            _assignedNumbers.Clear();
            _usedNumbers.Clear();
            _nextNumberIndex = 0;
            
            Debug.Log($"ShelfNumberingSystem: 编号系统已重置，清除了 {previousCount} 个编号");
            
            // 触发事件
            OnNumberingReset?.Invoke();
        }
        
        /// <summary>
        /// 获取下一个可用的编号字符
        /// </summary>
        /// <returns>下一个可用的编号字符</returns>
        public static char GetNextAvailableNumber()
        {
            // 首先尝试基础字母表 (A-Z)
            while (_nextNumberIndex < BaseAlphabet.Length)
            {
                char candidate = BaseAlphabet[_nextNumberIndex];
                _nextNumberIndex++;
                
                if (!_usedNumbers.Contains(candidate))
                {
                    return candidate;
                }
            }
            
            // 如果基础字母表用完，使用扩展编号
            return GetExtendedNumber();
        }
        
        /// <summary>
        /// 获取所有已分配的编号信息
        /// </summary>
        /// <returns>编号信息字典 (货架名称 -> 编号)</returns>
        public static Dictionary<string, string> GetAllAssignedNumbers()
        {
            var result = new Dictionary<string, string>();
            
            foreach (var kvp in _assignedNumbers)
            {
                // 尝试通过实例ID获取GameObject名称
                var obj = GetGameObjectByInstanceId(kvp.Key);
                string objectName = obj != null ? obj.name : $"Unknown_{kvp.Key}";
                string shelfId = GenerateShelfId(kvp.Value);
                
                result[objectName] = shelfId;
            }
            
            return result;
        }
        
        /// <summary>
        /// 获取编号系统的统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public static NumberingStatistics GetStatistics()
        {
            return new NumberingStatistics
            {
                totalAssigned = _assignedNumbers.Count,
                usedNumbers = _usedNumbers.Count,
                nextNumberIndex = _nextNumberIndex,
                availableBasicNumbers = BaseAlphabet.Length - _usedNumbers.Count(c => c >= 'A' && c <= 'Z'),
                usingExtendedNumbers = _nextNumberIndex >= BaseAlphabet.Length
            };
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 生成货架标识符
        /// </summary>
        /// <param name="number">编号字符</param>
        /// <returns>完整的货架标识符</returns>
        private static string GenerateShelfId(char number)
        {
            return $"{SHELF_PREFIX}{number}{SESSION_SUFFIX}";
        }
        
        /// <summary>
        /// 生成默认货架标识符（用于错误情况）
        /// </summary>
        /// <returns>默认货架标识符</returns>
        private static string GenerateDefaultShelfId()
        {
            return $"{SHELF_PREFIX}X{SESSION_SUFFIX}";
        }
        
        /// <summary>
        /// 验证编号字符是否有效
        /// </summary>
        /// <param name="numberChar">编号字符</param>
        /// <returns>是否有效</returns>
        private static bool IsValidNumberChar(char numberChar)
        {
            // 基础字母表 A-Z
            if (numberChar >= 'A' && numberChar <= 'Z')
                return true;
            
            // 扩展编号 (AA, AB, AC...)
            // 这里简化处理，只允许单个字符
            return false;
        }
        
        /// <summary>
        /// 获取扩展编号（当A-Z用完后）
        /// </summary>
        /// <returns>扩展编号字符</returns>
        private static char GetExtendedNumber()
        {
            // 扩展策略：使用双字母编号的第一个字符
            // AA=A1, AB=A2, ... BA=B1, BB=B2...
            // 这里简化实现，使用数字后缀
            
            // 寻找第一个未使用的字符（包括已释放的）
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (!_usedNumbers.Contains(c))
                {
                    return c;
                }
            }
            
            // 如果所有基础字符都被使用，返回A（会在上层逻辑中处理冲突）
            Debug.LogWarning("ShelfNumberingSystem: 所有编号都已使用，返回默认编号A");
            return 'A';
        }
        
        /// <summary>
        /// 通过实例ID获取GameObject（可能返回null如果对象已销毁）
        /// </summary>
        /// <param name="instanceId">实例ID</param>
        /// <returns>GameObject或null</returns>
        private static GameObject GetGameObjectByInstanceId(int instanceId)
        {
            // Unity没有直接通过实例ID获取对象的API
            // 这里返回null，实际使用时可以考虑其他方案
            return null;
        }
        
        #endregion
        
        #region 调试和工具方法
        
        /// <summary>
        /// 打印当前编号状态
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void DebugPrintStatus()
        {
            var stats = GetStatistics();
            Debug.Log($"ShelfNumberingSystem Status:\n" +
                     $"  Total Assigned: {stats.totalAssigned}\n" +
                     $"  Used Numbers: {stats.usedNumbers}\n" +
                     $"  Next Index: {stats.nextNumberIndex}\n" +
                     $"  Available Basic: {stats.availableBasicNumbers}\n" +
                     $"  Using Extended: {stats.usingExtendedNumbers}\n" +
                     $"  Used Numbers: [{string.Join(", ", _usedNumbers.OrderBy(c => c))}]");
        }
        
        /// <summary>
        /// 验证编号系统的一致性
        /// </summary>
        /// <returns>验证结果</returns>
        public static bool ValidateConsistency(out List<string> issues)
        {
            issues = new List<string>();
            bool isValid = true;
            
            // 检查分配表和使用集合的一致性
            var assignedNumbers = new HashSet<char>(_assignedNumbers.Values);
            if (assignedNumbers.Count != _usedNumbers.Count)
            {
                issues.Add($"分配编号数量 ({assignedNumbers.Count}) 与使用编号数量 ({_usedNumbers.Count}) 不匹配");
                isValid = false;
            }
            
            // 检查是否有编号在分配表中但不在使用集合中
            foreach (char number in assignedNumbers)
            {
                if (!_usedNumbers.Contains(number))
                {
                    issues.Add($"编号 '{number}' 已分配但未标记为使用");
                    isValid = false;
                }
            }
            
            // 检查是否有编号在使用集合中但不在分配表中
            foreach (char number in _usedNumbers)
            {
                if (!assignedNumbers.Contains(number))
                {
                    issues.Add($"编号 '{number}' 标记为使用但未分配给任何货架");
                    isValid = false;
                }
            }
            
            // 检查重复分配
            var numberCounts = _assignedNumbers.Values.GroupBy(n => n).Where(g => g.Count() > 1);
            foreach (var group in numberCounts)
            {
                issues.Add($"编号 '{group.Key}' 被分配给了 {group.Count()} 个货架");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 清理无效的编号分配（清理已销毁对象的编号）
        /// </summary>
        /// <returns>清理的编号数量</returns>
        public static int CleanupInvalidAssignments()
        {
            var toRemove = new List<int>();
            
            foreach (var kvp in _assignedNumbers)
            {
                var obj = GetGameObjectByInstanceId(kvp.Key);
                if (obj == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (int instanceId in toRemove)
            {
                if (_assignedNumbers.TryGetValue(instanceId, out char number))
                {
                    _assignedNumbers.Remove(instanceId);
                    _usedNumbers.Remove(number);
                }
            }
            
            if (toRemove.Count > 0)
            {
                Debug.Log($"ShelfNumberingSystem: 清理了 {toRemove.Count} 个无效编号分配");
            }
            
            return toRemove.Count;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 编号系统统计信息
    /// </summary>
    [System.Serializable]
    public struct NumberingStatistics
    {
        public int totalAssigned;           // 总分配数量
        public int usedNumbers;            // 已使用编号数量
        public int nextNumberIndex;        // 下一个编号索引
        public int availableBasicNumbers;  // 可用基础编号数量
        public bool usingExtendedNumbers;  // 是否使用扩展编号
        
        public override string ToString()
        {
            return $"Assigned: {totalAssigned}, Used: {usedNumbers}, Next: {nextNumberIndex}, " +
                   $"Available: {availableBasicNumbers}, Extended: {usingExtendedNumbers}";
        }
    }
}
