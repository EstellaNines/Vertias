using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InventorySystem.Database
{
    /// <summary>
    /// 物品数据库管理器 - 基于GlobalId的物品映射系统
    /// 提供快速的物品查找和验证功能
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        #region 单例模式

        private static ItemDatabase instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ItemDatabase>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ItemDatabase");
                        instance = go.AddComponent<ItemDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        #endregion

        #region 核心数据

        [Header("数据库状态")]
        [FieldLabel("初始化状态")]
        [SerializeField, Tooltip("数据库是否已初始化")]
        private bool isInitialized = false;

        [FieldLabel("物品总数")]
        [SerializeField, Tooltip("已加载的物品总数")]
        private int totalItemCount = 0;

        [FieldLabel("加载时间(毫秒)")]
        [SerializeField, Tooltip("数据库加载时间(毫秒)")]
        private float loadTimeMs = 0f;

        [Header("映射表")]
        [FieldLabel("映射表大小")]
        [SerializeField, Tooltip("GlobalId映射表的大小")]
        private int mappingTableSize = 0;

        // 核心映射表：GlobalId -> ItemDataSO
        private Dictionary<long, ItemDataSO> globalIdToItemMap;

        // 辅助映射表：ItemCategory -> List<ItemDataSO>
        private Dictionary<ItemCategory, List<ItemDataSO>> categoryToItemsMap;

        // 加载的所有物品数据缓存
        private List<ItemDataSO> allItemData;

        // 错误记录
        private List<string> loadErrors;
        private List<long> duplicateGlobalIds;

        #endregion

        #region 公共属性

        /// <summary>
        /// 数据库是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 物品总数
        /// </summary>
        public int TotalItemCount => totalItemCount;

        /// <summary>
        /// 加载时间(毫秒)
        /// </summary>
        public float LoadTimeMs => loadTimeMs;

        /// <summary>
        /// 映射表大小
        /// </summary>
        public int MappingTableSize => mappingTableSize;

        /// <summary>
        /// 加载错误列表
        /// </summary>
        public IReadOnlyList<string> LoadErrors => loadErrors?.AsReadOnly();

        /// <summary>
        /// 重复GlobalId列表
        /// </summary>
        public IReadOnlyList<long> DuplicateGlobalIds => duplicateGlobalIds?.AsReadOnly();

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 确保单例唯一性
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Debug.LogWarning("[ItemDatabase] 检测到重复的ItemDatabase实例，销毁当前实例.");
                Destroy(gameObject);
                return;
            }

            // 初始化集合
            globalIdToItemMap = new Dictionary<long, ItemDataSO>();
            categoryToItemsMap = new Dictionary<ItemCategory, List<ItemDataSO>>();
            allItemData = new List<ItemDataSO>();
            loadErrors = new List<string>();
            duplicateGlobalIds = new List<long>();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializeDatabase();
            }
        }

        #endregion

        #region 数据库初始化

        /// <summary>
        /// 初始化物品数据库
        /// </summary>
        [ContextMenu("Initialize Database")]
        public void InitializeDatabase()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[ItemDatabase] 数据库已经初始化，跳过重复初始化.");
                return;
            }

            Debug.Log("[ItemDatabase] 开始初始化物品数据库...");
            var startTime = Time.realtimeSinceStartup;

            try
            {
                // 清理之前的数据
                ClearDatabase();

                // 加载所有ItemDataSO
                LoadAllItemData();

                // 建立映射表
                BuildMappingTables();

                // 验证数据完整性
                ValidateDatabase();

                // 记录统计信息
                totalItemCount = allItemData.Count;
                mappingTableSize = globalIdToItemMap.Count;
                loadTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;

                isInitialized = true;

                Debug.Log($"[ItemDatabase] 数据库初始化完成! 加载 {totalItemCount} 个物品，映射表大小 {mappingTableSize}，耗时 {loadTimeMs:F2}ms");

                // 输出统计信息
                LogDatabaseStats();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemDatabase] 数据库初始化失败: {e.Message}");
                loadErrors.Add($"初始化异常: {e.Message}");
            }
        }

        /// <summary>
        /// 清理数据库
        /// </summary>
        private void ClearDatabase()
        {
            globalIdToItemMap?.Clear();
            categoryToItemsMap?.Clear();
            allItemData?.Clear();
            loadErrors?.Clear();
            duplicateGlobalIds?.Clear();

            totalItemCount = 0;
            mappingTableSize = 0;
            loadTimeMs = 0f;
            isInitialized = false;
        }

        #endregion

        #region 资源加载

        /// <summary>
        /// 加载所有ItemDataSO资源
        /// </summary>
        private void LoadAllItemData()
        {
            Debug.Log("[ItemDatabase] 开始加载ItemDataSO资源...");

            // 使用Resources.LoadAll加载所有ItemDataSO
            const string resourcePath = "InventorySystemResources/ItemScriptableObject";
            ItemDataSO[] loadedItems = Resources.LoadAll<ItemDataSO>(resourcePath);

            if (loadedItems == null || loadedItems.Length == 0)
            {
                string error = $"未找到ItemDataSO资源，路径: {resourcePath}";
                Debug.LogError($"[ItemDatabase] {error}");
                loadErrors.Add(error);
                return;
            }

            Debug.Log($"[ItemDatabase] 从Resources加载到 {loadedItems.Length} 个ItemDataSO资源");

            // 过滤和验证加载的数据
            foreach (var item in loadedItems)
            {
                if (item == null)
                {
                    loadErrors.Add("发现空的ItemDataSO引用");
                    continue;
                }

                // 验证GlobalId
                if (item.GlobalId <= 0)
                {
                    string error = $"ItemDataSO '{item.name}' 的GlobalId无效: {item.GlobalId}";
                    Debug.LogWarning($"[ItemDatabase] {error}");
                    loadErrors.Add(error);
                    continue;
                }

                allItemData.Add(item);
            }

            Debug.Log($"[ItemDatabase] 成功加载 {allItemData.Count} 个有效的ItemDataSO");
        }

        #endregion

        #region 映射表构建

        /// <summary>
        /// 建立映射表
        /// </summary>
        private void BuildMappingTables()
        {
            Debug.Log("[ItemDatabase] 开始构建映射表...");

            // 构建GlobalId映射表
            BuildGlobalIdMapping();

            // 构建分类映射表
            BuildCategoryMapping();

            Debug.Log($"[ItemDatabase] 映射表构建完成. GlobalId映射: {globalIdToItemMap.Count}, 分类映射: {categoryToItemsMap.Count}");
        }

        /// <summary>
        /// 构建GlobalId映射表
        /// </summary>
        private void BuildGlobalIdMapping()
        {
            foreach (var item in allItemData)
            {
                long globalId = item.GlobalId;

                // 检查重复的GlobalId
                if (globalIdToItemMap.ContainsKey(globalId))
                {
                    string error = $"发现重复的GlobalId: {globalId}, 物品1: '{globalIdToItemMap[globalId].name}', 物品2: '{item.name}'";
                    Debug.LogWarning($"[ItemDatabase] {error}");
                    loadErrors.Add(error);
                    duplicateGlobalIds.Add(globalId);
                    continue; // 保留第一个，跳过重复的
                }

                globalIdToItemMap[globalId] = item;
            }
        }

        /// <summary>
        /// 构建分类映射表
        /// </summary>
        private void BuildCategoryMapping()
        {
            foreach (var item in allItemData)
            {
                ItemCategory category = item.category;

                if (!categoryToItemsMap.ContainsKey(category))
                {
                    categoryToItemsMap[category] = new List<ItemDataSO>();
                }

                categoryToItemsMap[category].Add(item);
            }
        }

        #endregion

        #region 数据库验证

        /// <summary>
        /// 验证数据库完整性
        /// </summary>
        private void ValidateDatabase()
        {
            Debug.Log("[ItemDatabase] 开始验证数据库完整性...");

            int validItems = 0;
            int invalidItems = 0;

            foreach (var item in allItemData)
            {
                if (ValidateItemData(item))
                {
                    validItems++;
                }
                else
                {
                    invalidItems++;
                }
            }

            Debug.Log($"[ItemDatabase] 数据验证完成. 有效物品: {validItems}, 无效物品: {invalidItems}");

            if (invalidItems > 0)
            {
                Debug.LogWarning($"[ItemDatabase] 发现 {invalidItems} 个无效物品，请检查ItemDataSO配置");
            }
        }

        /// <summary>
        /// 验证单个物品数据
        /// </summary>
        private bool ValidateItemData(ItemDataSO item)
        {
            if (item == null) return false;

            // 验证必要字段
            if (item.GlobalId <= 0)
            {
                loadErrors.Add($"物品 '{item.name}' 的GlobalId无效: {item.GlobalId}");
                return false;
            }

            if (string.IsNullOrEmpty(item.itemName))
            {
                loadErrors.Add($"物品 '{item.name}' 的itemName为空");
                return false;
            }

            if (item.width <= 0 || item.height <= 0)
            {
                loadErrors.Add($"物品 '{item.name}' 的尺寸无效: {item.width}x{item.height}");
                return false;
            }

            return true;
        }

        #endregion

        #region 公共查找API

        /// <summary>
        /// 根据GlobalId获取物品数据
        /// </summary>
        /// <param name="globalId">全局唯一ID</param>
        /// <returns>ItemDataSO，如果不存在返回null</returns>
        public ItemDataSO GetItemByGlobalId(long globalId)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[ItemDatabase] 数据库未初始化，无法查找物品");
                return null;
            }

            globalIdToItemMap.TryGetValue(globalId, out ItemDataSO item);
            return item;
        }

        /// <summary>
        /// 尝试根据GlobalId获取物品数据
        /// </summary>
        /// <param name="globalId">全局唯一ID</param>
        /// <param name="item">输出的ItemDataSO</param>
        /// <returns>是否找到物品</returns>
        public bool TryGetItemByGlobalId(long globalId, out ItemDataSO item)
        {
            item = null;

            if (!isInitialized)
            {
                Debug.LogWarning("[ItemDatabase] 数据库未初始化，无法查找物品");
                return false;
            }

            return globalIdToItemMap.TryGetValue(globalId, out item);
        }

        /// <summary>
        /// 验证GlobalId是否有效
        /// </summary>
        /// <param name="globalId">全局唯一ID</param>
        /// <returns>是否有效</returns>
        public bool IsValidGlobalId(long globalId)
        {
            if (!isInitialized) return false;
            return globalIdToItemMap.ContainsKey(globalId);
        }

        /// <summary>
        /// 获取所有物品数据
        /// </summary>
        /// <returns>所有ItemDataSO的副本列表</returns>
        public List<ItemDataSO> GetAllItems()
        {
            if (!isInitialized) return new List<ItemDataSO>();
            return new List<ItemDataSO>(allItemData);
        }

        /// <summary>
        /// 根据类别获取物品数据
        /// </summary>
        /// <param name="category">物品种类</param>
        /// <returns>该类别的所有ItemDataSO</returns>
        public List<ItemDataSO> GetItemsByCategory(ItemCategory category)
        {
            if (!isInitialized) return new List<ItemDataSO>();

            if (categoryToItemsMap.TryGetValue(category, out List<ItemDataSO> items))
            {
                return new List<ItemDataSO>(items);
            }

            return new List<ItemDataSO>();
        }

        /// <summary>
        /// 获取所有GlobalId
        /// </summary>
        /// <returns>所有有效的GlobalId列表</returns>
        public List<long> GetAllGlobalIds()
        {
            if (!isInitialized) return new List<long>();
            return globalIdToItemMap.Keys.ToList();
        }

        #endregion

        #region 调试和统计

        /// <summary>
        /// 输出数据库统计信息
        /// </summary>
        [ContextMenu("Log Database Stats")]
        public void LogDatabaseStats()
        {
            if (!isInitialized)
            {
                Debug.Log("[ItemDatabase] 数据库未初始化");
                return;
            }

            Debug.Log("=== ItemDatabase 统计信息 ===");
            Debug.Log($"初始化状态: {isInitialized}");
            Debug.Log($"物品总数: {totalItemCount}");
            Debug.Log($"映射表大小: {mappingTableSize}");
            Debug.Log($"加载时间: {loadTimeMs:F2}ms");
            Debug.Log($"加载错误数: {loadErrors.Count}");
            Debug.Log($"重复GlobalId数: {duplicateGlobalIds.Count}");

            // 按类别统计
            Debug.Log("=== 分类统计 ===");
            foreach (var kvp in categoryToItemsMap)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value.Count} 个物品");
            }

            // 错误信息
            if (loadErrors.Count > 0)
            {
                Debug.Log("=== 加载错误 ===");
                foreach (var error in loadErrors)
                {
                    Debug.LogWarning($"  - {error}");
                }
            }

            // 重复GlobalId
            if (duplicateGlobalIds.Count > 0)
            {
                Debug.Log("=== 重复GlobalId ===");
                foreach (var id in duplicateGlobalIds)
                {
                    Debug.LogWarning($"  - GlobalId: {id}");
                }
            }
        }

        /// <summary>
        /// 强制重新初始化数据库
        /// </summary>
        [ContextMenu("Force Reinitialize")]
        public void ForceReinitialize()
        {
            Debug.Log("[ItemDatabase] 强制重新初始化数据库...");
            isInitialized = false;
            InitializeDatabase();
        }

        #endregion

        #region 编辑器工具

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中验证所有ItemDataSO的GlobalId唯一性
        /// </summary>
        [ContextMenu("Editor: Validate GlobalId Uniqueness")]
        public void EditorValidateGlobalIdUniqueness()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDataSO");
            Dictionary<long, string> globalIdToAssetPath = new Dictionary<long, string>();
            List<string> duplicates = new List<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ItemDataSO item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(assetPath);

                if (item != null)
                {
                    long globalId = item.GlobalId;

                    if (globalIdToAssetPath.ContainsKey(globalId))
                    {
                        duplicates.Add($"GlobalId {globalId}: '{globalIdToAssetPath[globalId]}' 和 '{assetPath}'");
                    }
                    else
                    {
                        globalIdToAssetPath[globalId] = assetPath;
                    }
                }
            }

            if (duplicates.Count > 0)
            {
                Debug.LogError($"[ItemDatabase] 发现 {duplicates.Count} 个GlobalId重复:");
                foreach (var duplicate in duplicates)
                {
                    Debug.LogError($"  - {duplicate}");
                }
            }
            else
            {
                Debug.Log($"[ItemDatabase] GlobalId唯一性验证通过，共检查 {globalIdToAssetPath.Count} 个ItemDataSO");
            }
        }
#endif

        #endregion
    }
}